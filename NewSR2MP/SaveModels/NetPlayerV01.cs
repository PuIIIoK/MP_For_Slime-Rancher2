
using Il2CppMonomiPark.SlimeRancher.Persist;
using System;
using System.Collections.Generic;
using Il2CppSystem.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace NewSR2MP.SaveModels
{
    /// <summary>
    /// TODO: Add health, stamina, and rads.
    /// </summary>
    public class NetPlayerV01 : SaveComponentBase<NetPlayerV01>
    {
        // Required constructors for SaveComponentBase
        public NetPlayerV01(System.IO.BinaryReader reader, System.IO.BinaryWriter writer) : base(reader, writer) { }
        public NetPlayerV01() : base() { }
        
        public override string ComponentIdentifier { get; }
        
        public override int ComponentVersion => 1; // Version 1

        public byte sceneGroup = 1;
        
        public ModdedVector3V01 position = new ModdedVector3V01(541.6466f, 18.646f, 349.3299f);
        public ModdedVector3V01 rotation = new ModdedVector3V01(Vector3.up * 236.8107f);

        public List<NetworkAmmoDataV01> ammo = new List<NetworkAmmoDataV01>();
        
        // Track if player has completed initial tutorials
        public bool tutorialsCompleted = false;
        
        // Track if player has seen the intro sequence
        public bool hasSeenIntro = false;
        
        // Waypoint data
        public bool hasWaypoint = false;
        public ModdedVector3V01 waypointPosition = new ModdedVector3V01(0, 0, 0);
        public byte waypointMap = 0; // 0 = RainbowIsland, 1 = Labyrinth
        
        public override void WriteComponent()
        {
            Write(position);
            Write(rotation);
            
            Write(sceneGroup);
            
            WriteList(ammo);
            
            Write(tutorialsCompleted);
            Write(hasSeenIntro);
            
            // Save waypoint data
            Write(hasWaypoint);
            Write(waypointPosition);
            Write(waypointMap);
        }

        public override void ReadComponent()
        {
            position = Read<ModdedVector3V01>();
            rotation = Read<ModdedVector3V01>();
            
            sceneGroup = Read<byte>();
            
            ammo = ReadList<NetworkAmmoDataV01>();
            
            // Try to read tutorial state if available (for forward compatibility)
            try
            {
                if (Reader.BaseStream.Position < Reader.BaseStream.Length)
                {
                    tutorialsCompleted = Read<bool>();
                    
                    // Try to read intro state if available
                    if (Reader.BaseStream.Position < Reader.BaseStream.Length)
                    {
                        hasSeenIntro = Read<bool>();
                    }
                    
                    // Try to read waypoint data if available
                    if (Reader.BaseStream.Position < Reader.BaseStream.Length)
                    {
                        hasWaypoint = Read<bool>();
                        waypointPosition = Read<ModdedVector3V01>();
                        waypointMap = Read<byte>();
                    }
                }
            }
            catch
            {
                tutorialsCompleted = false;
                hasSeenIntro = false;
                hasWaypoint = false;
            }
        }

        public override void UpgradeComponent(NetPlayerV01 old)
        {
            // This shouldn't be called with the new approach
            // But implement it for safety
            position = old.position;
            rotation = old.rotation;
            sceneGroup = old.sceneGroup;
            ammo = old.ammo;
            tutorialsCompleted = false; // Default for upgraded saves
        }
    }
}
