using HarmonyLib;
using Il2CppMonomiPark.SlimeRancher.UI.Map;

[HarmonyPatch(typeof(MapNodeActivator), nameof(MapNodeActivator.Activate))]
internal class MapNodeActivatorActivate
{
    public static void Postfix(MapNodeActivator __instance)
    {
        if (ClientActive() || ServerActive())
        {
            MapUnlockPacket packet = new MapUnlockPacket()
            {
                id = __instance._fogRevealEvent._dataKey
            };
            MultiplayerManager.NetworkSend(packet);
        }
    }
}