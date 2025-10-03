using System;
using Il2CppMonomiPark.SlimeRancher.Weather;
using Il2CppMonomiPark.SlimeRancher.World;
using NewSR2MP.Packet;

namespace NewSR2MP.Patches
{
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.Awake))]
    public class WeatherRegistryAwake
    {
        public static void Postfix(WeatherRegistry __instance)
        {
            CreateWeatherPatternLookup(__instance);
        }
    }
    
    // ===== ПОЛНАЯ БЛОКИРОВКА ГЕНЕРАЦИИ ПОГОДЫ НА КЛИЕНТЕ =====
    // Клиент ТОЛЬКО применяет погоду от хоста, НЕ генерирует свою
    
    // Блокируем автоматическое обновление погоды на клиенте
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.Update))]
    public class WeatherRegistryUpdate
    {
        private static bool loggedOnce = false;
        
        public static bool Prefix()
        {
            // Клиент НЕ обновляет погоду автоматически
            if (ClientActive())
            {
                if (!loggedOnce)
                {
                    SRMP.Log("✓ Client weather generation BLOCKED - receiving weather from host only");
                    loggedOnce = true;
                }
                return false;
            }
            
            return true;
        }
    }
    
    // Отправляем погоду СРАЗУ при её запуске на хосте
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.RunPatternState))]
    public class WeatherRegistryRunPatternState
    {
        public static bool Prefix()
        {
            // На клиенте разрешаем ТОЛЬКО если это от пакета хоста
            if (ClientActive() && !handlingPacket)
            {
                SRMP.Debug("Blocked RunPatternState on client - only host can start weather");
                return false;
            }
            return true;
        }
        
        public static void Postfix(WeatherRegistry __instance)
        {
            // Хост отправляет обновление при изменении погоды
            if (ServerActive() && !handlingPacket)
            {
                WeatherRegistryPatchHelpers.SendWeatherUpdate(__instance);
            }
        }
    }
    
    // Отправляем погоду СРАЗУ при её остановке на хосте
    [HarmonyPatch(typeof(WeatherRegistry), nameof(WeatherRegistry.StopPatternState))]
    public class WeatherRegistryStopPatternState
    {
        public static bool Prefix()
        {
            // На клиенте разрешаем ТОЛЬКО если это от пакета хоста
            if (ClientActive() && !handlingPacket)
            {
                SRMP.Debug("Blocked StopPatternState on client - only host can stop weather");
                return false;
            }
            return true;
        }
        
        public static void Postfix(WeatherRegistry __instance)
        {
            // Хост отправляет обновление при изменении погоды
            if (ServerActive() && !handlingPacket)
            {
                WeatherRegistryPatchHelpers.SendWeatherUpdate(__instance);
            }
        }
    }
    
    // Вспомогательный класс для отправки погоды
    public static class WeatherRegistryPatchHelpers
    {
        public static void SendWeatherUpdate(WeatherRegistry registry)
        {
            try
            {
                var packet = new WeatherSyncPacket(registry._model);
                if (packet.initializedPacket)
                {
                    MultiplayerManager.NetworkSend(packet);
                    SRMP.Debug("Weather change detected - sent immediate update to clients");
                }
            }
            catch (Exception ex)
            {
                SRMP.Debug($"Failed to send immediate weather update: {ex.Message}");
            }
        }
    }
}
