using Il2CppMonomiPark.SlimeRancher.Persist;
using System.IO;

namespace NewSR2MP.SaveModels
{
    public class NetworkV01 : SaveComponentBase<NetworkV01>
    {
        // Required constructors for SaveComponentBase
        public NetworkV01(System.IO.BinaryReader reader, System.IO.BinaryWriter writer) : base(reader, writer) { }
        public NetworkV01() : base() { }
        
        public override string ComponentIdentifier => "MPNK";
        public override int ComponentVersion => 1;

        public PlayerListV01 savedPlayers = new PlayerListV01();
        
        
        public override void WriteComponent()
        {
            Write(savedPlayers);
        }

        public override void ReadComponent()
        {
            savedPlayers = Read<PlayerListV01>();
        }

        public override void UpgradeComponent(NetworkV01 old)
        {
            throw new NotImplementedException();
        }
    }
}
