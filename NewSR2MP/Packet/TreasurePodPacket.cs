using UnityEngine;

namespace NewSR2MP.Packet
{
    public class TreasurePodPacket : IPacket
    {   
        public PacketReliability Reliability => PacketReliability.UnreliableUnordered;

        public PacketType Type => PacketType.TreasurePod;

        public int id;
        public void Serialize(OutgoingMessage msg)
        {
            msg.Write(id);
        }

        public void Deserialize(IncomingMessage msg)
        {
            id = msg.ReadInt32();
        }
    }
}