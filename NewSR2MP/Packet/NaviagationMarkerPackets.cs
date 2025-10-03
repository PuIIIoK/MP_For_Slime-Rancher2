using UnityEngine;

namespace NewSR2MP.Packet
{
    public enum MapType : byte
    {
        RainbowIsland,
        Labyrinth,
    }
    
    public class PlaceNavMarkerPacket : IPacket
    {
        public PacketReliability Reliability => PacketReliability.UnreliableUnordered;

        public PacketType Type => NavigationMarkerPlace;

        public MapType map;
        public Vector3 position;
        public ushort playerID; // ID игрока, который поставил waypoint
        
        public void Serialize(OutgoingMessage msg)
        {
            msg.Write((byte)map);
            msg.Write(position);
            msg.Write(playerID);
        }

        public void Deserialize(IncomingMessage msg)
        {
            map = (MapType)msg.ReadByte();
            position = msg.ReadVector3();
            playerID = msg.ReadUInt16();
        }
    }
    
    public class RemoveNavMarkerPacket : IPacket
    {
        public PacketReliability Reliability => PacketReliability.UnreliableUnordered;

        public PacketType Type => NavigationMarkerRemove;

        public ushort playerID; // ID игрока, который удалил waypoint
        
        public void Serialize(OutgoingMessage msg)
        {
            msg.Write(playerID);
        }

        public void Deserialize(IncomingMessage msg)
        {
            playerID = msg.ReadUInt16();
        }
    }
}
