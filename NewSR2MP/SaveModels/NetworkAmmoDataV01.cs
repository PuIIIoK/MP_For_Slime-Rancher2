namespace NewSR2MP.SaveModels;

public class NetworkAmmoDataV01 : SaveComponentBase<NetworkAmmoDataV01>
{
    // Required constructors for SaveComponentBase
    public NetworkAmmoDataV01(System.IO.BinaryReader reader, System.IO.BinaryWriter writer) : base(reader, writer) { }
    public NetworkAmmoDataV01() : base() { }
    
    public override string ComponentIdentifier => "MPAD";
    public override int ComponentVersion => 1;

    public int ident;
    public int count;
    
    public float emotionX, emotionY, emotionZ, emotionW;
    
    public override void WriteComponent()
    {
        Write(ident);
        Write(count);
        
        Write(emotionX);
        Write(emotionY);
        Write(emotionZ);
        Write(emotionW);
    }

    public override void ReadComponent()
    {
        ident = Read<int>();
        count = Read<int>();
        
        emotionX = Read<float>();
        emotionY = Read<float>();
        emotionZ = Read<float>();
        emotionW = Read<float>();
    }

    public override void UpgradeComponent(NetworkAmmoDataV01 old)
    {
        throw new NotImplementedException();
    }
}