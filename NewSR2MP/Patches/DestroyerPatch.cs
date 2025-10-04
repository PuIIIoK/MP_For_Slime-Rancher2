using System;
using Il2CppMonomiPark.SlimeRancher.World;
using UnityEngine;

namespace NewSR2MP.Patches
{
    [HarmonyPatch(typeof(Destroyer), nameof(Destroyer.DestroyActor), typeof(GameObject), typeof(string), typeof(bool))]
    public class DestroyerDestroyActor
    {
        public static bool Prefix(GameObject actorObj, string source, bool okIfNonActor)
        {
            if (isJoiningAsClient) return true;
            try
            {
                if (ServerActive() || ClientActive())
                {
                    if (source.Equals("ResourceCycle.RegistryUpdate#1"))
                    {
                        return false;
                    }
                    if (source.Equals("SlimeFeral.Awake"))
                    {
                        return false;
                    }
                }
            }
            catch { }

            // Moved here because it would spam testers' melonloader logs and lag the game because it didnt destroy (^^^^) but it sent the packet anyways.

            if ((ServerActive() || ClientActive()) && !handlingPacket && actorObj)
            {
                // Проверяем что у объекта есть IdentifiableActor компонент
                var identActor = actorObj.GetComponent<IdentifiableActor>();
                if (identActor != null)
                {
                    try
                    {
                        var packet = new ActorDestroyGlobalPacket()
                        {
                            id = identActor.GetActorId().Value,
                        };
                        MultiplayerManager.NetworkSend(packet);
                    }
                    catch (Exception ex)
                    {
                        // Игнорируем ошибки при отправке (например NoConnection при отключении)
                        SRMP.Debug($"Failed to send ActorDestroy packet: {ex.Message}");
                    }
                }
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(Destroyer), nameof(DestroyGadget), typeof(GameObject), typeof(string))]
    public class DestroyerDestroy
    {
        public static void Prefix(GameObject gadgetObj, string source)
        {
            if (isJoiningAsClient) return;
            
            if ((ServerActive() || ClientActive()) && !handlingPacket && gadgetObj) 
            {
                // Проверяем что у объекта есть Gadget компонент
                var gadget = gadgetObj.GetComponent<Gadget>();
                if (gadget != null)
                {
                    try
                    {
                        SRMP.Debug("Destroyed Gadget!");
                        var packet = new ActorDestroyGlobalPacket()
                        {
                            id = gadget.GetActorId().Value,
                        };
                        MultiplayerManager.NetworkSend(packet);
                    }
                    catch (Exception ex)
                    {
                        // Игнорируем ошибки при отправке (например NoConnection при отключении)
                        SRMP.Debug($"Failed to send GadgetDestroy packet: {ex.Message}");
                    }
                }
            }
        }
    }
}
