using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Player;
using NewSR2MP.Packet;
using PlortCollector = Il2Cpp.PlortCollector;

namespace NewSR2MP.Patches;

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.SetFeederSpeed))]
public class SlimeFeederSetFeederSpeed
{
    public static void Postfix(SlimeFeeder __instance, SlimeFeeder.FeedSpeed speed)
    {
        if (handlingPacket) return;
        
        MultiplayerManager.NetworkSend(new FeederSetSpeedPacket()
        {
            speed = speed,
            plot = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        });
    }
}

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.StepFeederSpeed))]
public class SlimeFeederStepFeederSpeed
{
    public static void Postfix(SlimeFeeder __instance)
    {
        if (handlingPacket) return;
        MultiplayerManager.NetworkSend(new FeederSetSpeedPacket()
        {
            speed = __instance._model.feederCycleSpeed,
            plot = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        });
    }
}

[HarmonyPatch(typeof(SlimeFeeder), nameof(SlimeFeeder.EjectFood))]
public class SlimeFeederEjectFood
{
    public static void Postfix(SlimeFeeder __instance, AmmoSlotManager storageAmmo)
    {
        if (handlingPacket) return;
        MultiplayerManager.NetworkSend(new FeederDispensePacket()
        {
            nextTime = __instance._nextEject,
            plot = __instance.gameObject.GetComponentInParent<LandPlotLocation>()._id
        });
    }
}