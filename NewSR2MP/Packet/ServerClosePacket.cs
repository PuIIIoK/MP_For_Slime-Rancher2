namespace NewSR2MP.Packet;

public class ServerClosePacket : IPacket
{
    public PacketType Type => ServerShutdown;
    public PacketReliability Reliability => PacketReliability.ReliableOrdered;
    public void Serialize(OutgoingMessage om) { }

    public void Deserialize(IncomingMessage im) { }
}