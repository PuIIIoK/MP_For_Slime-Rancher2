using NewSR2MP.Attributes;

namespace NewSR2MP;

public partial class NetworkHandler
{
    [PacketResponse]
    private static void HandleAmmoSlot(NetPlayerState netPlayer, AmmoEditSlotPacket packet, byte channel)
    {

        try
        {
            var ammo = GetNetworkAmmo(packet.id);
            
            handlingPacket = true;
            ammo?.MaybeAddToSpecificSlot(identifiableTypes[packet.ident], null, packet.slot, packet.count);
            handlingPacket = false;
        }
        catch (Exception e)
        {
            SRMP.Error($"Error in handling inventory({packet.id})! Stack Trace:\n{e}");
        }

    }

    [PacketResponse]
    private static void HandleAmmo(NetPlayerState netPlayer, AmmoAddPacket packet, byte channel)
    {

        try
        {
            var ammo = GetNetworkAmmo(packet.id);
            
            handlingPacket = true;
            ammo?.MaybeAddToSlot(identifiableTypes[packet.ident], null, SlimeAppearance.AppearanceSaveSet.NONE);
            handlingPacket = false;
        }
        catch (Exception e)
        {
            SRMP.Error($"Error in handling inventory({packet.id})! Stack Trace:\n{e}");
        }
    }

    
    [PacketResponse]
    private static void HandleAmmoSelect(NetPlayerState netPlayer, AmmoSelectPacket packet, byte channel)
    {

        try
        {
            var ammo = GetNetworkAmmo(packet.id);
            
            handlingPacket = true;
            ammo?.SetSelectedSlot(packet.index);
            handlingPacket = false;
        }
        catch (Exception e)
        {
            SRMP.Error($"Error in handling inventory({packet.id})! Stack Trace:\n{e}");
        }
    }

    
    [PacketResponse]
    private static void HandleAmmoReverse(NetPlayerState netPlayer, AmmoRemovePacket packet, byte channel)
    {

        try
        {
            var ammo = GetNetworkAmmo(packet.id);

            handlingPacket = true;
            ammo?.Decrement(packet.index, packet.count);
            handlingPacket = false;
        }
        catch (Exception e)
        {
            SRMP.Error($"Error in handling inventory({packet.id})! Stack Trace:\n{e}");
        }
    }
}