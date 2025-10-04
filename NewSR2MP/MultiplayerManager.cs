
using System.Collections;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Persist;
using Il2CppMonomiPark.SlimeRancher.Platform.Steam;
using Il2CppMonomiPark.SlimeRancher.Ranch;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.World;
using Il2CppMonomiPark.UnitPropertySystem;
using Il2CppMonomiPark.World;
using Il2CppTMPro;
using NewSR2MP.EpicSDK;
using NewSR2MP.Patches;
using SR2E;
using SR2E.Managers;
using SR2E.Menus;
using UnityEngine.Serialization;

namespace NewSR2MP
{
    [RegisterTypeInIl2Cpp(false)]
    public partial class MultiplayerManager : SRBehaviour
    {
        //public EOSLobbyGUI prototypeLobbyGUI;

        public GameObject onlinePlayerPrefab;


        GUIStyle guiStyle;

        public static MultiplayerManager Instance;

        public void Awake()
        {
            InitializeCommandExtensions();

            Instance = this;

            gameObject.AddComponent<NetworkUI>();
            gameObject.AddComponent<EpicApplication>();
            
            // Initialize NetworkHandler FIRST - critical for packet handling!
            NetworkHandler.Initialize();
            
            // Initialize MultiplayerWaypointManager singleton (not a MonoBehaviour anymore)
            var _ = MultiplayerWaypointManager.Instance;
        }
        private void Start()
        {
            SR2ECommandManager.RegisterCommand(new HostCommand());
            SR2ECommandManager.RegisterCommand(new JoinCommand());
            //SR2ECommandManager.RegisterCommand(new SplitScreenDebugCommand());
            SR2ECommandManager.RegisterCommand(new DevModifySyncTimerCommand());
            SR2ECommandManager.RegisterCommand(new ShowSRMPErrorsCommand());
            SR2ECommandManager.RegisterCommand(new GivePlayerCommand());
        }

        public void GeneratePlayerModel()
        {
            var found = GameObject.Find("BeatrixMainMenu");

            onlinePlayerPrefab = Instantiate(found);

            onlinePlayerPrefab.SetActive(false);

            var comp = onlinePlayerPrefab.AddComponent<NetworkPlayer>();
            onlinePlayerPrefab.AddComponent<TransformSmoother>();

            var text = new GameObject("Username") { transform = { parent = onlinePlayerPrefab.transform } };
            onlinePlayerPrefab.transform.localScale = Vector3.one * .85f;

            comp.enabled = false;
            comp.usernamePanel = text.AddComponent<TextMesh>();

            text.transform.localPosition = Vector3.up * 3f;
            text.transform.localEulerAngles = Vector3.up * 180f;

            DontDestroyOnLoad(onlinePlayerPrefab);
        }

        public void SetupPlayerAnimations()
        {
            var animator = sceneContext.Player.GetComponent<Animator>();

            var prefabAnim = onlinePlayerPrefab.GetComponent<Animator>();
            prefabAnim.avatar = animator.avatar;
            prefabAnim.runtimeAnimatorController = animator.runtimeAnimatorController;

            if (ClientActive())
            {
                foreach (var player in players)
                {
                    var playerAnim = player.worldObject.gameObject.GetComponent<Animator>();
                    playerAnim.avatar = animator.avatar;
                    playerAnim.runtimeAnimatorController = animator.runtimeAnimatorController;
                }
            }
        }

        public RenderTexture playerCameraPreviewImage = new RenderTexture(250, 250, 24);

        public NetworkPlayer currentPreviewRenderer;

        public void OnDestroy()
        {
            SRMP.Error("THIS SHOULD NOT APPEAR!!!!");
            SRMP.Error("SR2MP has quit unexpectedly, restart your game to play multiplayer.");
        }

        HashSet<string> getPediaEntries()
        {

            var pedias = sceneContext.PediaDirector._pediaModel.unlocked;

            var ret = new HashSet<string>();

            foreach (var pedia in pedias)
            {
                ret.Add(pedia.name);
            }

            return ret;
        }

        // Hefty code
        public static void PlayerJoin(ushort conn, Guid savingID, string username)
        {
            SRMP.Log($"========== CLIENT CONNECTION ==========");
            SRMP.Log($"Player: {username}");
            SRMP.Log($"Connection ID: {conn}");
            SRMP.Log($"GUID: {savingID}");
            SRMP.Log($"=======================================");

            // Сохраняем связь между connection ID и GUID игрока
            // Это нужно чтобы при отключении найти сохранение игрока
            if (clientToGuid.ContainsKey(conn))
            {
                SRMP.Log($"⚠ Connection {conn} already has a GUID registered! Replacing...");
                clientToGuid[conn] = savingID;
            }
            else
            {
                clientToGuid.Add(conn, savingID);
            }

            // Проверяем существует ли сохранение для этого GUID
            var newPlayer = !savedGame.savedPlayers.TryGetPlayer(savingID, out var playerData);
            if (newPlayer)
            {
                // НОВЫЙ ИГРОК - создаем пустое сохранение
                playerData = new NetPlayerV01();
                
                // Set spawn position to default spawn for new players
                playerData.position = new ModdedVector3V01(541.6466f, 18.646f, 349.3299f); // Default spawn
                playerData.rotation = new ModdedVector3V01(Vector3.up * 236.8107f);
                
                // Initialize empty inventory for new player (7 slots)
                playerData.ammo = new List<NetworkAmmoDataV01>();
                for (int slot = 0; slot < 7; slot++)
                {
                    playerData.ammo.Add(new NetworkAmmoDataV01()
                    {
                        ident = 9, // Empty slot ID
                        count = 0
                    });
                }
                
                SRMP.Log($"✓ NEW PLAYER: {username} (GUID: {savingID.ToString().Substring(0, 8)}...)");
                SRMP.Log($"  → Created empty inventory (7 slots)");

                savedGame.savedPlayers.playerList.Add(new GuidV01(savingID), playerData);
            }
            else
            {
                // ВОЗВРАЩАЮЩИЙСЯ ИГРОК - загружаем его сохранение
                int existingItems = 0;
                int totalSlots = playerData.ammo.Count;
                foreach (var ammoSlot in playerData.ammo)
                {
                    if (ammoSlot.count > 0)
                        existingItems++;
                }
                
                SRMP.Log($"✓ RETURNING PLAYER: {username} (GUID: {savingID.ToString().Substring(0, 8)}...)");
                SRMP.Log($"  → Loaded saved inventory: {existingItems} items in {totalSlots} slots");
            }

            try
            {
                // Variables
                double time = sceneContext.TimeDirector.CurrTime();
                List<InitActorData> actors = new List<InitActorData>();
                HashSet<InitGordoData> gordos = new HashSet<InitGordoData>();
                List<InitPlayerData> initPlayers = new List<InitPlayerData>();
                List<InitPlotData> plots = new List<InitPlotData>();
                List<InitSwitchData> switches = new List<InitSwitchData>();
                List<string> pedias = new List<string>();

                foreach (var pedia in sceneContext.PediaDirector._pediaModel.unlocked)
                {
                    pedias.Add(pedia.name);
                }

                var upgrades = new Dictionary<byte, sbyte>();
                var upgModel = sceneContext.PlayerState._model.upgradeModel;
                foreach (var upg in upgModel.upgradeDefinitions.items)
                {
                    upgrades.Add((byte)upg._uniqueId, (sbyte)upgModel.GetUpgradeLevel(upg));
                }

                // Actors
                foreach (var typeDict in sceneContext.GameModel.identifiablesByIdent)
                foreach (var a in typeDict.value)
                {
                    try
                    {
                        
                        var data = new InitActorData()
                        {
                            id = a.actorId.Value,
                            ident = GetIdentID(a.ident),
                            pos = a.lastPosition,
                            scene = sceneGroupsReverse[a.sceneGroup.name],
                            rot = a.GetActorRotationFromModel()
                        };
                        
                        var gadget = a.TryCast<GadgetModel>();
                        if (gadget != null)
                        {
                            data.gadgetType = InitActorData.GadgetType.BASIC;

                            data.constructionEndTime = gadget.waitForChargeupTime;
                            
                            var teleporter = gadget.TryCast<TeleporterGadgetModel>();
                            if (teleporter != null)
                            {
                                if (teleporter.GetLinkedGadget() != null)
                                {
                                    data.linkedId = teleporter.GetLinkedGadget().actorId.Value;
                                }

                                data.gadgetType = InitActorData.GadgetType.LINKED_NO_AMMO;
                            }

                            var warpDepot = gadget.TryCast<WarpDepotModel>();
                            if (warpDepot != null)
                            {
                                data.gadgetType = InitActorData.GadgetType.LINKED_WITH_AMMO;
                                if (warpDepot.GetLinkedGadget() != null)
                                {
                                    data.linkedId = warpDepot.GetLinkedGadget().actorId.Value;
                                }
                                HashSet<AmmoData> ammo = new HashSet<AmmoData>();
                                for (var idx = 0; idx < warpDepot.ammo.Slots.Count; idx++)
                                {
                                    var slot = warpDepot.ammo.Slots[idx];
                                    if (slot != null)
                                    {
                                        var ammoSlot = new AmmoData()
                                        {
                                            slot = idx,
                                            id = GetIdentID(slot._id),
                                            count = slot.Count,
                                        };
                                        ammo.Add(ammoSlot);
                                    }
                                    else
                                    {
                                        var ammoSlot = new AmmoData()
                                        {
                                            slot = idx,
                                            id = 9,
                                            count = 0,
                                        };
                                        ammo.Add(ammoSlot);
                                    }
                                }

                                data.ammo = new InitSiloData
                                {
                                    slots = warpDepot.ammo.Slots.Count,
                                    ammo = ammo
                                };
                            }
                        }
                        
                        if (actors.FirstOrDefault(x => x == data) == null)
                            actors.Add(data);
                    }
                    catch
                    {
                    }
                }



                // Gordos
                foreach (var g in sceneContext.GameModel.gordos)
                {
                    gordos.Add(new InitGordoData()
                    {
                        id = int.Parse(g.key.Replace("gordo","")),
                        eaten = g.value.GordoEatenCount,
                        ident = GetIdentID(g.value.identifiableType),
                        targetCount = g.value.targetCount,
                    });
                }

                // Current Players
                foreach (var player in players)
                {
                    if (player.playerID != 65535 && player.playerID != conn)
                    {

                        var p = new InitPlayerData()
                        {
                            id = player.playerID,
                            username = playerUsernamesReverse[player.playerID]
                        };
                        initPlayers.Add(p);
                    }
                }

                var p2 = new InitPlayerData()
                {
                    id = 65535,
                    username = Main.data.Username
                };
                initPlayers.Add(p2);



                // Plots
                foreach (var landplot in sceneContext.GameModel.landPlots)
                {
                    var plot = landplot.value;
                    try
                    {
                        Dictionary<string, InitSiloData> silos = new();
                        foreach (var siloAmmo in plot.siloAmmo)
                        {
                            // Silos

                            InitSiloData s = new InitSiloData
                            {
                                ammo = new HashSet<AmmoData>()
                            }; // Empty


                            HashSet<AmmoData> ammo = new HashSet<AmmoData>();
                            for (var idx = 0; idx < siloAmmo.value.Slots.Count; idx++)
                            {
                                var slot = siloAmmo.value.Slots[idx];
                                if (slot != null)
                                {
                                    var ammoSlot = new AmmoData()
                                    {
                                        slot = idx,
                                        id = GetIdentID(slot._id),
                                        count = slot.Count,
                                    };
                                    ammo.Add(ammoSlot);
                                }
                                else
                                {
                                    var ammoSlot = new AmmoData()
                                    {
                                        slot = idx,
                                        id = 9,
                                        count = 0,
                                    };
                                    ammo.Add(ammoSlot);
                                }
                            }

                            s = new InitSiloData
                            {
                                slots = siloAmmo.value.Slots.Count,
                                ammo = ammo
                            };
                            
                            silos.Add(GetScriptableObjectByGuid<PlotAmmoSetDefinition>(siloAmmo.Key).name, s);
                        }

                        int cropIdent = 9;
                        if (plot.resourceGrowerDefinition != null)
                        {
                            cropIdent = GetIdentID(plot.resourceGrowerDefinition._primaryResourceType);
                        }

                        int[] siloSlotSelections = new int[4];
                        landplot.value.siloStorageIndices.CopyTo(siloSlotSelections, 0);
                        
                        var p = new InitPlotData()
                        {
                            id = int.Parse(landplot.key.Replace("plot","")),
                            type = plot.typeId,
                            upgrades = plot.upgrades,
                            
                            cropIdent = cropIdent,

                            siloSlotSelections = siloSlotSelections,
                            siloData = silos,
                            
                            feederSpeed = plot.feederCycleSpeed
                        };
                        plots.Add(p);
                    }
                    catch (Exception ex)
                    {
                        SRMP.Error($"Landplot failed to send! This will cause major desync.\n{ex}");
                    }
                }

                // Slime Gates || Ranch expansions
                List<InitAccessData> access = new List<InitAccessData>();
                foreach (var accessDoor in sceneContext.GameModel.doors)
                {
                    access.Add(new InitAccessData()
                    {
                        open = (accessDoor.Value.state == AccessDoor.State.OPEN),
                        id = int.Parse(accessDoor.key.Replace("door",""))
                    });
                }

                List<AmmoData> playerAmmoData = new List<AmmoData>();
                int i = 0;
                int clientInventoryItems = 0;
                foreach (var ammoSlot in playerData.ammo)
                {
                    var playerSlot = new AmmoData()
                    {
                        slot = i,
                        id = ammoSlot.ident,
                        count = ammoSlot.count,
                    };
                    playerAmmoData.Add(playerSlot);
                    if (ammoSlot.count > 0)
                        clientInventoryItems++;
                    i++;
                }

                SRMP.Log($"→ Sending to client {username}: inventory with {clientInventoryItems} items in {playerAmmoData.Count} slots");
                
                LocalPlayerData localPlayerData = new LocalPlayerData()
                {
                    pos = playerData.position.value,
                    rot = playerData.rotation.value,
                    ammo = playerAmmoData,
                    sceneGroup = playerData.sceneGroup,
                    hasSeenIntro = playerData.hasSeenIntro,
                    hasWaypoint = playerData.hasWaypoint,
                    waypointPosition = playerData.waypointPosition.value,
                    waypointMap = playerData.waypointMap
                };


                // First time ever coding a local function.... its not good, shouldve just used a normal one
                List<string> GetListFromFogEvents(
                    Il2CppSystem.Collections.Generic.Dictionary<string, EventRecordModel.Entry> events)
                {
                    var ret = new List<string>();
                    foreach (var e in events)
                        ret.Add(e.key);
                    return ret;
                }

                List<string> fogEvents = new List<string>();
                if (sceneContext.eventDirector._model.table.TryGetValue("fogRevealed", out var table))
                    fogEvents = GetListFromFogEvents(table);


                var money = sceneContext.PlayerState.GetCurrency(CurrencyUtility.DefaultCurrency);

                var prices = new List<float>();
                foreach (var price in sceneContext.PlortEconomyDirector._currValueMap)
                    prices.Add(price.value.CurrValue);

                foreach (var sw in sceneContext.GameModel.switches)
                    switches.Add(new InitSwitchData
                    {
                        id = sw.key,
                        state = (byte)sw.Value.state,
                    });

                Dictionary<int, int> refineryItems = new Dictionary<int, int>();
                foreach (var item in sceneContext.GadgetDirector._model._itemCounts)
                    refineryItems.Add(GetIdentID(item.Key), item.Value);

                Dictionary<int, TreasurePod.State> pods = new();
                foreach (var pod in sceneContext.GameModel.pods)
                {
                    pods.Add(int.Parse(pod.key.Replace("pod","")), pod.value.state);
                }

                // Progress tracking - пока недоступен через API
                List<string> progressUnlocks = new List<string>();
                List<string> completedTutorials = new List<string>();
                bool hasCompletedFTE = false;
                
                // Send save data.
                var saveMessage = new LoadPacket()
                {
                    initActors = actors,
                    initPlayers = initPlayers,
                    initPlots = plots,
                    initGordos = gordos,
                    initPedias = pedias,
                    initAccess = access, initPods = pods, initSwitches = switches, money = money, time = time,
                    initMaps = fogEvents, playerID = conn, localPlayerSave = localPlayerData, upgrades = upgrades,
                    marketPrices = prices, refineryItems = refineryItems,
                    initProgress = progressUnlocks,
                    initTutorials = completedTutorials,
                    hasCompletedFirstTimeExperience = hasCompletedFTE,
                };

                NetworkSend(saveMessage, ServerSendOptions.SendToPlayer(conn));
                SRMP.Debug("The world data has been sent to the client!");
                
                // Отправляем текущую погоду клиенту сразу при подключении
                try
                {
                    var weatherPacket = new WeatherSyncPacket(sceneContext.WeatherRegistry._model);
                    if (weatherPacket.initializedPacket)
                    {
                        NetworkSend(weatherPacket, ServerSendOptions.SendToPlayer(conn));
                        SRMP.Log($"→ Sent initial weather state to {username}");
                    }
                }
                catch (Exception weatherEx)
                {
                    SRMP.Debug($"Failed to send initial weather to client: {weatherEx.Message}");
                }

            }
            catch (Exception ex)
            {
                clientToGuid.Remove(conn);
                SRMP.Error(ex.ToString());
            }

            // ===== УПРОЩЕННАЯ СИСТЕМА БЕЗ ВИРТУАЛЬНОГО AmmoSlotManager =====
            // Инвентарь клиента хранится ТОЛЬКО как List<NetworkAmmoDataV01> в playerData.ammo
            // НЕ создаем виртуальный AmmoSlotManager - это избыточно и вызывает проблемы
            // Вместо этого работаем напрямую с данными:
            // 1. При подключении - отправляем playerData.ammo клиенту в LoadPacket
            // 2. При отключении - получаем ClientInventorySyncPacket и обновляем playerData.ammo
            // 3. При сохранении - сохраняем playerData.ammo в файл
            
            SRMP.Log($"========== CLIENT INVENTORY SETUP ==========");
            
            // Подготавливаем данные инвентаря
            if (playerData.ammo.Count < 7)
            {
                // Новый игрок - создаем пустой инвентарь
                playerData.ammo = new List<NetworkAmmoDataV01>();
                for (var i = 0; i < 7; i++)
                {
                    playerData.ammo.Add(new NetworkAmmoDataV01()
                    {
                        count = 0,
                        ident = 9,
                        emotionX = 0,
                        emotionY = 0,
                        emotionZ = 0,
                        emotionW = 0,
                    });
                }
                SRMP.Debug($"  Created empty inventory (7 slots)");
            }
            
            int itemCount = playerData.ammo.Count(x => x.count > 0);
            SRMP.Log($"✓ Inventory ready: {itemCount} items in {playerData.ammo.Count} slots");
            SRMP.Log($"  Inventory will be sent to client in LoadPacket");
            SRMP.Log($"============================================");

        }


        /// <summary>
        /// Shows SRMP errors on networking related stuff.
        /// </summary>
        public void ShowSRMPErrors()
        {
            ShowErrors = true;
        }

        public static void ClientLeave()
        {
            // Отправляем инвентарь хосту перед выходом
            if (ClientActive() && !ServerActive())
            {
                MelonCoroutines.Start(ClientLeaveCoroutine());
            }
            else
            {
                systemContext.SceneLoader.LoadSceneGroup(systemContext.SceneLoader._mainMenuSceneGroup);
            }
        }
        
        /// <summary>
        /// Корутина для плавного выхода клиента с сохранением инвентаря
        /// </summary>
        private static IEnumerator ClientLeaveCoroutine()
        {
            SRMP.Log("========== CLIENT LEAVING ==========");
            
            // 1. Отправляем финальный инвентарь хосту
            SendClientInventoryToHost();
            
            // 2. Короткая задержка для отправки пакета
            yield return null;
            
            SRMP.Log("✓ Final inventory sent, disconnecting...");
            
            // 3. Выходим в главное меню
            systemContext.SceneLoader.LoadSceneGroup(systemContext.SceneLoader._mainMenuSceneGroup);
            
            SRMP.Log("====================================");
        }
        
        /// <summary>
        /// Sends client's current inventory to the host before disconnecting
        /// </summary>
        private static void SendClientInventoryToHost()
        {
            try
            {
                if (sceneContext == null || sceneContext.PlayerState == null || sceneContext.PlayerState.Ammo == null)
                {
                    SRMP.Log("⚠ Cannot send inventory - scene context not available");
                    return;
                }
                
                SRMP.Log($"========== SENDING CLIENT INVENTORY ==========");
                
                var clientAmmo = sceneContext.PlayerState.Ammo;
                var inventoryData = new List<AmmoData>();
                
                int itemCount = 0;
                for (int i = 0; i < clientAmmo.Slots.Count; i++)
                {
                    var slot = clientAmmo.Slots[i];
                    int identId = (slot._id == null) ? -1 : GetIdentID(slot._id);
                    
                    inventoryData.Add(new AmmoData
                    {
                        slot = i,
                        id = identId,
                        count = slot._count
                    });
                    
                    if (slot._count > 0)
                    {
                        itemCount++;
                        string itemName = (slot._id != null) ? slot._id.name : "empty";
                        SRMP.Log($"  Slot {i}: {itemName} (ID: {identId}) x{slot._count}");
                    }
                    else
                    {
                        SRMP.Debug($"  Slot {i}: empty (ID: {identId})");
                    }
                }
                
                var packet = new ClientInventorySyncPacket
                {
                    inventory = inventoryData
                };
                
                NetworkSend(packet);
                
                SRMP.Log($"✓ Sent inventory to host: {itemCount} items in {inventoryData.Count} slots");
                SRMP.Log($"==============================================");
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to send inventory to host: {ex}");
            }
        }

        public void Connect(string lobby)
        {
            if (ServerActive())
            {
                SRMP.Error("You can't join a server while hosting!");
                return;
            }
            
            EpicApplication.Instance.Lobby.JoinLobby(lobby);
        }

        public static void BeginWaitingForSaveData() => Instance.waitingForSave = true;
        
        
        bool waitingForSave = false;
        bool waitingForSceneLoad = false;

        // Store client's inventory before joining
        private static List<Il2CppMonomiPark.SlimeRancher.Player.AmmoSlot> savedClientAmmo = null;
        
        bool WaitForSaveData()
        {
            if (!waitingForSave) return false;
            if (latestSaveJoined == null) return false;
            if (!waitingForSceneLoad)
            {
                if (latestSaveJoined?.localPlayerSave == null)
                {
                    //SRMP.Error("Failed to get the client's player data from save!");
                    //Shutdown();
                    return false;
                }

                // Save client's inventory before starting new game
                try
                {
                    if (sceneContext?.PlayerState?.Ammo != null)
                    {
                        var currentAmmo = sceneContext.PlayerState.Ammo;
                        savedClientAmmo = new List<Il2CppMonomiPark.SlimeRancher.Player.AmmoSlot>();
                        
                        for (int i = 0; i < currentAmmo.Slots.Count; i++)
                        {
                            var currentSlot = currentAmmo.Slots[i];
                            var newSlot = new Il2CppMonomiPark.SlimeRancher.Player.AmmoSlot();
                            newSlot._id = currentSlot._id;
                            newSlot._count = currentSlot._count;
                            newSlot.Emotions = currentSlot.Emotions;
                            newSlot.Appearance = currentSlot.Appearance;
                            newSlot.Definition = currentSlot.Definition;
                            newSlot._maxCountValue = currentSlot._maxCountValue;
                            newSlot._isUnlockedValue = currentSlot._isUnlockedValue;
                            savedClientAmmo.Add(newSlot);
                        }
                        
                        SRMP.Debug("Saved client's inventory!");
                    }
                }
                catch (Exception ex)
                {
                    SRMP.Error($"Failed to save client inventory: {ex}");
                }

                // Use StartNewGame instead of SavedGame.CreateNew
                gameContext.AutoSaveDirector.StartNewGame(-1, CreateEmptyGameSettingsModel());

                systemContext.SceneLoader.LoadSceneGroup(sceneGroups[latestSaveJoined.localPlayerSave.sceneGroup]);
                waitingForSceneLoad = true;
                return false;
            }

            clientLoading = true;
            
            handlingPacket = true;

            if (systemContext.SceneLoader.IsSceneLoadInProgress) return false;
            
            handlingPacket = false;

            isJoiningAsClient = true;


            SRMP.Debug("Received the save data!");

            ammoByPlotID.Clear();

            MelonCoroutines.Start(Main.OnSaveLoaded());
            
            // Always restore client state (inventory and tutorials)
            MelonCoroutines.Start(RestoreClientInventory());

            isJoiningAsClient = false;
            waitingForSceneLoad = false;

            return true;
        }
        
        private static IEnumerator RestoreClientInventory()
        {
            yield return null;
            yield return null;
            
            try
            {
                // ВАЖНО: НЕ перезаписываем инвентарь, который пришел от хоста!
                // Инвентарь уже был восстановлен в Main.OnSaveLoaded() из save.localPlayerSave.ammo
                // savedClientAmmo - это инвентарь из главного меню, который НЕ нужен
                
                // Очищаем savedClientAmmo, чтобы не было утечки памяти
                if (savedClientAmmo != null)
                {
                    SRMP.Debug("Cleared savedClientAmmo (not used - host inventory takes priority)");
                    savedClientAmmo = null;
                }
                
                // Инвентарь клиента уже правильно восстановлен из save.localPlayerSave.ammo
                // который был отправлен хостом с сохраненным инвентарем этого клиента
                
                if (sceneContext?.PlayerState?.Ammo != null)
                {
                    int itemCount = 0;
                    foreach (var slot in sceneContext.PlayerState.Ammo.Slots)
                    {
                        if (slot != null && slot._count > 0)
                            itemCount++;
                    }
                    SRMP.Log($"✓ Client inventory restored from host: {itemCount} items");
                }
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed in RestoreClientInventory: {ex}");
            }
            
            yield return null;
            
            try
            {
                if (ClientActive())
                {
                    // Убираем возможный черный экран и замораживание
                    try
                    {
                        // Восстанавливаем нормальную скорость времени
                        Time.timeScale = 1.0f;
                        
                        // Убеждаемся что игрок может двигаться
                        if (sceneContext?.player != null)
                        {
                            var playerController = sceneContext.player.GetComponent<Il2CppMonomiPark.SlimeRancher.Player.CharacterController.SRCharacterController>();
                            if (playerController != null)
                            {
                                playerController.enabled = true;
                                SRMP.Debug("Client player controller enabled");
                            }
                        }
                    }
                    catch (Exception stateEx)
                    {
                        SRMP.Debug($"Player state setup (non-critical): {stateEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to setup client state: {ex}");
            }
            
            // Restore waypoint if player had one
            yield return null;
            
            try
            {
                if (latestSaveJoined?.localPlayerSave != null && latestSaveJoined.localPlayerSave.hasWaypoint)
                {
                    var playerSave = latestSaveJoined.localPlayerSave;
                    
                    // Restore waypoint через систему маяков
                    if (MultiplayerWaypointManager.Instance != null)
                    {
                        MultiplayerWaypointManager.Instance.SetWaypoint(
                            (ushort)currentPlayerID,
                            playerSave.waypointPosition,
                            (MapType)playerSave.waypointMap
                        );
                        
                        // Также установим в игровой системе
                        MapDefinition map = null;
                        switch ((MapType)playerSave.waypointMap)
                        {
                            case MapType.RainbowIsland:
                                map = sceneContext.MapDirector._mapList._maps[0];
                                break;
                            case MapType.Labyrinth:
                                map = sceneContext.MapDirector._mapList._maps[1];
                                break;
                        }
                        
                        if (map != null)
                        {
                            handlingNavPacket = true;
                            sceneContext.MapDirector.SetPlayerNavigationMarker(playerSave.waypointPosition, map, 0);
                            handlingNavPacket = false;
                        }
                        
                        SRMP.Debug($"Restored client waypoint at {playerSave.waypointPosition}");
                    }
                }
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to restore waypoint: {ex}");
            }
        }
        
        /// <summary>
        /// Disable tutorials for clients who have already seen them
        /// </summary>
        private static void DisableTutorialsForClient(Guid clientGuid)
        {
            try
            {
                // Check if this client has completed tutorials before
                if (savedGame.savedPlayers.TryGetPlayer(clientGuid, out var playerData) && 
                    playerData.tutorialsCompleted)
                {
                    if (sceneContext?.TutorialDirector != null)
                    {
                        // Cancel all active tutorials
                        if (sceneContext.TutorialDirector.CurrentTutorial != null)
                        {
                            sceneContext.TutorialDirector.CancelTutorial(sceneContext.TutorialDirector.CurrentTutorial);
                        }
                        
                        // Hide tutorial popup if visible
                        sceneContext.TutorialDirector.HideTutorialPopup();
                        
                        // Suppress all tutorials using this object as the requester
                        var suppressRequester = new Il2CppSystem.Object();
                        sceneContext.TutorialDirector.SuppressTutorials(suppressRequester);
                        
                        SRMP.Debug($"Disabled tutorials for returning client: {clientGuid}");
                    }
                }
                else
                {
                    SRMP.Debug($"Client {clientGuid} seeing tutorials for the first time");
                }
            }
            catch (Exception ex)
            {
                SRMP.Error($"Failed to disable tutorials: {ex}");
            }
        }

        public void OnConnectionSuccessful(object? sender, EventArgs args)
        {
            waitingForSave = true;
        }

        public void OnClientDisconnect(object? sender, EventArgs args)
        {
            systemContext.SceneLoader.LoadMainMenuSceneGroup();
            Shutdown();
        }

        public void OnClientConnectionFail(object? sender, EventArgs args)
        {
            Shutdown();
        }

        public void Host()
        {
            if (!SystemContext.Instance.SceneLoader.IsCurrentSceneGroupGameplay())
            {
                SRMP.Error("You can't host a server while not being in a world!");
                return;
            }

            if (ClientActive())
            {
                SRMP.Error("You can't host a server while in one!");
                return;
            }

            EpicApplication.Instance.Lobby.CreateLobby();
        }

        public bool loadingZone = false;

        

        private float networkUpdateInterval = .15f;
        private float nextNetworkUpdate = -1f;

        public int sceneLoadingFrameCounter = -1;

        void Update()
        {

            if (WaitForSaveData())
            {
                waitingForSave = false;
            }

            if (WaitForZoneLoad())
            {
                loadingZone = false;
            }

            if (systemContext.SceneLoader.IsSceneLoadInProgress)
                if (sceneLoadingFrameCounter >= 8)
                    loadingZone = true;
                else
                    sceneLoadingFrameCounter++;
            else
                sceneLoadingFrameCounter = 0;
            
            
        }

        bool WaitForZoneLoad()
        {
            if (!loadingZone)
                return false;
            if (systemContext.SceneLoader.IsSceneLoadInProgress)
                return false;
            if (!systemContext.SceneLoader._previousGroup._isGameplay)
                return false;
            if (!systemContext.SceneLoader._currentSceneGroup._isGameplay)
                return false;
            
            IEnumerable<Il2CppSystem.Collections.Generic.Dictionary<ActorId, IdentifiableModel>.Entry> actors = null;

            if (ClientActive())
                actors = sceneContext.GameModel.identifiables._entries.Where(x =>
                    x != null &&
                    x.value != null &&
                    x.value.TryCast<ActorModel>() != null &&
                    x.value.sceneGroup == systemContext.SceneLoader._currentSceneGroup);
            else if (ServerActive())
                actors = sceneContext.GameModel.identifiables._entries.Where(x =>
                    x != null &&
                    x.value != null &&
                    x.value.TryCast<ActorModel>() != null &&
                    x.value.sceneGroup == systemContext.SceneLoader._currentSceneGroup &&
                    x.value.Transform == null);
            else
                return true;

            MelonCoroutines.Start(LoadZoneActors(actors.ToList()));

            return true;
        }

        IEnumerator LoadZoneActors(
            List<Il2CppSystem.Collections.Generic.Dictionary<ActorId, IdentifiableModel>.Entry> actorEntries)
        {
            int yeildCounter = 0;
            int i = 0;
            foreach (var t in actorEntries)
            {
                handlingPacket = true;
                var actor = InstantiateActorFromModel(t.value.Cast<ActorModel>());
                handlingPacket = false;
                actor.transform.position = t.value.lastPosition;

                yeildCounter++;
                i++;
                if (i >= actorEntries.Count)
                    break;
                if (yeildCounter == 50)
                {
                    yeildCounter = 0;
                    yield return null;
                }
            }
        }

        public static void Shutdown()
        {
            EpicApplication.Instance.Lobby.Shutdown();
        }
    }
}

