using Il2CppMonomiPark.SlimeRancher.Weather;
using HarmonyLib;

namespace NewSR2MP.Patches
{
    // ===== БЛОКИРОВКА АВТОМАТИЧЕСКОЙ ГЕНЕРАЦИИ ПОГОДЫ НА КЛИЕНТЕ =====
    // WeatherDirector управляет погодой в текущей зоне игрока
    // На клиенте он должен ТОЛЬКО применять погоду от хоста, НЕ генерировать свою
    
    // Блокируем автоматическое обновление погоды на клиенте (FixedUpdate)
    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.FixedUpdate))]
    public class WeatherDirectorFixedUpdate
    {
        public static void Postfix(WeatherDirector __instance)
        {
            // Инициализируем weatherDirectorInstance при первом вызове
            if (weatherDirectorInstance == null)
            {
                weatherDirectorInstance = __instance;
                SRMP.Log($"✓ WeatherDirector initialized for weather sync");
            }
        }
    }
    
    // НЕ блокируем FixedUpdate полностью - он обновляет визуальные эффекты!
    // Вместо этого блокируем только изменение состояний (RunState/StopState)
    
    // Блокируем запуск состояний погоды на клиенте (кроме случаев от хоста)
    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.RunState))]
    public class WeatherDirectorRunState
    {
        public static bool Prefix()
        {
            // На клиенте разрешаем ТОЛЬКО если это от пакета хоста
            if (ClientActive() && !handlingPacket)
            {
                SRMP.Debug("Blocked RunState on client - only host can start weather state");
                return false;
            }
            
            return true;
        }
    }
    
    // Блокируем остановку состояний погоды на клиенте (кроме случаев от хоста)
    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.StopState))]
    public class WeatherDirectorStopState
    {
        public static bool Prefix()
        {
            // На клиенте разрешаем ТОЛЬКО если это от пакета хоста
            if (ClientActive() && !handlingPacket)
            {
                SRMP.Debug("Blocked StopState on client - only host can stop weather state");
                return false;
            }
            
            return true;
        }
    }
}
