using Il2CppMonomiPark.SlimeRancher.DataModel;
using NewSR2MP.Attributes;

namespace NewSR2MP;

public partial class NetworkHandler
{
    
    [PacketResponse]
    private static void HandleGordoEat(NetPlayerState netPlayer, GordoEatPacket packet, byte channel)
    {

        try
        {
            if (!sceneContext.GameModel.gordos.TryGetValue(packet.id, out var gordo))
                sceneContext.GameModel.gordos.Add(packet.id, new GordoModel()
                {
                    fashions = new Il2CppSystem.Collections.Generic.List<IdentifiableType>(),
                    gordoEatCount = packet.count,
                    gordoSeen = true,
                    identifiableType = identifiableTypes[packet.ident],
                    gameObj = null,
                    GordoEatenCount = packet.count,
                    targetCount = gameContext.LookupDirector._gordoDict[identifiableTypes[packet.ident]]
                        .GetComponent<GordoEat>().TargetCount,
                });
            gordo.gordoEatCount = packet.count;
        }
        catch (Exception e)
        {
            SRMP.Log($"Exception in feeding gordo({packet.id})! Stack Trace:\n{e}");
        }
    }


    [PacketResponse]
    private static void HandleGordoBurst(NetPlayerState netPlayer, GordoBurstPacket packet, byte channel)
    {

        try
        {
            var target = gameContext.LookupDirector._gordoDict[identifiableTypes[packet.ident]]
                .GetComponent<GordoEat>().TargetCount;
            if (!sceneContext.GameModel.gordos.TryGetValue(packet.id, out var gordo))
                sceneContext.GameModel.gordos.Add(packet.id, new GordoModel()
                {
                    fashions = new Il2CppSystem.Collections.Generic.List<IdentifiableType>(),
                    gordoEatCount = target,
                    gordoSeen = true,
                    identifiableType = identifiableTypes[packet.ident],
                    gameObj = null,
                    GordoEatenCount = target,
                    targetCount = gameContext.LookupDirector._gordoDict[identifiableTypes[packet.ident]]
                        .GetComponent<GordoEat>().TargetCount,
                });
            else
            {
                var gordoObj = gordo.gameObj;
                gordoObj.AddComponent<PreventReward>();
                var eats = gordoObj.GetComponent<GordoEat>();
                
                handlingPacket = true;
                eats.StartCoroutine(eats.ReachedTarget());
                handlingPacket = false;
            }
        }
        catch (Exception e)
        {
            SRMP.Log($"Exception in popping gordo({packet.id})! Stack Trace:\n{e}");
        }


    }
}