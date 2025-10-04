namespace NewSR2MP;

public static class Timers
{
    private static float timeSyncTimer = 1f;
    private static float actorSyncTimer = 0.275f;
    private static float playerSyncTimer = 0.275f;
    private static float weatherSyncTimer = 1.0f; // Уменьшено с 2.75f для более быстрой синхронизации погоды
    private static float clientInventorySyncTimer = 5.0f; // Автосохранение инвентаря клиента каждые 5 секунд
    
    public static float WeatherTimer => weatherSyncTimer;
    public static float ActorTimer => actorSyncTimer;
    public static float PlayerTimer => playerSyncTimer;
    public static float TimeSyncTimer => timeSyncTimer;
    public static float ClientInventoryTimer => clientInventorySyncTimer;

    public enum SyncTimerType
    {
        WEATHER,
        ACTOR,
        PLAYER,
        WORLD_TIME,
    }

    internal static void SetTimer(SyncTimerType timerType, float value)
    {
        switch (timerType)
        {
            case SyncTimerType.WEATHER:
                weatherSyncTimer = value;
                return;
            case SyncTimerType.ACTOR:
                actorSyncTimer = value;
                return;
            case SyncTimerType.PLAYER:
                playerSyncTimer = value;
                return;
            case SyncTimerType.WORLD_TIME:
                timeSyncTimer = value;
                return;
        }
    }
}