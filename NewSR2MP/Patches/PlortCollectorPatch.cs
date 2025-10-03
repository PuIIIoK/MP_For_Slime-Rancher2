using PlortCollector = Il2Cpp.PlortCollector;

namespace NewSR2MP.Patches;

[HarmonyPatch(typeof(PlortCollector), nameof(PlortCollector.StartCollection))]
public class PlortCollectorStartCollection
{
    public static void Postfix(PlortCollector __instance)
    {
        if (handlingPacket) return;
        MultiplayerManager.NetworkSend(new PlortCollectorPacket
        {
            endTime = __instance._endCollectAt,
            plot = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        });
    }

}