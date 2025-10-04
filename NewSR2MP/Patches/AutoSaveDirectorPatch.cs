using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.Persist;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using NewSR2MP.Component;
using NewSR2MP.Packet;
using NewSR2MP.SaveModels;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


namespace NewSR2MP.Patches
{
    [HarmonyPatch(typeof(AutoSaveDirector), nameof(AutoSaveDirector.Load), typeof(GameSaveIdentifier), typeof(bool), typeof(Il2CppSystem.IO.Stream))]
    public class AutoSaveDirectorLoadSave
    {
        public static void Postfix(AutoSaveDirector __instance, GameSaveIdentifier identifier, bool reloadAllCoreScenes, Il2CppSystem.IO.Stream stream)
        {
            if (ClientActive()) return;
            MultiplayerManager.CheckForMPSavePath();
            
            var path = Path.Combine(__instance._storageProvider.Cast<FileStorageProvider>().savePath, "MultiplayerSaves", $"{identifier.GameName}.srmp");
            var networkGame = new NetworkV01();

            if (File.Exists(path))
            {
                FileStream fs = File.Open(path, FileMode.Open);
                BinaryReader br = new BinaryReader(fs);
                
                try { networkGame.ReadData(br); } catch (Exception e) { SRMP.Error($"Error loading multiplayer save file: {e}"); }
                
                br.Dispose();
                fs.Dispose();
            }
            
            savedGame = networkGame;
            savedGamePath = path;
        }
    }
    
    [HarmonyPatch(typeof(AutoSaveDirector), nameof(AutoSaveDirector.StartNewGame), typeof(int), typeof(GameSettingsModel))]
    public class AutoSaveDirectorStartNewGame
    {
        public static void Postfix(AutoSaveDirector __instance, int saveSlotIndex, GameSettingsModel gameSettingsModel)
        {
            // Для КЛИЕНТА: не создаем мультиплеерный сейв (клиент использует сейв хоста)
            if (ClientActive())
            {
                SRMP.Debug("Client skipping multiplayer save creation - using host's save");
                return;
            }
            
            // Для ХОСТА: создаем мультиплеерный сейв файл
            MultiplayerManager.CheckForMPSavePath();
            var gameName = __instance.CurrentSaveGameName();
            var path = Path.Combine(__instance._storageProvider.TryCast<FileStorageProvider>().savePath, "MultiplayerSaves", $"{gameName}.srmp");
            var networkGame = new NetworkV01();
            
            FileStream fs = File.Create(path);
            BinaryWriter bw = new BinaryWriter(fs);
            
            try { networkGame.WriteData(bw); } catch { }
            
            bw.Dispose();
            fs.Dispose();
            
            savedGame = networkGame;
            savedGamePath = path;
        }
    }
    
    [HarmonyPatch(typeof(AutoSaveDirector), nameof(AutoSaveDirector.SaveGame), typeof(bool), typeof(AutoSaveDirector.ModifySaveGameDelegate))]
    public class AutoSaveDirectorSaveGame
    {
        public static bool Prefix(AutoSaveDirector __instance, bool ignoreDebugProfileDisableSavingGame, AutoSaveDirector.ModifySaveGameDelegate modifyBeforeSerialize)
        {
            return !ClientActive();
        }
        
        public static void Postfix(AutoSaveDirector __instance, bool ignoreDebugProfileDisableSavingGame, AutoSaveDirector.ModifySaveGameDelegate modifyBeforeSerialize)
        {
            if (ClientActive()) return;
            
            try
            {
                MultiplayerManager.DoNetworkSave();
            }
            catch (Exception ex)
            {
                SRMP.Error($"Error occured during saving multiplayer data!\n{ex}");
            }
        }
    }
    
    [HarmonyPatch(typeof(AutoSaveDirector), nameof(AutoSaveDirector.Awake))]
    public class AutoSaveDirectorAwake
    {
        public static void Postfix(AutoSaveDirector __instance)
        {
            // Check if already initialized to prevent duplicate key exceptions
            if (identifiableTypes.Count > 0)
                return;
                
            var saveRef = __instance._saveReferenceTranslation;
            
            foreach (var ident in saveRef._identifiableTypeLookup)
            {
                identifiableTypes[GetIdentID(ident.Value)] = ident.Value;
            }
            foreach (var pedia in Resources.FindObjectsOfTypeAll<PediaEntry>()) // SavedGame's list doesnt include some pedia entries.
            {
                pediaEntries[pedia.name] = pedia; 
            }

            foreach (var scene in saveRef._sceneGroupTranslation.RawLookupDictionary)
            {
                var sceneId = saveRef._sceneGroupTranslation.InstanceLookupTable._reverseIndex[scene.key];
                sceneGroups[sceneId] = scene.value;
                sceneGroupsReverse[scene.key.TrimStart('S','c','e','n','e','G','r','o','u','p','.')] = sceneId;
            }
            
            CreateWeatherLookup(saveRef);
        }
    }
}
