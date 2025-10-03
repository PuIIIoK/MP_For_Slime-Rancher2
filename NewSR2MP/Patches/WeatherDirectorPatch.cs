using Il2CppMonomiPark.SlimeRancher.Weather;

namespace NewSR2MP.Patches
{
    // ===== БЛОКИРОВКА АВТОМАТИЧЕСКОЙ ГЕНЕРАЦИИ ПОГОДЫ НА КЛИЕНТЕ =====
    // WeatherDirector управляет погодой в текущей зоне игрока
    // На клиенте он должен ТОЛЬКО применять погоду от хоста, НЕ генерировать свою
    
    // Блокируем автоматическое обновление погоды на клиенте (FixedUpdate)
    [HarmonyPatch(typeof(WeatherDirector), nameof(WeatherDirector.FixedUpdate))]
    public class WeatherDirectorFixedUpdate
    {
        public static bool Prefix()
        {
            // Клиент НЕ обновляет погоду автоматически
            if (ClientActive())
                return false;
            
            return true;
        }
    }
    
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
