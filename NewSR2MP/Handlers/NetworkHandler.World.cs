using System.IO;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Event;
using Il2CppMonomiPark.SlimeRancher.Map;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppMonomiPark.SlimeRancher.World;
using Il2CppMonomiPark.World;
using NewSR2MP.Attributes;
using NewSR2MP.Packet;
using NewSR2MP.SaveModels;

namespace NewSR2MP;

public partial class NetworkHandler
{
    
    [PacketResponse]
    private static void HandleDoor(NetPlayerState netPlayer, DoorOpenPacket packet, byte channel)
    {
        sceneContext.GameModel.doors[packet.id].gameObj.GetComponent<AccessDoor>().CurrState =
            AccessDoor.State.OPEN;
    }
    [PacketResponse]
    private static void HandleMoneyChange(NetPlayerState netPlayer, SetMoneyPacket packet, byte channel)
    {
        handlingPacket = true;
        
        var currency = CurrencyUtility.DefaultCurrency;
        var currentMoney = sceneContext.PlayerState.GetCurrency(currency);
        var difference = packet.newMoney - currentMoney;
        
        if (difference > 0)
            sceneContext.PlayerState.AddCurrency(currency, difference, false, null);
        else if (difference < 0)
            sceneContext.PlayerState.SpendCurrency(currency, -difference, null);
        
        handlingPacket = false;
    }

    

    [PacketResponse]
    private static void HandleTime(NetPlayerState netPlayer, TimeSyncPacket packet, byte channel)
    {
        try
        {
            TimeSmoother.Instance.nextTime = packet.time;
        }
        catch
        {
        }
    }
    
    [PacketResponse]
    private static void HandleSleep(NetPlayerState netPlayer, SleepPacket packet, byte channel)
    {

        try
        {
            sceneContext?.TimeDirector.FastForwardTo(packet.targetTime);
        }
        catch (Exception e)
        {
            SRMP.Error($"Exception in sleeping! Stack Trace:\n{e}");
        }
    }

    [PacketResponse]
    private static void HandleNavPlace(NetPlayerState netPlayer, PlaceNavMarkerPacket packet, byte channel)
    {
        // Добавляем waypoint в систему множественных waypoints
        if (MultiplayerWaypointManager.Instance != null)
        {
            MultiplayerWaypointManager.Instance.SetWaypoint(packet.playerID, packet.position, packet.map);
        }

        // Если это waypoint локального игрока - устанавливаем его в игровой системе
        if (packet.playerID == currentPlayerID)
        {
            MapDefinition map = null;
            switch (packet.map)
            {
                case MapType.RainbowIsland:
                    map = sceneContext.MapDirector._mapList._maps[0];
                    break;
                case MapType.Labyrinth:
                    map = sceneContext.MapDirector._mapList._maps[1];
                    break;
            }

            handlingNavPacket = true;
            sceneContext.MapDirector.SetPlayerNavigationMarker(packet.position, map, 0);
            handlingNavPacket = false;
        }
    }

    [PacketResponse]
    private static void HandleNavRemove(NetPlayerState netPlayer, RemoveNavMarkerPacket packet, byte channel)
    {
        // Удаляем waypoint из системы множественных waypoints
        if (MultiplayerWaypointManager.Instance != null)
        {
            MultiplayerWaypointManager.Instance.ClearWaypoint(packet.playerID);
        }

        // Если это waypoint локального игрока - удаляем его из игровой системы
        if (packet.playerID == currentPlayerID)
        {
            handlingNavPacket = true;
            sceneContext.MapDirector.ClearPlayerNavigationMarker();
            handlingNavPacket = false;
        }
    }



    [PacketResponse]
    private static void HandleWeather(NetPlayerState netPlayer, WeatherSyncPacket packet, byte channel)
    {
        if (ClientActive())
        {
            int zoneCount = (packet.sync.zones != null) ? packet.sync.zones.Count : 0;
            SRMP.Debug($"← Client received weather update with {zoneCount} zones");
        }
        MelonCoroutines.Start(WeatherHandlingCoroutine(packet));
    }


    [PacketResponse]
    private static void HandleSwitchModify(NetPlayerState netPlayer, SwitchModifyPacket packet, byte channel)
    {

        if (sceneContext.GameModel.switches.TryGetValue(packet.id, out var model))
        {
            model.state = (SwitchHandler.State)packet.state;
            if (model.gameObj)
            {
                handlingPacket = true;

                if (model.gameObj.TryGetComponent<WorldStatePrimarySwitch>(out var primary))
                    primary.SetStateForAll((SwitchHandler.State)packet.state, false);

                if (model.gameObj.TryGetComponent<WorldStateSecondarySwitch>(out var secondary))
                    secondary.SetState((SwitchHandler.State)packet.state, false);

                if (model.gameObj.TryGetComponent<WorldStateInvisibleSwitch>(out var invisible))
                    invisible.SetStateForAll((SwitchHandler.State)packet.state, false);

                handlingPacket = false;
            }
        }
        else
        {
            model = new WorldSwitchModel()
            {
                gameObj = null,
                state = (SwitchHandler.State)packet.state,
            };
            sceneContext.GameModel.switches.Add(packet.id, model);
        }
    }

    [PacketResponse]
    private static void HandleMapUnlock(NetPlayerState netPlayer, MapUnlockPacket packet, byte channel)
    {

        sceneContext.MapDirector.NotifyZoneUnlocked(GetGameEvent(packet.id), false, 0);

        var activator = Resources.FindObjectsOfTypeAll<MapNodeActivator>().FirstOrDefault(x => x._fogRevealEvent._dataKey == packet.id);

        if (activator)
            activator.StartCoroutine(activator.ActivateHologramAnimation());
        
        
        var eventDirModel = sceneContext.eventDirector._model;
        if (!eventDirModel.table.TryGetValue("fogRevealed", out var table))
        {
            eventDirModel.table.Add("fogRevealed",
                new Il2CppSystem.Collections.Generic.Dictionary<string, EventRecordModel.Entry>());
            table = eventDirModel.table["fogRevealed"];
        }

        table.TryAdd(packet.id, new EventRecordModel.Entry
        {
            count = 1,
            createdRealTime = 0,
            createdGameTime = 0,
            dataKey = packet.id,
            eventKey = "fogRevealed",
            updatedRealTime = 0,
            updatedGameTime = 0,
        });
    }

    


    [PacketResponse]
    private static void HandleTreasurePod(NetPlayerState netPlayer, TreasurePodPacket packet, byte channel)
    {
        
        var identifier = $"pod{ExtendInteger(packet.id)}";
        
        if (sceneContext.GameModel.pods.TryGetValue(identifier, out var model))
        {
            handlingPacket = true;
            model.gameObj?.GetComponent<TreasurePod>().Activate();
            handlingPacket = false;
            
            model.state = Il2Cpp.TreasurePod.State.OPEN;
        }
        else
        {
            sceneContext.GameModel.pods.Add(identifier, new TreasurePodModel
            {
                state = Il2Cpp.TreasurePod.State.OPEN,
                gameObj = null,
                spawnQueue = new Il2CppSystem.Collections.Generic.Queue<IdentifiableType>()
            });
        }
    }
    
    [PacketResponse]
    private static void HandleClientInventorySync(NetPlayerState netPlayer, ClientInventorySyncPacket packet, byte channel)
    {
        // Только хост обрабатывает этот пакет
        if (!ServerActive())
            return;
        
        try
        {
            SRMP.Log($"========== CLIENT INVENTORY SYNC ==========");
            SRMP.Log($"Connection ID: {netPlayer.playerID}");
            
            // Получаем GUID клиента
            if (!clientToGuid.TryGetValue(netPlayer.playerID, out var clientGuid))
            {
                SRMP.Log($"⚠ Cannot find GUID for connection {netPlayer.playerID}");
                return;
            }
            
            SRMP.Log($"Client GUID: {clientGuid}");
            
            // Получаем данные игрока из сохранения
            if (!savedGame.savedPlayers.TryGetPlayer(clientGuid, out var playerData))
            {
                SRMP.Log($"⚠ Cannot find player data for GUID {clientGuid}");
                return;
            }
            
            // Обновляем playerData.ammo напрямую (виртуального AmmoSlotManager больше нет)
            var networkAmmoData = new List<NetworkAmmoDataV01>();
            int itemCount = 0;
            
            foreach (var slotData in packet.inventory)
            {
                networkAmmoData.Add(new NetworkAmmoDataV01
                {
                    ident = slotData.id,
                    count = slotData.count,
                    emotionX = 0,
                    emotionY = 0,
                    emotionZ = 0,
                    emotionW = 0,
                });
                
                if (slotData.count > 0)
                {
                    itemCount++;
                    // Логируем что именно сохраняем
                    string itemName = "unknown";
                    if (identifiableTypes.ContainsKey(slotData.id))
                    {
                        itemName = identifiableTypes[slotData.id]?.name ?? "null";
                    }
                    SRMP.Debug($"  Slot {slotData.slot}: {itemName} (ID: {slotData.id}) x{slotData.count}");
                }
            }
            
            playerData.ammo = networkAmmoData;
            
            SRMP.Log($"✓ Updated playerData.ammo: {itemCount} items in {networkAmmoData.Count} slots");
            
            // ВАЖНО: Сохраняем изменения напрямую в файл
            // НЕ используем DoNetworkSave() т.к. клиент уже отключен и не в списке players!
            if (!string.IsNullOrEmpty(savedGamePath))
            {
                try
                {
                    FileStream fs = File.Open(savedGamePath, FileMode.Create);
                    BinaryWriter bw = new BinaryWriter(fs);
                    
                    savedGame.WriteData(bw);
                    
                    bw.Dispose();
                    fs.Dispose();
                    
                    SRMP.Log($"✓ Inventory saved to file: {savedGamePath}");
                }
                catch (Exception saveEx)
                {
                    SRMP.Error($"Failed to save inventory to file: {saveEx.Message}");
                }
            }
            else
            {
                SRMP.Log($"⚠ Cannot save inventory - savedGamePath is empty");
            }
            
            SRMP.Log($"===========================================");
        }
        catch (Exception ex)
        {
            SRMP.Error($"Failed to handle client inventory sync: {ex}");
        }
    }
}