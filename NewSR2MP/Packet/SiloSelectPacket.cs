namespace NewSR2MP.Packet;

public class SiloSelectPacket : IPacket
{
    
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;

    public PacketType Type => SiloSlotSelect;

    public string id;
    public int groupIdx;
    public byte select;
        
    public void Serialize(OutgoingMessage msg)
    {
        msg.Write(id);
        msg.Write(groupIdx);
        msg.Write(select);
    }

    public void Deserialize(IncomingMessage msg)
    {
        id = msg.ReadString();
        groupIdx = msg.ReadInt32();
        select = msg.ReadByte();
    }
}