
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
            
            DoNetworkSave();
            
            try
            {
                if (!TryGetPlayer(player, out var state))
                    return;
                
                Destroy(state.worldObject);
                players.Remove(state);
                playerUsernames.Remove(playerUsernamesReverse[state.playerID]);
                playerUsernamesReverse.Remove(player);
                players.Remove(state);
                clientToGuid.Remove(player);
            }
            catch { }

        }
        
        /// <summary>
        /// Save client's inventory and tutorial state when they disconnect
        /// </summary>
        private static void SaveClientDataOnDisconnect(ushort player)
        {
            try
            {
                // Get the client's GUID
                if (!clientToGuid.TryGetValue(player, out var clientGuid))
                    return;
                
                // Get the player data from saved game
                if (!savedGame.savedPlayers.TryGetPlayer(clientGuid, out var playerData))
                    return;
                
                // Find the client's ammo by their player pointer
                string playerPointer = $"player_{clientGuid}";
                IntPtr clientAmmoPointer = IntPtr.Zero;
                
                foreach (var ammoEntry in ammoByPlotID.ToList()) // ToList() to avoid modification during iteration
                {
                    if (ammoPointersToPlotIDs.TryGetValue(ammoEntry.Value.Pointer, out var plotId) &&
                        plotId == playerPointer)
                    {
                        // Сохраняем виртуальный инвентарь клиента с хоста
                        var clientAmmo = ammoEntry.Value;
                        clientAmmoPointer = clientAmmo.Pointer;
                        
                        // Подсчитываем непустые слоты для логирования
                        int savedItemsCount = 0;
                        foreach (var slot in clientAmmo.Slots)
                        {
                            if (slot != null && slot._count > 0)
                                savedItemsCount++;
                        }
                        
                        playerData.ammo = SlotsToSRMPAmmoData(clientAmmo.Slots.ToArray());
                        
                        // Mark tutorials as completed (client has seen the world)
                        playerData.tutorialsCompleted = true;
                        
                        // Save waypoint if exists
                        if (MultiplayerWaypointManager.Instance != null)
                        {
                            var waypoint = MultiplayerWaypointManager.Instance.GetWaypoint(player);
                            if (waypoint != null && waypoint.isActive)
                            {
                                playerData.hasWaypoint = true;
                                playerData.waypointPosition = new ModdedVector3V01(waypoint.position);
                                playerData.waypointMap = (byte)waypoint.mapType;
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
                        }
                        
                        SRMP.Log($"✓ Saved client inventory: {savedItemsCount} items in {playerData.ammo.Count} slots");
                        SRMP.Debug($"Client data saved - tutorials: {playerData.tutorialsCompleted}, waypoint: {playerData.hasWaypoint}");
                        
                        // ВАЖНО: Удалить виртуальный инвентарь клиента из системы после сохранения
                        // Это предотвращает конфликт с инвентарем хоста
                        ammoByPlotID.Remove(playerPointer);
                        SRMP.Debug($"✓ Cleaned up virtual inventory: {playerPointer}");
                        break;
                    }
                }
                
                // Удалить pointer клиента из lookup таблицы
                if (clientAmmoPointer != IntPtr.Zero && ammoPointersToPlotIDs.ContainsKey(clientAmmoPointer))
                {
                    ammoPointersToPlotIDs.Remove(clientAmmoPointer);
                    SRMP.Debug($"Removed client ammo pointer from lookup table");
                }
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
            foreach (var gadget in gadgets.Values)
            {
                if (gadget.TryGetGameObject(out var gadgetObject))
                    DestroyGadget(gadgetObject, "SRMP.EraseValuesGadget");
            }
            foreach (var actor in actors.Values)
            {
                if (actor.TryGetGameObject(out var actorObject))
                    DestroyActor(actorObject, "SRMP.EraseValuesActor");
            }
            actors.Clear();
            gadgets.Clear();

            foreach (var player in players)
            {
                Destroy(player.worldObject.gameObject);
            }
            players.Clear();
            playerUsernames.Clear();
            playerUsernamesReverse.Clear();

            clientToGuid.Clear();

            ammoByPlotID.Clear();

            savedGame = new NetworkV01();
            savedGamePath = String.Empty;
        }


        public static void DoNetworkSave()
        {
            foreach (var player in players)
            {
                if (player.playerID == ushort.MaxValue)
                    continue;
                

                if (clientToGuid.TryGetValue(player.playerID, out var playerID))
                {
                    var ammo = GetNetworkAmmo($"player_{playerID}");
                    
                    if (player.worldObject && savedGame.savedPlayers.TryGetPlayer(playerID, out var playerFromID) )
                    {
                        List<NetworkAmmoDataV01> ammoData = SlotsToSRMPAmmoData(ammo.Slots);
                        playerFromID.ammo = ammoData;
                        
                        playerFromID.position = new ModdedVector3V01(player.worldObject.transform.position);
                        playerFromID.rotation = new ModdedVector3V01(player.worldObject.transform.eulerAngles);
                    }
                }
                else
                {
                    SRMP.Error($"Error saving player {player.playerID}, their GUID was not found.");
                }
            }

            FileStream fs = File.Open(savedGamePath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            
            savedGame.WriteData(bw);
            
            bw.Dispose();
            fs.Dispose();
        }
    }
}
