using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.Player.CharacterController;

namespace NewSR2MP.Packet;

[HarmonyPatch(typeof(PlortEconomyDirector),nameof(PlortEconomyDirector.ResetPrices))]
public class EconomyDirectorResetPrices
{
    static bool Prefix(PlortEconomyDirector __instance)
    {
        if (ClientActive()) return false;

        return true;
    }

    static void Postfix(PlortEconomyDirector __instance)
    {
        if (!ServerActive()) return;
        
        var prices = new List<float>();
        
        foreach (var price in __instance._currValueMap)
            prices.Add(price.value.CurrValue);

        var packet = new MarketRefreshPacket
        {
            prices = prices
        };
        MultiplayerManager.NetworkSend(packet);
        SRMP.Debug($"Market Price Listing Count: {prices.Count}");
    }
}