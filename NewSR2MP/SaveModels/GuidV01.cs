namespace NewSR2MP.SaveModels;

public class GuidV01 : SaveComponentBase<GuidV01>
{
    // Required constructors for SaveComponentBase
    public GuidV01(System.IO.BinaryReader reader, System.IO.BinaryWriter writer) : base(reader, writer) { }
    
    public GuidV01(Guid guid)
    {
        value = guid;
    }
    public GuidV01() { }
    public override int ComponentVersion => 1;
    public override string ComponentIdentifier => "GUID";
    public Guid value;
    public override void WriteComponent()
    {
        Write(value.ToString());
    }

    public override void ReadComponent()
    {
        value = Guid.Parse(Read<string>());
    }

    public override void UpgradeComponent(GuidV01 old)
    {
        throw new NotImplementedException();
    }
}