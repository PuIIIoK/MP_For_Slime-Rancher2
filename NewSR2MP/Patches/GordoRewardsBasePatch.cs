namespace NewSR2MP.Patches;

[HarmonyPatch(typeof(GordoRewardsBase), nameof(GordoRewardsBase.GiveRewards))]
public class GordoRewardsBasePatch
{
    public static bool Prefix(GordoRewardsBase __instance)
        => __instance.gameObject.GetComponent<PreventReward>() == null;
    
}