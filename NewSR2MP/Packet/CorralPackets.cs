namespace NewSR2MP.Packet;

public class PlortCollectorPacket : IPacket
{
    public PacketType Type => PacketType.PlortCollector;
    public PacketReliability Reliability => PacketReliability.ReliableUnordered;

    public string plot;
    public double endTime;
    
    public void Serialize(OutgoingMessage om)
    {
        om.Write(plot);
        om.Write(endTime);
    }

    public void Deserialize(IncomingMessage im)
    {
        plot = im.ReadString();
        endTime = im.ReadDouble();
    }
}
public class FeederDispensePacket : IPacket
{
    public PacketType Type => AutoFeederDispense;
    public PacketReliability Reliability => PacketReliability.ReliableUnordered;

    public string plot;
    public double nextTime;
    
    public void Serialize(OutgoingMessage om)
    {
        om.Write(plot);
        om.Write(nextTime);
    }

    public void Deserialize(IncomingMessage im)
    {
        plot = im.ReadString();
        nextTime = im.ReadDouble();
    }
}
public class FeederSetSpeedPacket : IPacket
{
    public PacketType Type => AutoFeederMode;
    public PacketReliability Reliability => PacketReliability.ReliableUnordered;

    public string plot;

    public SlimeFeeder.FeedSpeed speed;
    
    public void Serialize(OutgoingMessage om)
    {
        om.Write(plot);
        om.Write((byte)speed);
    }

    public void Deserialize(IncomingMessage im)
    {
        plot = im.ReadString();
        speed = (SlimeFeeder.FeedSpeed)im.ReadByte();
    }
}