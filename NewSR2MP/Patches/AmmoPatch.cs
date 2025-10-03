using Il2CppMonomiPark.SlimeRancher.Player;
using Il2CppMonomiPark.SlimeRancher.Slime;

namespace NewSR2MP.Patches
{
    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToSpecificSlot), typeof(IdentifiableType), typeof(Identifiable), typeof(int), typeof(int), typeof(bool))]
    public class AmmoMaybeAddToSpecificSlot
    {
        public static void Postfix(AmmoSlotManager __instance, ref bool __result, IdentifiableType id, Identifiable identifiable, int slotIdx, int count, bool overflow)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            // Проверяем что это зарегистрированный инвентарь (не отключенного игрока)
            var plotId = __instance.GetPlotID();
            if (plotId == null)
                return;
            
            if (__result)
            {
                var packet = new AmmoEditSlotPacket()
                {
                    ident = GetIdentID(id),
                    slot = slotIdx,
                    count = count,
                    id = plotId
                };
                
                MultiplayerManager.NetworkSend(packet);
            }
        }
    }

    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.MaybeAddToSlot), typeof(IdentifiableType), typeof(Identifiable), typeof(SlimeAppearance.AppearanceSaveSet), typeof(bool))]
    public class AmmoMaybeAddToSlot
    {
        public static void Postfix(AmmoSlotManager __instance, ref bool __result, IdentifiableType id, Identifiable identifiable, SlimeAppearance.AppearanceSaveSet appearance, bool fillVac)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            // Проверяем что это зарегистрированный инвентарь (не отключенного игрока)
            var plotId = __instance.GetPlotID();
            if (plotId == null)
                return;
            
            if (__result)
            {
                // Находим слот, куда был добавлен предмет
                int slotIDX = -1;
                for (int i = 0; i < __instance.Slots.Count; i++)
                {
                    if (__instance.Slots[i]._id == id)
                    {
                        slotIDX = i;
                        break;
                    }
                }
                
                if (slotIDX == -1) return;
                
                var packet = new AmmoEditSlotPacket
                {
                    ident = GetIdentID(id),
                    slot = slotIDX,
                    count = 1,
                    id = plotId
                };

                MultiplayerManager.NetworkSend(packet);
            }
        }
    }

    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.Decrement), typeof(int), typeof(int))]
    public class AmmoDecrement
    {
        public static void Postfix(AmmoSlotManager __instance, int index, int count)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            // Проверяем что это зарегистрированный инвентарь (не отключенного игрока)
            var plotId = __instance.GetPlotID();
            if (plotId == null)
                return;
            
            if (__instance.Slots[index]._count <= 0) __instance.Slots[index]._id = null;

            var packet = new AmmoRemovePacket()
            {
                index = index,
                count = count,
                id = plotId
            };
            
            MultiplayerManager.NetworkSend(packet);
        }
    }

    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.SetSelectedSlot), typeof(int))]
    public class AmmoSetSelectedSlot
    {
        public static void Postfix(AmmoSlotManager __instance, int idx)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            // Проверяем что это зарегистрированный инвентарь (не отключенного игрока)
            var plotId = __instance.GetPlotID();
            if (plotId == null)
                return;
            
            var packet = new AmmoSelectPacket()
            {
                index = idx,
                id = plotId
            };

            MultiplayerManager.NetworkSend(packet);
        }
    }

    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.NextAmmoSlot))]
    public class AmmoNextAmmoSlot
    {
        public static void Postfix(AmmoSlotManager __instance)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            var packet = new AmmoSelectPacket()
            {
                index = __instance._selectedAmmoIdx,
                id = __instance.GetPlotID()
            };          
            
            if (packet.id == null) return;

            MultiplayerManager.NetworkSend(packet);
        }
    }
    
    [HarmonyPatch(typeof(AmmoSlotManager), nameof(AmmoSlotManager.PrevAmmoSlot))]
    public class AmmoPrevAmmoSlot
    {
        public static void Postfix(AmmoSlotManager __instance)
        {
            if (!(ClientActive() || ServerActive()) || handlingPacket)
                return;
            
            // Don't sync player inventory - each player has their own
            if (__instance == sceneContext?.PlayerState?.Ammo)
                return;
            
            var packet = new AmmoSelectPacket()
            {
                index = __instance._selectedAmmoIdx,
                id = __instance.GetPlotID()
            };   
            if (packet.id == null) return;

            MultiplayerManager.NetworkSend(packet);
        }
    }
}
