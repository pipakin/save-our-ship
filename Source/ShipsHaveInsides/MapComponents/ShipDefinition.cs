using Harmony;
using RimWorld;
using ShipsHaveInsides.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ShipsHaveInsides.MapComponents
{
    public class ShipDefinition : IExposable
    {
        public static void ProcessNewThing(List<ShipDefinition> shipDefinitions, Thing t, string name = null)
        {
            if (!(t is Building) && t.def.building?.shipPart != true)
            {
                return;
            }

            var ships = shipDefinitions.Where(s => s.ShouldAddToShip(t)).ToList();
            if (ships.Count == 1)
            {
                ships[0].AddToShip(t);
            }
            else if (ships.Count > 1)
            {
                ships[0].AddToShip(t);
                shipDefinitions.RemoveAll(s => ships.Contains(s));
                shipDefinitions.Add(ships.Aggregate((a, b) => a.MergeWith(b)));
            }
            else if (t.def.building?.shipPart == true)
            {
                var newShip = new ShipDefinition();
                newShip.name = name;
                newShip.AddToShip(t);
                shipDefinitions.Add(newShip);
            }
        }

        private string name = null;

        private List<WeakReference> thingsInShip = new List<WeakReference>();
        private Dictionary<IntVec3, GasMixture> positionsInShip = new Dictionary<IntVec3, GasMixture>();

        private List<Thing> thingsToSpawn = new List<Thing>();

        public IEnumerable<IntVec3> Positions => positionsInShip.Keys;

        public IntVec3 Min
        {
            get
            {
                return new IntVec3(Positions.Min(x => x.x), 0, Positions.Min(x => x.z));
            }
        }

        public IntVec3 Max
        {
            get
            {
                return new IntVec3(Positions.Max(x => x.x), 0, Positions.Max(x => x.z));
            }
        }

        public IntVec3 Center
        {
            get
            {
                var min = Min;
                var max = Max;

                return new IntVec3(min.x + (max.x - min.x) / 2, 0, min.z + (max.z - min.z) / 2);
            }
        }

        public IEnumerable<Thing> Things => thingsInShip.Select(t => (Thing)t.Target).Where(t => t != null);

        public string Name { get => name ?? "Unnamed Ship"; set => name = value; }

        public bool ShouldAddToShip(Thing t)
        {
            if (t.def.building?.shipPart == true)
            {
                return Things.Any(t2 => (t2.def.building?.shipPart == true && t.IsAdjacentToCardinalOrInside(t2))) || Positions.Contains(t.Position);
            }
            else
            {
                return Positions.Contains(t.Position);
            }
        }

        public void AddToShip(Thing t)
        {
            if (!thingsInShip.Any(t2 => t2.Target == t))
            {
                thingsInShip.Add(new WeakReference(t));
                if (t.def.building?.shipPart == true)
                {
                    foreach (var c in t.OccupiedRect())
                    {
                        if (!Positions.Contains(c))
                        {
                            positionsInShip.Add(c, t.Map.IsSpace() ? GasMixture.Vacuum : GasMixture.EarthNorm);
                        }
                        //add everything at this position
                        foreach (var tC in t.Map.thingGrid.ThingsAt(t.Position))
                        {
                            AddToShip(tC);
                        }
                    }
                }

                mass = 0.0f;
                thrust = 0.0f;
            }
        }

        public List<ShipDefinition> RemoveFromShip(Thing t)
        {
            if (!thingsInShip.Any(t2 => t2.Target == t))
            {
                return null;
            }

            //Make sure we are up to date
            Regenerate(t.Map);

            //decide if this would split us.
            thingsInShip.RemoveAll(p => p.Target == t);

            mass = 0.0f;
            thrust = 0.0f;

            if (t.def.building?.shipPart != true)
                return null;

            var otherThings = thingsInShip.Where(t2 => (t2?.Target as Thing)?.OccupiedRect().Contains(t.Position) == true);
            if (!otherThings.Any(p => (p?.Target as Thing)?.def?.building?.shipPart == true))
            {
                //remove all the parts
                thingsInShip.RemoveAll(p => otherThings.Contains(p));
                RegeneratePoints(true);

                var defs = new List<ShipDefinition>();
                foreach (var existingThing in Things)
                {
                    ProcessNewThing(defs, existingThing, this.name != null ? this.name + " Debris" : null);
                }

                if (defs.Count > 1)
                {
                    return defs;
                }
                else if (defs.Count == 0)
                {
                    return new List<ShipDefinition>();
                }
            }

            return null;
        }

        public ShipDefinition MergeWith(ShipDefinition otherShip)
        {
            string newName = name;
            if (name == null)
            {
                newName = otherShip.name;
            } else
            {
                if (otherShip.name == null)
                {
                    newName = name;
                } else
                {
                    newName = name + "-" + otherShip.name;
                }
            }
            return new ShipDefinition()
            {
                name = newName,
                thingsInShip = thingsInShip.Concat(otherShip.thingsInShip).ToList(),
                positionsInShip = positionsInShip.Concat(otherShip.positionsInShip).ToDictionary(x => x.Key, x => x.Value)
            };
        }

        public bool ShouldSaveThings { get; set; } = false;
        public void ExposeData()
        {
            List<Thing> list = null;
            if (Scribe.mode == LoadSaveMode.Saving && ShouldSaveThings)
            {
                list = new ThingMutator<Thing>()
                   .ExpandContained<Building, Thing>(b => GridsUtility.GetThingList(b.Position, b.Map).Where(t => !(t is Building) && !(t is Mote)))
                   .UnsafeExecute(Things)
                   .ToList();
            }
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref list, "things", LookMode.Deep);
            Scribe_Collections.Look(ref positionsInShip, "positionsInShip");

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (positionsInShip == null && list != null && list.Count > 0)
                {
                    //loading from 1.x Save
                    //weee....
                    positionsInShip = new HashSet<IntVec3>(list.Where(x => x?.def?.building?.shipPart == true).SelectMany(x => x.OccupiedRect().Cells).ToList()).ToDictionary(x => x, x => GasMixture.EarthNorm);
                }

                if (list != null && list.Count > 0)
                {
                    thingsToSpawn = list.Where(x => x != null && x.def != null).ToList();
                }

                if (positionsInShip == null)
                {
                    positionsInShip = new Dictionary<IntVec3, GasMixture>();
                }
            }
        }

        public void Regenerate(Map map, bool regenWithData = false)
        {
            if (thingsInShip.Count > 0 && !regenWithData)
            {
                return;
            }
            else
            {
                thingsInShip.Clear();
            }
            foreach (var p in Positions)
            {
                foreach (var thing in map.thingGrid.ThingsAt(p))
                    AddToShip(thing);
            }
        }

        public void RegeneratePoints(bool clear = false)
        {
            if (clear) positionsInShip.Clear();

            foreach (var thing in Things)
            {
                if (thing.def.building?.shipPart == true)
                {
                    foreach (var cell in thing.OccupiedRect())
                    {
                        if (!Positions.Contains(cell))
                            positionsInShip.Add(cell, thing.Map.IsSpace() ? GasMixture.Vacuum : GasMixture.EarthNorm);
                    }
                }
            }
        }

        public int? Tile { get; set; }

        public bool ReadyForShortRangeJump {
            get { return Tile != null; }
            set
            {
                if (!value)
                {
                    Tile = null;
                }
            }
        }

        public IntVec3? DetermineLandingSpot(Map map)
        {
            IntVec3 outCell = IntVec3.Zero;

            if (CellFinderLoose.TryFindRandomNotEdgeCellWith((int)Mathf.Ceil(Mathf.Max(Max.x - Min.x, Max.z - Min.z)), cell =>
             {
                 LongEventHandler.SetCurrentEventText("Scanning for Landing Zone: (" + cell.x + ", " + cell.z + ")");
                 try
                 {
                     return !positionsInShip.Select(x => x.Key + (cell - Center)).Any(x =>
                         map.thingGrid.CellContains(x, ThingCategory.Building)
                         || map.thingGrid.CellContains(x, ThingCategory.Pawn));
                 }
                 catch
                 {
                     return false;
                 }
             }, map, out outCell))
                return outCell;

            return null;
        }

        public ThingMutator<Thing>.ThenContainer<Thing> Move(Map oldMap, Func<Map> newMap, string LoadingPrefix, bool async, Action<Exception> handler, AirShipWorldObject obj = null, IntVec3? landingSpot = null, bool clearLandingZone = false)
        {
            if(obj != null)
                obj.ShipDefinition = this;

            ReadyForShortRangeJump = false;

            var offset = landingSpot != null ? landingSpot.Value - Center : IntVec3.Zero;

            LongEventHandler.QueueLongEvent(() => {
                oldMap.GetSpaceAtmosphereMapComponent().processUpdates = false;
                newMap().GetSpaceAtmosphereMapComponent().processUpdates = false;
                oldMap.GetSpaceAtmosphereMapComponent().RemoveWholeShip(this);
            }, LoadingPrefix + "_Generate", async, handler);

            Dictionary<Pawn, Building_Bed> ownedBeds = new Dictionary<Pawn, Building_Bed>();

            return new ThingMutator<Thing>()
                .ExpandContained<Building, Thing>(b => GridsUtility.GetThingList(b.Position, oldMap).Where(t => !(t is Building) && !(t is Mote) && !(t is Plant)))
                .For<Pawn>(p => {
                    ownedBeds[p] = p.ownership.OwnedBed;
                    if (ownedBeds[p] != null)
                        p.ownership.UnclaimBed();
                })
                .QueueAsLongEvent(Things, LoadingPrefix + "_Spawn", async, handler)
                .Then(new ThingMutator<Thing>()
                        .ForComp<CompPower>(powerComp =>
                        {
                            PowerConnectionMaker.DisconnectFromPowerNet(powerComp);
                            oldMap.powerNetManager.UpdatePowerNetsAndConnections_First();
                        })
                        .DeSpawn<Thing>()
                        .Move(x => x + offset)
                        .SpawnInto<Thing>(newMap)
                        .ForComp<CompPowerTrader>(powerComp =>
                        {
                            powerComp.PowerOn = true;
                        })
                        .ForComp<CompPowerPlant>(powerComp =>
                        {
                            powerComp.UpdateDesiredPowerOutput();
                        })
                        .SetAsHome<Thing>(x => x.def.defName == "ShipHullTile" || x.def.defName == "ShipAirlock")
                    , LoadingPrefix + "_Spawn", handler)
                .Then(new ThingMutator<Thing>()
                        .ForComp<CompPowerTrader>(powerComp =>
                        {
                            if (!powerComp.PowerOn)
                            {
                                newMap().powerNetManager.UpdatePowerNetsAndConnections_First();
                                if (powerComp.PowerNet != null && powerComp.PowerNet.CurrentEnergyGainRate() > 1E-07f)
                                {
                                    powerComp.PowerOn = true;
                                }
                            }
                        })
                    , LoadingPrefix + "_Spawn", handler)
                .Then(() => {
                    foreach(var bedOwn in ownedBeds)
                    {
                        if(bedOwn.Value != null && Things.Contains(bedOwn.Value))
                        {
                            bedOwn.Key.ownership.ClaimBedIfNonMedical(bedOwn.Value);
                        }
                    }
                    newMap().GetSpaceAtmosphereMapComponent().AddWholeShip(this);
                    oldMap.GetSpaceAtmosphereMapComponent().processUpdates = true;
                    newMap().GetSpaceAtmosphereMapComponent().processUpdates = true;
                }, LoadingPrefix + "_Spawn", handler);
        }

        internal void Destroy(DestroyMode mode)
        {
            Map map = Things.First().Map;
            map.GetSpaceAtmosphereMapComponent().processUpdates = false;
            new ThingMutator<Thing>()
               .ExpandContained<Building, Thing>(b => GridsUtility.GetThingList(b.Position, b.Map).Where(t => !(t is Building) && !(t is Mote)))
               .Destroy<Thing>(mode)
               .UnsafeExecute(Things.ToList());

            map.GetSpaceAtmosphereMapComponent().RemoveWholeShip(this);
            map.GetSpaceAtmosphereMapComponent().processUpdates = true;
        }

        internal ThingMutator<Thing>.ThenContainer<Thing> AdaptToNewGame(Map map, int offsetx, int offsety, string LoadingPrefix, bool async, Action<Exception> handler) => 
            new ThingMutator<Thing>()
                .ExpandContained<Building_CryptosleepCasket, Thing>(casket => casket.ContainedThing)
                .Move(oldPos => new IntVec3(oldPos.x + offsetx, oldPos.y, oldPos.z + offsety))
                .SetFaction(Faction.OfPlayer)
                .ClearOwnership()
                .QueueAsLongEvent(thingsToSpawn, LoadingPrefix + "_Adapt", async, handler)
                .Then(() => new ThingMutator<Thing>()
                .For<Pawn>(p =>
                {
                    p.Kill(null);
                })
                .UnsafeExecute(thingsToSpawn), LoadingPrefix + "_Stowaways", handler);

        internal void SpawnInNewGame(Map map, string v1, bool v2, Action<Exception> handler)
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                map.GetSpaceAtmosphereMapComponent().processUpdates = false;
            }, v1 + "_Spawn", v2, handler);
            new ThingMutator<Thing>()
                .For<Thing>(x => x.SpawnSetup(map, false))
                .SetAsHome<Thing>()
                .QueueAsLongEvent(thingsToSpawn, v1 + "_Spawn", v2, handler)
                .Then(() =>
                {
                    thingsInShip = thingsToSpawn.Where(t => t is Building).Select(t => new WeakReference(t)).ToList();
                    map.GetSpaceAtmosphereMapComponent().AddWholeShip(this);
                    map.GetSpaceAtmosphereMapComponent().processUpdates = true;
                    thingsToSpawn = null;
                }, v1 + "_Spawn", handler);

        }

        private float mass = 0.0f;
        public float Mass
        {
            get
            {
                if (mass == 0.0f)
                    mass = Things.Sum(x => x.def.fillPercent * 100000.0f);
                return mass;
            }
        }

        private float thrust = 0.0f;
        public float Thrust
        {
            get
            {
                if (thrust == 0.0f)
                    thrust = Things.Where(x => x.def.defName == "Ship_Engine").Count() * 250000000f;
                return thrust;
            }
        }

        public float TWR
        {
            get
            {
                if(Things.First().Map.terrainGrid.TerrainAt(IntVec3.Zero).defName == "HardVacuum")
                {
                    return Thrust / Mass;
                }
                else
                {
                    //lets just go ahead and assume 1G
                    return Thrust / (Mass * 9.8f);
                }
            }
        }

        public GasVolume GetGas(Room r)
        {
            GasVolume vol = GasVolume.Empty;
            bool found = false;
            foreach(var cell in r.Cells)
            {
                if(positionsInShip.ContainsKey(cell))
                {
                    found = true;
                    vol += new GasVolume(positionsInShip[cell]);
                }
            }

            if(!found)
            {
                return r.Map.IsSpace() 
                    ? new GasVolume(GasMixture.Vacuum, 1000f)
                    : new GasVolume(GasMixture.EarthNorm, 1000f);
            }

            return vol;
        }

        public GasVolume GetGas(RoomGroup rg)
        {
            return rg.Rooms.Aggregate(GasVolume.Empty, (v, r) => v + GetGas(r));
        }

        public class GasCalculator
        {
            private ShipDefinition def;
            private Dictionary<RoomGroup, List<Func<RoomGroup, GasVolume, GasVolume>>> actions = new Dictionary<RoomGroup, List<Func<RoomGroup, GasVolume, GasVolume>>>();
            private List<List<RoomGroup>> groups = new List<List<RoomGroup>>();

            public GasCalculator(ShipDefinition def)
            {
                this.def = def;
            }

            public void Execute()
            {
                var gasDict = new Dictionary<RoomGroup, GasVolume>();
                foreach(var group in actions.Keys)
                {
                    var gas = def.GetGas(group);
                    foreach(var action in actions[group])
                    {
                        gas = action(group, gas);
                    }
                    gasDict[group] = gas;
                }

                foreach(var grouping in groups)
                {
                    var gas = grouping.Aggregate(GasVolume.Empty, (v, r) => v + gasDict[r]);
                    foreach(var grouped in grouping)
                    {
                        gasDict[grouped] = gas;
                    }
                }

                foreach (var group in gasDict.Keys)
                {
                    foreach (var cell in group.Cells)
                    {
                        if (def.positionsInShip.ContainsKey(cell))
                        {
                            def.positionsInShip[cell] = gasDict[group].mixture;
                        }
                    }
                }

            }

            public void Equalize(RoomGroup rg)
            {
                if(!actions.ContainsKey(rg))
                {
                    actions[rg] = new List<Func<RoomGroup, GasVolume, GasVolume>>();
                }

                actions[rg].Add((r, gas) => gas);
            }

            public void Equalize(RoomGroup rg, GasMixture ambient)
            {
                if (!actions.ContainsKey(rg))
                {
                    actions[rg] = new List<Func<RoomGroup, GasVolume, GasVolume>>();
                }

                actions[rg].Add((r, gas) => gas + new GasVolume(ambient, 1000f));
            }

            public void Equalize(IEnumerable<RoomGroup> rg)
            {
                groups.Add(rg.ToList());
                foreach(var r in rg)
                {
                    if (!actions.ContainsKey(r))
                    {
                        actions[r] = new List<Func<RoomGroup, GasVolume, GasVolume>>();
                    }
                }
            }

            public void GasExchange(RoomGroup rg, GasVolume removed = null, GasVolume added = null)
            {
                if (rg == null)
                    return;
                if (!actions.ContainsKey(rg))
                {
                    actions[rg] = new List<Func<RoomGroup, GasVolume, GasVolume>>();
                }

                actions[rg].Add((r, gas) => {
                    float removedAmount;
                    var rGas = gas.removeDirect(removed ?? GasVolume.Empty, out removedAmount);
                    var toAdd = (removed != null && added != null) ?
                        new GasVolume(added.mixture * removedAmount, added.metersCubed)
                        : added ?? GasVolume.Empty;
                    return rGas.addDirect(toAdd);
                });
            }
        }
    }
}
