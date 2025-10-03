using HarmonyLib;

using NewSR2MP;
using NewSR2MP.Component;
using NewSR2MP.Packet;
using System;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.UI.Map;
using Il2CppMonomiPark.SlimeRancher.Economy;
using Il2CppMonomiPark.SlimeRancher.UI.Framework.Data;

namespace NewSR2MP.Patches
{
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.AddCurrency))]
    internal class PlayerStateAddCurrency
    {
        public static void Postfix(PlayerState __instance, ICurrency currencyDefinition, int adjust, bool showUiNotification, IUIDisplayData sourceOfChange)
        {
            if (handlingPacket)
                return;
            
            if (ClientActive() || ServerActive())
            {
                SetMoneyPacket message = new SetMoneyPacket()
                {
                    newMoney = __instance.GetCurrency(currencyDefinition)
                };
                MultiplayerManager.NetworkSend(message);
            }
        }
    }
    
    [HarmonyPatch(typeof(PlayerState), nameof(PlayerState.SpendCurrency))]
    internal class PlayerStateSpendCurrency
    {
        public static void Postfix(PlayerState __instance, ICurrency currency, int adjust, IUIDisplayData sourceOfChange)
        {
            if (handlingPacket)
                return;
            
            if (ClientActive() || ServerActive())
            {
                SetMoneyPacket message = new SetMoneyPacket()
                {
                    newMoney = __instance.GetCurrency(currency)
                };
                MultiplayerManager.NetworkSend(message);
            }
        }
    }
    
}
