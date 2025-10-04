using UnityEngine;
using TreasurePod = Il2Cpp.TreasurePod;

namespace NewSR2MP.Packet
{
    public class LoadPacket : IPacket
    {
        public List<InitActorData> initActors;
        public List<InitPlayerData> initPlayers;
        public List<InitPlotData> initPlots;
        public List<InitAccessData> initAccess;
        public List<InitSwitchData> initSwitches;
        public Dictionary<int, TreasurePod.State> initPods;
        
        public List<InitResourceNodeData> initResourceNodes;

        public List<string> initPedias;
        public List<string> initMaps;

        public HashSet<InitGordoData> initGordos;

        public LocalPlayerData localPlayerSave;
        public int playerID;
        
        public int money;
        public Dictionary<byte, sbyte> upgrades;
        public double time;
        
        public List<float> marketPrices = new();
        public Dictionary<int, int> refineryItems = new();

        // Progress tracking
        public List<string> initProgress = new(); // ProgressDirector unlocks
        public List<string> initTutorials = new(); // Completed tutorials
        public bool hasCompletedFirstTimeExperience = false;

        public PacketType Type => PacketType.JoinSave;
        public PacketReliability Reliability => PacketReliability.ReliableUnordered;

        public void Serialize(OutgoingMessage msg)
        {
            msg.Write(initActors.Count);
            foreach (var actor in initActors)
            {
                msg.Write(actor.id);
                msg.Write(actor.ident);
                msg.Write(actor.scene);
                msg.Write(actor.pos);
                msg.Write(actor.rot);

                msg.Write((byte)actor.gadgetType);
                switch (actor.gadgetType)
                {
                    case InitActorData.GadgetType.NON_GADGET:
                        break;
                    case InitActorData.GadgetType.BASIC:
                        msg.Write(actor.constructionEndTime);
                        break;
                    case InitActorData.GadgetType.LINKED_NO_AMMO:
                        msg.Write(actor.constructionEndTime);
                        msg.Write(actor.linkedId);
                        break;
                    case InitActorData.GadgetType.LINKED_WITH_AMMO:
                        msg.Write(actor.constructionEndTime);
                        msg.Write(actor.linkedId);
                        
                        msg.Write(actor.ammo.slots);

                        msg.Write(actor.ammo.ammo.Count);
                        foreach (var ammo in actor.ammo.ammo)
                        {
                            msg.Write(ammo);
                        }
                        break;
                    default:
                        MelonLogger.Error(new NotImplementedException($"Failed to find Gadget Type {actor.gadgetType}"));
                        break;
                }
            }
            msg.Write(initPlayers.Count);
            foreach (var player in initPlayers)
            {
                msg.Write(player.id);
                msg.Write(player.username);
            }
            msg.Write(initPlots.Count);
            foreach (var plot in initPlots)
            {
                msg.Write(plot.id);
                msg.Write((int)plot.type); 
                msg.Write(plot.upgrades.Count);

                foreach (var upg in plot.upgrades)
                {
                    msg.Write((int)upg);
                }

                msg.Write(plot.siloData.Count);
                foreach (var silo in plot.siloData)
                {
                    msg.Write(silo.Key);
                    msg.Write(silo.Value.slots);

                    msg.Write(silo.Value.ammo.Count);
                    foreach (var ammo in silo.Value.ammo)
                    {
                        msg.Write(ammo);
                    }
                }
                msg.Write(plot.cropIdent);
                
                msg.Write(plot.siloSlotSelections.Length);
                foreach (var selected in plot.siloSlotSelections)
                {
                    msg.Write(selected);
                }
                
                msg.Write((byte)plot.feederSpeed);
            }
            msg.Write(initGordos.Count);
            foreach (var gordo in initGordos)
            {
                msg.Write(gordo.id);
                msg.Write(gordo.eaten);
                msg.Write(gordo.ident);
                msg.Write(gordo.targetCount);
            }
            msg.Write(initPedias.Count);
            foreach (var pedia in initPedias)
            {
                msg.Write(pedia);
            }
            msg.Write(initMaps.Count);
            foreach (var map in initMaps)
            {
                msg.Write(map);
            }
            msg.Write(initAccess.Count);
            foreach (var access in initAccess)
            {
                msg.Write(access.id);
                msg.Write(access.open);
            }

            msg.Write(playerID);
            msg.Write(localPlayerSave.pos);
            msg.Write(localPlayerSave.rot);
            msg.Write(localPlayerSave.ammo.Count);

            foreach (var amm in localPlayerSave.ammo)
            {
                msg.Write(amm);
            }
            msg.Write(localPlayerSave.sceneGroup);
            
            msg.Write(money);

            msg.Write(upgrades.Count);
            foreach (var upgrade in upgrades)
            {
                msg.Write(upgrade.Key);
                msg.Write(upgrade.Value);
            }
            

            msg.Write(time);

            msg.Write(marketPrices.Count);
            foreach (var price in marketPrices)
                msg.Write(price);
            
            msg.Write(refineryItems.Count);
            foreach (var item in refineryItems)
            {
                msg.Write(item.Key);
                msg.Write(item.Value);
            }

            msg.Write(initSwitches.Count);
            foreach (var _switch in initSwitches)
            {
                msg.Write(_switch.id);
                msg.Write(_switch.state);
            }
            
            msg.Write(initPods.Count);
            foreach (var pod in initPods)
            {
                msg.Write(pod.Key);
                msg.Write((byte)pod.Value);
            }

            // Progress tracking
            msg.Write(initProgress.Count);
            foreach (var progress in initProgress)
                msg.Write(progress);

            msg.Write(initTutorials.Count);
            foreach (var tutorial in initTutorials)
                msg.Write(tutorial);

            msg.Write(hasCompletedFirstTimeExperience);
        }

        public void Deserialize(IncomingMessage msg)
        {
            int lengthActor = msg.ReadInt32();

            initActors = new List<InitActorData>();
            for (int i = 0; i < lengthActor; i++)
            {
                long id = msg.ReadInt64();
                int ident = msg.ReadInt32();
                int sg = msg.ReadInt32();
                Vector3 actorPos = msg.ReadVector3();
                Vector3 actorRot = msg.ReadVector3();
                var data = new InitActorData
                {
                    id = id,
                    ident = ident,
                    scene = sg,
                    pos = actorPos,
                    rot = actorRot,
                };
                InitActorData.GadgetType gadgetType = (InitActorData.GadgetType)msg.ReadByte();
                switch (gadgetType)
                {
                    case InitActorData.GadgetType.NON_GADGET:
                        break;
                    case InitActorData.GadgetType.BASIC:
                        data.constructionEndTime = msg.ReadDouble();
                        break;
                    case InitActorData.GadgetType.LINKED_NO_AMMO:
                        data.constructionEndTime = msg.ReadDouble();
                        data.linkedId = msg.ReadInt64();
                        break;
                    case InitActorData.GadgetType.LINKED_WITH_AMMO:
                        data.constructionEndTime = msg.ReadDouble();
                        data.linkedId = msg.ReadInt64();
                        
                        int slots = msg.ReadInt32();
                        int ammLength = msg.ReadInt32();
                        HashSet<AmmoData> ammoDatas = new HashSet<AmmoData>();
                        for (int i2 = 0; i2 < ammLength; i2++)
                        {
                            var ammo = msg.ReadAmmoData();
                            ammoDatas.Add(ammo);
                        }

                        data.ammo = new InitSiloData()
                        {
                            slots = slots,
                            ammo = ammoDatas,
                        };
                        break;
                    default:
                        MelonLogger.Error(new NotImplementedException($"Failed to find Gadget Type {gadgetType}"));
                        break;
                }
                initActors.Add(data);
            }

            int lengthPlayer = msg.ReadInt32();
            initPlayers = new List<InitPlayerData>();
            for (int i = 0; i < lengthPlayer; i++)
            {
                int id = msg.ReadInt32();
                string username = msg.ReadString();
                initPlayers.Add(new InitPlayerData()
                {
                    id = id,
                    username = username,
                });
            }

            int lengthPlot = msg.ReadInt32();
            initPlots = new List<InitPlotData>();
            for (int i = 0; i < lengthPlot; i++)
            {
                int id = msg.ReadInt32();
                LandPlot.Id type = (LandPlot.Id)msg.ReadInt32();
                int upgLength = msg.ReadInt32();
                Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> upgrades =
                    new Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade>();
                for (int i2 = 0; i2 < upgLength; i2++)
                {
                    upgrades.Add((LandPlot.Upgrade)msg.ReadInt32());
                }

                Dictionary<string, InitSiloData> silos = new();
                int inventories = msg.ReadInt32();
                for (int j = 0; j < inventories; j++)
                {
                    string inventoryID = msg.ReadString();
                    int slots = msg.ReadInt32();
                    int ammLength = msg.ReadInt32();
                    HashSet<AmmoData> ammoDatas = new HashSet<AmmoData>();
                    for (int i2 = 0; i2 < ammLength; i2++)
                    {
                        var data = msg.ReadAmmoData();
                        ammoDatas.Add(data);
                    }

                    silos.Add(inventoryID, new InitSiloData
                    {
                        slots = slots,
                        ammo = ammoDatas
                    });
                }
                var crop = msg.ReadInt32();
                
                int slotGroupCount = msg.ReadInt32();
                List<int> slotSelections = new();
                for (int i2 = 0; i2 < slotGroupCount; i2++)
                {
                    slotSelections.Add(msg.ReadInt32());
                }
                
                SlimeFeeder.FeedSpeed feederSpeed = (SlimeFeeder.FeedSpeed)msg.ReadByte();
                
                initPlots.Add(new InitPlotData()
                {
                    type = type,
                    id = id,
                    upgrades = upgrades,
                    siloData = silos,
                    cropIdent = crop,
                    siloSlotSelections = slotSelections.ToArray(),
                    feederSpeed = feederSpeed
                });
            }

            int lengthGordo = msg.ReadInt32();
            initGordos = new HashSet<InitGordoData>();
            for (int i = 0; i < lengthGordo; i++)
            {
                int id = msg.ReadInt32();
                int eaten = msg.ReadInt32();
                int ident = msg.ReadInt32();
                int target = msg.ReadInt32();
                initGordos.Add(new InitGordoData()
                {
                    id = id,
                    eaten = eaten,
                    ident = ident,
                    targetCount = target,
                });
            }

            int pedLength = msg.ReadInt32();
            initPedias = new List<string>();
            for (int i = 0; i < pedLength; i++)
            {
                initPedias.Add(msg.ReadString());
            }

            int mapLength = msg.ReadInt32();
            initMaps = new List<string>();
            for (int i = 0; i < mapLength; i++)
            {
                initMaps.Add(msg.ReadString());
            }

            int accLength = msg.ReadInt32();
            initAccess = new List<InitAccessData>();
            for (int i = 0; i < accLength; i++)
            {
                int id = msg.ReadInt32();
                bool open = msg.ReadBoolean();
                InitAccessData accessData = new InitAccessData()
                {
                    id = id,
                    open = open,
                };
                initAccess.Add(accessData);
            }

            playerID = msg.ReadInt32();
            var pos = msg.ReadVector3();
            var rot = msg.ReadVector3();

            var localAmmoCount = msg.ReadInt32();

            List<AmmoData> localAmmo = new List<AmmoData>();
            for (int i = 0; i < localAmmoCount; i++)
            {
                localAmmo.Add(msg.ReadAmmoData());
            }

            int scene = msg.ReadInt32();

            localPlayerSave = new LocalPlayerData()
            {
                pos = pos,
                rot = rot,
                ammo = localAmmo,
                sceneGroup = scene
            };


            money = msg.ReadInt32();

            var pUpgradesCount = msg.ReadInt32();
            upgrades = new(pUpgradesCount);

            for (int i = 0; i < pUpgradesCount; i++)
            {
                var key = msg.ReadByte();
                var val = msg.ReadSByte();

                upgrades.TryAdd(key, val);
            }

            time = msg.ReadDouble();

            var marketCount = msg.ReadInt32();
            marketPrices = new List<float>(marketCount);

            for (int i = 0; i < marketCount; i++)
                marketPrices.Add(msg.ReadFloat());

            var refineryCount = msg.ReadInt32();
            refineryItems = new Dictionary<int, int>(refineryCount);

            for (int i = 0; i < refineryCount; i++)
                refineryItems.Add(msg.ReadInt32(), msg.ReadInt32());


            initSwitches = new List<InitSwitchData>();
            var switchCount = msg.ReadInt32();
            for (int i = 0; i < switchCount; i++)
                initSwitches.Add(new InitSwitchData
                {
                    id = msg.ReadString(),
                    state = msg.ReadByte()
                });

            initPods = new Dictionary<int, TreasurePod.State>();
            var podCount = msg.ReadInt32();
            for (int i = 0; i < podCount; i++)
                initPods.Add(msg.ReadInt32(), (TreasurePod.State)msg.ReadByte());

            // Progress tracking
            int progressCount = msg.ReadInt32();
            initProgress = new List<string>();
            for (int i = 0; i < progressCount; i++)
                initProgress.Add(msg.ReadString());

            int tutorialCount = msg.ReadInt32();
            initTutorials = new List<string>();
            for (int i = 0; i < tutorialCount; i++)
                initTutorials.Add(msg.ReadString());

            hasCompletedFirstTimeExperience = msg.ReadBoolean();
        }
    }

    public class InitActorData
    {
        public enum GadgetType : byte
        {
            NON_GADGET,
            /// <summary>
            /// Basic gadgets like decorations
            /// </summary>
            BASIC,
            /// <summary>
            /// Teleporters
            /// </summary>
            LINKED_NO_AMMO,
            /// <summary>
            /// Warp Depots
            /// </summary>
            LINKED_WITH_AMMO,
            /// <summary>
            /// The drone
            /// </summary>
            DRONE,
            
        }
        public long id;
        public int ident;
        public int scene;
        public Vector3 pos;
        public Vector3 rot;

        // Gadgets
        public double constructionEndTime;
        public GadgetType gadgetType;
        
        // LINKED_NO_AMMO
        // LINKED_WITH_AMMO
        public long linkedId;
        
        // LINKED_WITH_AMMO
        public InitSiloData ammo;
        
        // DRONE
    }
    public class InitGordoData
    {
        // Use $"gordo{ExtendInteger(id)}" to get actual id
        public int id;
        public int eaten;
        public int ident;
        public int targetCount;
    }
    /*public class InitGadgetData
    {
        public thingy gadgetData;

        public string id;
        public string gadget;
    }*/

    public class InitAccessData
    {
        // Use $"door{ExtendInteger(id)}" to get actual id
        public int id;
        public bool open;
    }
    public class InitPlotData
    {
        // Use $"plot{ExtendInteger(id)}" to get actual id
        public int id;
        public LandPlot.Id type;
        public Il2CppSystem.Collections.Generic.HashSet<LandPlot.Upgrade> upgrades;
        public int cropIdent;

        public int[] siloSlotSelections; // needs to be int[4], lengths higher or lower will probably break.
        public Dictionary<string, InitSiloData> siloData;

        public SlimeFeeder.FeedSpeed feederSpeed = SlimeFeeder.FeedSpeed.NORMAL;
    }

    public class InitSiloData
    {
        public int slots;

        public HashSet<AmmoData> ammo;
    }

    public class AmmoData
    {
        public int id;
        public int count;
        public int slot;
    }

    public class InitPlayerData
    {
        public int id;
        public string username;
    }
    public class InitSwitchData
    {
        // some idiot made a switch id be longer than a signed long
        public string id;
        public byte state;
    }
    public class InitResourceNodeData
    {
        // Use $"resource_node{ExtendInteger(id)}" to get actual id
        public int id;
        public byte definition;
    }
    public class LocalPlayerData
    {
        public Vector3 pos;
        public Vector3 rot;

        public int sceneGroup;
        
        public List<AmmoData> ammo;
        
        // Intro state
        public bool hasSeenIntro;
        
        // Waypoint data
        public bool hasWaypoint;
        public Vector3 waypointPosition;
        public byte waypointMap; // 0 = RainbowIsland, 1 = Labyrinth
    }
}
