using NewSR2MP.Attributes;
using NewSR2MP;
using NewSR2MP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NewSR2MP
{
    public static partial class NetworkHandler
    {
        public class PacketHandler
        {
            public MethodInfo Method;
            public IPacket Packet;
            public PacketResponseAttribute Attribute;

            public PacketHandler(MethodInfo method, IPacket packet, PacketResponseAttribute attribute)
            {
                Method = method;
                Packet = packet;
                Attribute = attribute;
            }

            public bool Execute(NetPlayerState netPlayer, IncomingMessage im, byte channel)
            {
                Packet.Deserialize(im);
                var value = Method.Invoke(null, new object[] { netPlayer, Packet, channel });
                if (value == null) return true;

                return value is bool returnBool ? returnBool : true;
            }
        }
        static Dictionary<PacketType, PacketHandler> packetHandlers = new Dictionary<PacketType, PacketHandler>();
        public static void Initialize()
        {
            
            // It grabs all the available Methods in the NetworkHandlers, creates a instance of the packet and saves it into a Dictionary with the packet type
            // The Handle methods then get executed accordingly by the HandleClientPacket and HandleServerPacket Methods
            int count = 0;
            foreach (var method in typeof(NetworkHandler).GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                var parameters = method.GetParameters();
                if (parameters.Length == 3 && parameters[0].ParameterType == typeof(NetPlayerState) && typeof(IPacket).IsAssignableFrom(parameters[1].ParameterType) && parameters[2].ParameterType == typeof(byte))
                {
                    try
                    {
                        var packet = (IPacket)Activator.CreateInstance(parameters[1].ParameterType);
                        var attribute = method.GetCustomAttribute<PacketResponseAttribute>();
                        packetHandlers.Add(packet.Type, new PacketHandler(method, packet, attribute));
                        count++;
                    }
                    catch (Exception ex)
                    {
                        SRMP.Error($"Error registering handler for method {method.Name}: {ex.Message}");
                    }
                }
            }
            SRMP.Log($"Registered {count} packet handlers");
        }

        private static HashSet<PacketType> reportedMissingHandlers = new HashSet<PacketType>();
        
        internal static void HandleClientPacket(PacketType type, byte channel, IncomingMessage im)
        {
            if (packetHandlers.TryGetValue(type, out var handler))
            {
                if (!handler.Execute(null, im, channel))
                    SRMP.Error($"Failed to handle packet {type}");
            }
            else
            {
                // Only report each missing handler once to avoid spam
                if (reportedMissingHandlers.Add(type))
                {
                    SRMP.Error($"Failed to find packet handler for {type}");
                }
            }
        }

        internal static void HandleServerPacket(NetPlayerState netPlayer, byte channel, PacketType type, IncomingMessage im)
        {
            if (packetHandlers.TryGetValue(type, out var handler))
            {
                if (!handler.Execute(netPlayer, im, channel))
                {
                    SRMP.Error($"Failed to handle packet {type}");
                    return;
                }

                if (handler.Attribute != null)
                {
                    channel = handler.Attribute.Channel.HasValue ? handler.Attribute.Channel.Value : channel;
                    if (handler.Attribute.ExcludeSender)
                    {
                        handler.Packet.SendPacket(handler.Packet.Reliability, channel, netPlayer);
                    }
                    else
                    {
                        handler.Packet.SendPacket(handler.Packet.Reliability, channel);
                    }
                }
            }
            else
            {
                // Only report each missing handler once to avoid spam
                if (reportedMissingHandlers.Add(type))
                {
                    SRMP.Error($"Failed to find packet handler for {type}");
                }
            }
        }
        
        // NOTE: No [PacketResponse] attribute - this packet should not be retransmitted
        private static void HandleSaveReceived(NetPlayerState netPlayer, LoadPacket packet, byte channel)
        {
            latestSaveJoined = packet;
        }
        
        // NOTE: No [PacketResponse] attribute - this packet should not be retransmitted
        private static void HandleServerShutdown(NetPlayerState netPlayer, ServerClosePacket packet, byte channel)
        {
            if (ServerActive()) return; // nuh uh

            MultiplayerManager.EraseValues();
            
            systemContext.SceneLoader.LoadMainMenuSceneGroup();
        }
    }
}
