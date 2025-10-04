using System.Collections.Generic;

namespace NewSR2MP.Packet
{
    /// <summary>
    /// Packet sent from client to host when client is about to disconnect
    /// Contains current state of client's inventory for saving
    /// </summary>
    public class ClientInventorySyncPacket : IPacket
    {
        public PacketReliability Reliability => PacketReliability.ReliableOrdered;
        public PacketType Type => PacketType.ClientInventorySync;

        public List<AmmoData> inventory;
        
        public void Serialize(OutgoingMessage msg)
        {
            msg.Write(inventory.Count);
            foreach (var slot in inventory)
            {
                msg.Write(slot);
            }
        }

        public void Deserialize(IncomingMessage msg)
        {
            int count = msg.ReadInt32();
            inventory = new List<AmmoData>(count);
            
            for (int i = 0; i < count; i++)
            {
                inventory.Add(msg.ReadAmmoData());
            }
        }
    }
}

