namespace NewSR2MP.SaveModels;

public class ModdedVector3V01 : SaveComponentBase<ModdedVector3V01>
{
    // Required constructors for SaveComponentBase
    public ModdedVector3V01(System.IO.BinaryReader reader, System.IO.BinaryWriter writer) : base(reader, writer) { }
    
    public ModdedVector3V01(Vector3 vector)
    {
        value = vector;
    }
    public ModdedVector3V01(float x, float y, float z) : this(new Vector3(x, y, z)) { }
    public ModdedVector3V01 () : this(0,0,0) { }
    
    public override int ComponentVersion => 1;
    public override string ComponentIdentifier => "SRV3";
    
    public Vector3 value;
    
    public override void WriteComponent()
    {
        Write(value.x);
        Write(value.y);
        Write(value.z);
    }

    public override void ReadComponent()
    {
        value = new Vector3(Read<float>(), Read<float>(), Read<float>());
    }

    public override void UpgradeComponent(ModdedVector3V01 old)
    {
        throw new NotImplementedException();
    }
}