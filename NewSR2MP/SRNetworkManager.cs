
using Il2CppMonomiPark.SlimeRancher.Persist;
using NewSR2MP.Component;
using NewSR2MP.Packet;
using NewSR2MP.Patches;
using NewSR2MP.SaveModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Epic.OnlineServices;
using Il2CppMono.Security.Protocol.Ntlm;
using Il2CppTMPro;
using NewSR2MP.EpicSDK;
using SRMP.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace NewSR2MP
{
    public struct NetGameInitialSettings
    {
        public NetGameInitialSettings(bool defaultValueForAll = true) // Would not use paramater here but this version of c# is ehh...
        {
            shareMoney = defaultValueForAll;
            shareKeys = defaultValueForAll;
            shareUpgrades = defaultValueForAll;
        }

        public bool shareMoney;
        public bool shareKeys;
        public bool shareUpgrades;
    }
    public partial class MultiplayerManager
    {
        
        public static NetGameInitialSettings initialWorldSettings = new NetGameInitialSettings();

        internal static void CheckForMPSavePath()
        {
            if (!Directory.Exists(Path.Combine(GameContext.Instance.AutoSaveDirector._storageProvider.Cast<FileStorageProvider>().savePath, "MultiplayerSaves")))
            {
                Directory.CreateDirectory(Path.Combine(Path.Combine(GameContext.Instance.AutoSaveDirector._storageProvider.Cast<FileStorageProvider>().savePath, "MultiplayerSaves")));
            }
        }

        public void StartHosting()
        {
            foreach (var a in Resources.FindObjectsOfTypeAll<IdentifiableActor>())
            {
                try
                {
                    if (!string.IsNullOrEmpty(a.gameObject.scene.name))
                    {
                        var actor = a.gameObject;
                        actor.AddComponent<NetworkActor>();
                        actor.AddComponent<NetworkActorOwnerToggle>();
                        actor.AddComponent<TransformSmoother>();
                        actor.AddComponent<NetworkResource>();
                        var ts = actor.GetComponent<TransformSmoother>();
                        ts.interpolPeriod = ActorTimer;
                        ts.enabled = false;
                        actors.Add(a.GetActorId().Value, a.GetComponent<NetworkActor>());
                    }
                }
                catch { }
            }
            
            sceneContext.gameObject.AddComponent<NetworkTimeDirector>();
            sceneContext.gameObject.AddComponent<NetworkWeatherDirector>();
            
            var hostNetworkPlayer = sceneContext.player.AddComponent<NetworkPlayer>();
            hostNetworkPlayer.id = ushort.MaxValue;
            currentPlayerID = hostNetworkPlayer.id;
            players.Add(new NetPlayerState
            {
                epicID = EpicApplication.Instance.Authentication.ProductUserId,
                playerID = ushort.MaxValue,
                worldObject = hostNetworkPlayer,
                connectionState = NetworkPlayerConnectionState.Connected,
            });
            
            // Add map display for host
            sceneContext.player.AddComponent<NetworkPlayerMapDisplay>();
            
            // Add multiplayer waypoint display for host
            sceneContext.player.AddComponent<MultiplayerWaypointMapIcon>();
            
            MelonCoroutines.Start(OwnActors());
        }

        public NetworkPlayer OnPlayerJoined(string username, ushort id, ProductUserId epicID)
        {
            DoNetworkSave();
            
            var player = Instantiate(onlinePlayerPrefab);
            player.name = $"Player{id}";
            var netPlayer = player.GetComponent<NetworkPlayer>();
            
            netPlayer.usernamePanel = netPlayer.transform.GetChild(1).GetComponent<TextMesh>();
            netPlayer.usernamePanel.text = username;
            netPlayer.usernamePanel.characterSize = 0.2f;
            netPlayer.usernamePanel.anchor = TextAnchor.MiddleCenter;
            netPlayer.usernamePanel.fontSize = 24;
            
            playerUsernames.Add(username, id);
            playerUsernamesReverse.Add(id, username);
            
            netPlayer.id = id;
            
            // Add map display for joined player
            player.AddComponent<NetworkPlayerMapDisplay>();
            
            DontDestroyOnLoad(player);
            player.SetActive(true);
            
            var packet = new PlayerJoinPacket()
            {
                id = id,
                local = false,
                username = username,
            };
            var packet2 = new PlayerJoinPacket()
            {
                id = id,
                local = true,
                username = username,
            };
            NetworkSend(packet, ServerSendOptions.SendToAllExcept(id));
            NetworkSend(packet2, ServerSendOptions.SendToPlayer(id));

            return netPlayer;
        }
        public void OnPlayerLeft(ushort id)
        {
            OnServerDisconnect(id);
            
            var packet = new PlayerLeavePacket
            {
                id = id,
            };
            
            NetworkSend(packet);
        }
        
        public void StopHosting()
        {
            ammoByPlotID.Clear();

        }

        public void OnServerDisconnect(ushort player)
        {
            // Save client's data before disconnecting
            SaveClientDataOnDisconnect(player);
            
            // Сохраняем весь мир в файл (без попытки обработать отключившегося игрока)
            try
            {
                FileStream fs = File.Open(savedGamePath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);
                
                savedGame.WriteData(bw);
                
                bw.Dispose();
                fs.Dispose();
                
                SRMP.Debug("World data saved after client disconnect");
            }
            catch (Exception saveEx)
            {
                SRMP.Error($"Failed to save world data: {saveEx.Message}");
            }
            
            try
            {
                if (!TryGetPlayer(player, out var state))
                    return;
                
                // УДАЛЯЕМ МОДЕЛЬКУ ИГРОКА КЛИЕНТА
                if (state.worldObject != null)
                {
                    var playerGameObject = state.worldObject.gameObject;
                    if (playerGameObject != null)
                    {
                        SRMP.Log($"✓ Deleting client player model: Player{state.playerID}");
                        Destroy(playerGameObject);
                    }
                }
                
                players.Remove(state);
                playerUsernames.Remove(playerUsernamesReverse[state.playerID]);
                playerUsernamesReverse.Remove(player);
                players.Remove(state);
                clientToGuid.Remove(player);
            }
            catch (Exception ex)
            {
                SRMP.Error($"Error in OnServerDisconnect: {ex}");
            }

        }
        
        /// <summary>
        /// Save client's inventory and tutorial state when they disconnect
        /// </summary>
        private static void SaveClientDataOnDisconnect(ushort player)
        {
            try
            {
                SRMP.Log($"========== SAVING CLIENT DATA ==========");
                SRMP.Log($"Connection ID: {player}");
                
                // Get the client's GUID
                if (!clientToGuid.TryGetValue(player, out var clientGuid))
                {
                    SRMP.Log($"⚠ Cannot find GUID for connection {player} - data not saved!");
                    return;
                }
                
                SRMP.Log($"Client GUID: {clientGuid}");
                
                // Get the player data from saved game
                if (!savedGame.savedPlayers.TryGetPlayer(clientGuid, out var playerData))
                {
                    SRMP.Log($"⚠ Cannot find player data for GUID {clientGuid} - data not saved!");
                    return;
                }
                
                // ===== УПРОЩЕННАЯ СИСТЕМА БЕЗ ВИРТУАЛЬНОГО AmmoSlotManager =====
                // Инвентарь клиента УЖЕ СОХРАНЕН в playerData.ammo
                // Он обновляется через ClientInventorySyncPacket когда клиент выходит нормально
                // При аварийном выходе используется последнее сохраненное состояние
                
                bool inventorySaved = false;
                if (playerData.ammo != null && playerData.ammo.Count > 0)
                {
                    int itemCount = playerData.ammo.Count(x => x.count > 0);
                    SRMP.Log($"✓ Client inventory: {itemCount} items in {playerData.ammo.Count} slots");
                    SRMP.Log($"  (Updated from ClientInventorySyncPacket or last saved state)");
                    inventorySaved = true;
                }
                else
                {
                    SRMP.Log($"⚠ Client inventory is empty or null!");
                }
                
                // Mark tutorials as completed (client has seen the world)
                playerData.tutorialsCompleted = true;
                
                // Mark intro as seen (client will skip it on next join)
                // Note: hasSeenIntro is set to true in RestoreClientInventory when client first joins
                // We just keep it here to ensure it's saved to file
                
                // Save waypoint if exists
                if (MultiplayerWaypointManager.Instance != null)
                {
                    var waypoint = MultiplayerWaypointManager.Instance.GetWaypoint(player);
                    if (waypoint != null && waypoint.isActive)
                    {
                        playerData.hasWaypoint = true;
                        playerData.waypointPosition = new ModdedVector3V01(waypoint.position);
                        playerData.waypointMap = (byte)waypoint.mapType;
                        SRMP.Debug($"  Waypoint saved: {waypoint.mapType}");
                    }
                    else
                    {
                        playerData.hasWaypoint = false;
                    }
                }
                
                // Save player position
                if (TryGetPlayer(player, out var playerState) && playerState.worldObject != null)
                {
                    playerData.position = new ModdedVector3V01(playerState.worldObject.transform.position);
                    playerData.rotation = new ModdedVector3V01(playerState.worldObject.transform.eulerAngles);
                    SRMP.Debug($"  Position saved: {playerState.worldObject.transform.position}");
                }
                
                if (!inventorySaved)
                {
                    SRMP.Log($"⚠ Inventory data not saved (will use previous save state on reconnect)");
                }
                
                SRMP.Log($"✓ Client data saved successfully");
                SRMP.Log($"📁 Save location: HOST's computer (not client)");
                SRMP.Log($"📂 File: {savedGamePath}");
                SRMP.Log($"========================================");
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to save client data on disconnect: {ex}");
            }
        }
        public void Leave()
        {
            ammoByPlotID.Clear();
            try
            {
                systemContext.SceneLoader.LoadMainMenuSceneGroup();
            }
            catch { }
        }
        public void ClientDisconnect()
        {
            systemContext.SceneLoader.LoadMainMenuSceneGroup();
        }

        /// <summary>
        /// The send function common to both server and client.
        /// </summary>
        /// <typeparam name="P">Message struct type. Ex: 'PlayerJoinMessage'</typeparam>
        /// <param name="packet">The actual message itself. Should automatically set the M type paramater.</param>
        public static void NetworkSend<P>(P packet, ServerSendOptions serverOptions) where P : IPacket
        {
            // Check if network is available before sending
            if (EpicApplication.Instance == null || EpicApplication.Instance.Lobby == null)
                return;
            
            if (ServerActive())
            {
                if (EpicApplication.Instance.Lobby.NetworkServer == null)
                    return;
                    
                if (TryGetPlayer(serverOptions.player, out var state))
                {
                    if (serverOptions.ignoreSpecificPlayer)
                        EpicApplication.Instance.Lobby.NetworkServer.SendPacketToAll(packet, state.epicID, packetReliability:packet.Reliability);
                    else if (serverOptions.onlySendToPlayer)
                        EpicApplication.Instance.Lobby.NetworkServer.SendPacket(state.epicID, packet, packetReliability:packet.Reliability);
                    else
                        EpicApplication.Instance.Lobby.NetworkServer.SendPacketToAll(packet, packetReliability:packet.Reliability);
                }
                else
                    EpicApplication.Instance.Lobby.NetworkServer.SendPacketToAll(packet, packetReliability:packet.Reliability);
            }
            else if (ClientActive())
            {
                if (EpicApplication.Instance.Lobby.NetworkClient == null)
                    return;
                    
                EpicApplication.Instance.Lobby.NetworkClient.SendPacket(packet, packetReliability:packet.Reliability);
            }
        }

        public static void NetworkSend<P>(P packet) where P : IPacket
        {
            NetworkSend(packet, ServerSendOptions.SendToAllDefault());
        }

        public struct ServerSendOptions
        {
            public ushort player;
            public bool ignoreSpecificPlayer;
            public bool onlySendToPlayer;

            public static ServerSendOptions SendToAllDefault()
            {
                return new ServerSendOptions()
                {
                    ignoreSpecificPlayer = false,
                    onlySendToPlayer = false,
                    player = UInt16.MinValue
                };
            }
            public static ServerSendOptions SendToPlayer(ushort player)
            {
                return new ServerSendOptions()
                {
                    ignoreSpecificPlayer = false,
                    onlySendToPlayer = true,
                    player = player
                };
            }
            public static ServerSendOptions SendToAllExcept(ushort player)
            {
                return new ServerSendOptions()
                {
                    ignoreSpecificPlayer = true,
                    onlySendToPlayer = false,
                    player = player
                };
            }
        }

        /// <summary>
        /// Erases sync values.
        /// </summary>
        public static void EraseValues()
        {
            try
            {
                SRMP.Debug("Erasing multiplayer values...");
                
                foreach (var gadget in gadgets.Values)
                {
                    if (gadget != null && gadget.TryGetGameObject(out var gadgetObject))
                        DestroyGadget(gadgetObject, "SRMP.EraseValuesGadget");
                }
                foreach (var actor in actors.Values)
                {
                    if (actor != null && actor.TryGetGameObject(out var actorObject))
                        DestroyActor(actorObject, "SRMP.EraseValuesActor");
                }
                actors.Clear();
                gadgets.Clear();

                foreach (var player in players)
                {
                    // Проверяем что worldObject не null перед уничтожением
                    // (при смене сцены через портал объекты могут быть уже уничтожены)
                    if (player.worldObject != null)
                    {
                        try
                        {
                            Destroy(player.worldObject.gameObject);
                        }
                        catch (Exception ex)
                        {
                            SRMP.Debug($"Failed to destroy player object: {ex.Message}");
                        }
                    }
                }
                players.Clear();
                playerUsernames.Clear();
                playerUsernamesReverse.Clear();

                clientToGuid.Clear();

                ammoByPlotID.Clear();

                savedGame = new NetworkV01();
                savedGamePath = String.Empty;
                
                SRMP.Debug("✓ Multiplayer values erased");
            }
            catch (Exception ex)
            {
                SRMP.Error($"Error in EraseValues: {ex.Message}");
            }
        }


        public static void DoNetworkSave()
        {
            // Проверяем что путь не пустой перед сохранением
            if (string.IsNullOrEmpty(savedGamePath))
            {
                SRMP.Debug("Cannot save - savedGamePath is empty");
                return;
            }

            foreach (var player in players)
            {
                if (player.playerID == ushort.MaxValue)
                    continue;
                

                if (clientToGuid.TryGetValue(player.playerID, out var playerID))
                {
                    var ammo = GetNetworkAmmo($"player_{playerID}");
                    
                    if (player.worldObject && savedGame.savedPlayers.TryGetPlayer(playerID, out var playerFromID))
                    {
                        // Проверяем что виртуальный инвентарь существует
                        if (ammo != null && ammo.Slots != null && ammo.Slots.Count > 0)
                        {
                            try
                            {
                                List<NetworkAmmoDataV01> ammoData = SlotsToSRMPAmmoData(ammo.Slots);
                                playerFromID.ammo = ammoData;
                                SRMP.Debug($"Saved inventory for player {player.playerID}: {ammoData.Count} slots");
                            }
                            catch (Exception ex)
                            {
                                SRMP.Debug($"Failed to save inventory for player {player.playerID}: {ex.Message}");
                                // Используем существующие данные из playerFromID.ammo
                            }
                        }
                        else
                        {
                            SRMP.Debug($"Virtual inventory not found for player {player.playerID} - keeping existing data");
                        }
                        
                        playerFromID.position = new ModdedVector3V01(player.worldObject.transform.position);
                        playerFromID.rotation = new ModdedVector3V01(player.worldObject.transform.eulerAngles);
                    }
                }
                else
                {
                    SRMP.Error($"Error saving player {player.playerID}, their GUID was not found.");
                }
            }

            try
            {
                FileStream fs = File.Open(savedGamePath, FileMode.Create);
                BinaryWriter bw = new BinaryWriter(fs);
                
                savedGame.WriteData(bw);
                
                bw.Dispose();
                fs.Dispose();
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to save game: {ex.Message}");
            }
        }
    }
}
