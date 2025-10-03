using NewSR2MP.Attributes;

namespace NewSR2MP;

public partial class NetworkHandler
{
    
    [PacketResponse]
    private static void HandlePedia(NetPlayerState netPlayer, PediaPacket packet, byte channel)
    {
        if (!pediaEntries.TryGetValue(packet.id, out var pediaEntry))
        {
            SRMP.Error($"Pedia entry not found: {packet.id}");
            return;
        }

        handlingPacket = true;
        sceneContext.PediaDirector.Unlock(pediaEntry);
        handlingPacket = false;
    }
    [PacketResponse]
    private static void HandlePlayerUpgrade(NetPlayerState netPlayer, PlayerUpgradePacket packet, byte channel)
    {

        handlingPacket = true;
        sceneContext.PlayerState._model.upgradeModel.IncrementUpgradeLevel(sceneContext.PlayerState._model.upgradeModel.upgradeDefinitions.items._items
            .FirstOrDefault(x => x._uniqueId == packet.id));
        handlingPacket = false;

    }
}