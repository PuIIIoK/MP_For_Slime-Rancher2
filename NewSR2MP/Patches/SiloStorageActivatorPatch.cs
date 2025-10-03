using Il2CppXGamingRuntime.Interop;

namespace NewSR2MP.Patches;


[HarmonyPatch(typeof(SiloStorageActivator), nameof(SiloStorageActivator.Activate))]
public class SiloStorageActivatorActivate
{
    public static void Postfix(SiloStorageActivator __instance)
    {
        if (handlingPacket) return;
        
        MultiplayerManager.NetworkSend(new SiloSelectPacket
        {
            groupIdx = __instance.ActivatorIdx,
            id = __instance.GetComponentInParent<LandPlotLocation>().Id,
            select = (byte)__instance._landPlotModel.siloStorageIndices[__instance.ActivatorIdx]
        });
    }
}