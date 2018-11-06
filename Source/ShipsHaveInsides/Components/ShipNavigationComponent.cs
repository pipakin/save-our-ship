using ShipsHaveInsides;
using ShipsHaveInsides.MapComponents;
using ShipsHaveInsides.Mod;
using ShipsHaveInsides.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class ShipNavigationComponent : ThingComp
    {
        private Building parentBuilding
        {
            get { return parent as Building; }
        }

        private bool CanLaunchNow => !ShipUtility.LaunchFailReasons(parentBuilding).Any();
        private bool CanOrbitNow => !ShipUtility.LaunchFailReasons(parentBuilding).Any(x => x != Translator.Translate("ShipReportNoFullPods"));

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action launch = new Command_Action
            {
                action = TryLaunch,
                defaultLabel = "CommandShipLaunch".Translate(),
                defaultDesc = "CommandShipLaunchDesc".Translate()
            };
            if (!CanLaunchNow)
            {
                launch.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                launch.Disable();
            }
            launch.hotKey = KeyBindingDefOf.Misc1;
            launch.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            Command_Action orbit = new Command_Action
            {
                action = TryOrbit,
                defaultLabel = "ShipInsideOrbit".Translate(),
                defaultDesc = "ShipInsideOrbitDesc".Translate()
            };
            if (!CanOrbitNow)
            {
                orbit.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                orbit.Disable();
            }
            orbit.hotKey = KeyBindingDefOf.Misc1;
            orbit.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            Command_Action land = new Command_Action
            {
                action = TryLand,
                defaultLabel = "ShipInsideLand".Translate(),
                defaultDesc = "ShipInsideLandDesc".Translate()
            };
            if (!CanOrbitNow)
            {
                land.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                land.Disable();
            }
            land.hotKey = KeyBindingDefOf.Misc1;
            land.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            ShipDefinition def = parent.Map.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);

            Command_Action shortJump = new Command_Action
            {
                action = TryShortJump,
                defaultLabel = "ShipInsideShortJump".Translate(),
                defaultDesc = "ShipInsideShortJumpDesc".Translate()
            };
            if (ShipCountdown.CountingDown)
            {
                shortJump.Disable();
            }
            shortJump.hotKey = KeyBindingDefOf.Misc1;
            shortJump.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            Command_Action renameShip = new Command_Action
            {
                action = delegate
                {
                    Find.WindowStack.Add(new Dialog_NameShip(def));
                },
                defaultLabel = "ShipInsideRename".Translate(),
                defaultDesc = "ShipInsideRenameDesc".Translate()
            };
            if (ShipCountdown.CountingDown)
            {
                renameShip.Disable();
            }
            renameShip.hotKey = KeyBindingDefOf.Misc1;
            renameShip.icon = ContentFinder<Texture2D>.Get("UI/Commands/RenameZone");

            // Soon... so soon...
            List<Gizmo> gizmos = base.CompGetGizmosExtra().ToList();

            gizmos.Add(launch);

            if(parent.Map.terrainGrid.TerrainAt(IntVec3.Zero).defName == "HardVacuum")
            {
                if (def != null) gizmos.Add(land);
                if (def.ReadyForShortRangeJump)
                {
                    gizmos.Add(shortJump);
                }
            }
            else
            {
                if (def != null) gizmos.Add(orbit);
            }

            if (def != null)
            {
                gizmos.Add(renameShip);
            }

            return gizmos;
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder(base.CompInspectStringExtra());

            var def = parent.Map.GetSpaceAtmosphereMapComponent().DefinitionAt(parent.Position);

            if (def != null)
            {
                sb.Append("Controlling Ship: ");
                sb.AppendLine(def.Name);
                sb.Append("Total Mass: ");
                sb.Append(def.Mass.ToString("N0"));
                sb.AppendLine("kg");
                sb.Append("Total Thrust: ");
                sb.Append(def.Thrust.ToString("N0"));
                sb.AppendLine("kN");
                sb.Append("TWR: ");
                sb.Append(def.TWR.ToString("N3"));
            }

            return sb.ToString();
        }

        private void TryShortJump()
        {
            //move ship there
            Map newMap = null;
            Map oldMap = parent.Map;

            var def = oldMap
                .GetSpaceAtmosphereMapComponent()
                .DefinitionAt(parent.Position);

            ShipInteriorMod.Log("Adding world object...");
            Planet.MapParent obj = Planet.WorldObjectMaker.MakeWorldObject(GenDefDatabase.GetDef(typeof(WorldObjectDef), "OrbitShip") as WorldObjectDef) as Planet.MapParent;
            obj.Tile = def.Tile.Value;

            Find.World.worldObjects.Add(obj);

            //create world map pawn for ship
            LongEventHandler.QueueLongEvent(() =>
            {
                ShipInteriorMod.Log("Generating map...");
                //generate orbit map
                var generatorDef = GenDefDatabase.GetDef(typeof(MapGeneratorDef), "Orbit") as MapGeneratorDef;
                ShipInteriorMod.noSpaceWeather = true;
                newMap = MapGenerator.GenerateMap(oldMap.Size, obj, generatorDef, obj.ExtraGenStepDefs, null);
                ShipInteriorMod.noSpaceWeather = false;
            }, "ShortJump_Generate", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
            def.Move(oldMap, () => newMap, "ShortJump", true, Handler, (AirShipWorldObject)obj)
                .Then(() =>
                {
                    HashSet<RoomGroup> processedGroups = new HashSet<RoomGroup>();
                    foreach (var room in newMap.regionGrid.allRooms)
                    {
                        var group = room.Group;
                        if (!processedGroups.Contains(group))
                        {
                            processedGroups.Add(group);
                            group.Temperature = 21f; // initialize life support
                        }
                    }
                }, "ShortJump_Temperature", Handler)
                .Then(() =>
                {
                    //generate some meteors
                    ShipInteriorMod.Log("Generating meteors...");
                    int numMeteors = Rand.Range(ShipInteriorMod.instance.meteorMinCount.Value, ShipInteriorMod.instance.meteorMaxCount.Value);
                    for (int i = 0; i < numMeteors; i++)
                    {
                        if (!TryFindCell(out IntVec3 cell, newMap))
                        {
                            ShipInteriorMod.Log("Nowhere for meteor!?!");
                            continue;
                        }

                        ShipInteriorMod.Log("Found cell for meteor!");

                        List<Thing> list = new List<Thing>();
                        for (int m = 0; m < ShipInteriorMod.instance.meteorSizeMultiplier.Value; m++)
                        {
                            list.AddRange(ThingSetMakerDefOf.Meteorite.root.Generate());
                        }
                        ShipInteriorMod.Log("Meteor has " + list.Count + " chunks!");
                        for (int num = list.Count - 1; num >= 0; num--)
                        {
                            ShipInteriorMod.Log("Placing chunk!");
                            GenPlace.TryPlaceThing(list[num], cell, newMap, ThingPlaceMode.Near, (Thing thing, int count) =>
                            {
                                PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
                            });
                        }
                    }
                }, "ShortJump_Meteors", Handler)
                .Then(() => { Current.Game.CurrentMap = newMap; }, "ShortJump_Swap", Handler)
                .Then(() => {
                    foreach (var cell in Find.CurrentMap.areaManager.Home.ActiveCells.ToList())
                    {
                        if (!Find.CurrentMap.thingGrid.ThingsAt(cell).Any(x => x.def.defName == "ShipHullTile" || x.def.defName == "ShipAirlock"))
                        {
                            Find.CurrentMap.areaManager.Home[cell] = false;
                        }
                    }
                    foreach (var pawn in Find.CurrentMap.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer))
                    {
                        pawn.playerSettings.AreaRestriction = Find.CurrentMap.areaManager.Home;
                    }
                }, "ShortJump_Swap", Handler)
                .Then(() =>
                {
                    Find.World.worldObjects.Remove(oldMap.Parent);
                    Find.Maps.Remove(oldMap);
                }, "ShortJump_DestroyOldMap", Handler);
        }

        private void TryLand()
        {

            //move ship there
            Map oldMap = parent.Map;
            Map newMap = Find.Maps.FirstOrDefault(x => x.Tile == oldMap.Tile && x != oldMap);
            if(newMap == null)
            {
                ///OH NOES!
                throw new Exception("WTF");
            }

            var def = oldMap
                .GetSpaceAtmosphereMapComponent()
                .DefinitionAt(parent.Position);

            IntVec3? landingSpot = null;

            LongEventHandler.QueueLongEvent(() =>
            {
                landingSpot = def.DetermineLandingSpot(newMap);
                if(landingSpot == null)
                {
                    Find.LetterStack.ReceiveLetter("No Landing Spot", "No suitable landing spot found.", LetterDefOf.NeutralEvent);
                    LongEventHandler.ClearQueuedEvents();
                }
            }, "Landing_Spot", true, Handler);
            
            def
                .Move(oldMap, () => newMap, "Landing", true, Handler, null, () => landingSpot.Value)
                .Then(() =>
                {
                    Current.Game.CurrentMap = newMap;
                }, "Landing_Swap", Handler)
                .Then(() => {
                     foreach (var pawn in Find.CurrentMap.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer)) {
                         pawn.playerSettings.AreaRestriction = Find.CurrentMap.areaManager.Home;
                     }
                }, "Landing_Swap", Handler)
                .Then(() =>
                {
                    Find.World.worldObjects.Remove(oldMap.Parent);
                    Find.Maps.Remove(oldMap);
                }, "Landing_DestroyOldMap", Handler);


            //do cool graphic?
        }

        private bool TryFindCell(out IntVec3 cell, Map map)
        {
            IntRange mineablesCountRange = ThingSetMaker_Meteorite.MineablesCountRange;
            int maxMineables = mineablesCountRange.max;
            return CellFinderLoose.TryFindSkyfallerCell(ThingDefOf.MeteoriteIncoming, map, out cell, 10, default(IntVec3), -1, true, false, false, false, true, true, (IntVec3 x) =>
            {
                int num = Mathf.CeilToInt(Mathf.Sqrt((float)maxMineables)) + 2;
                CellRect cellRect = CellRect.CenteredOn(x, num, num);
                int num2 = 0;
                CellRect.CellRectIterator iterator = cellRect.GetIterator();
                while (!iterator.Done())
                {
                    if (iterator.Current.InBounds(map) && iterator.Current.Standable(map))
                    {
                        num2++;
                    }
                    iterator.MoveNext();
                }
                return num2 >= maxMineables;
            });
        }

        private void TryOrbit()
        {

            //move ship there
            Map newMap = null;
            Map oldMap = parent.Map;

            ShipInteriorMod.Log("Adding world object...");
            Planet.MapParent obj = Planet.WorldObjectMaker.MakeWorldObject(GenDefDatabase.GetDef(typeof(WorldObjectDef), "OrbitShip") as WorldObjectDef) as Planet.MapParent;
            obj.Tile = oldMap.Tile;

            Find.World.worldObjects.Add(obj);

            //create world map pawn for ship
            LongEventHandler.QueueLongEvent(() =>
            {
                ShipInteriorMod.Log("Generating map...");
                //generate orbit map
                var generatorDef = GenDefDatabase.GetDef(typeof(MapGeneratorDef), "Orbit") as MapGeneratorDef;
                ShipInteriorMod.noSpaceWeather = true;
                newMap = MapGenerator.GenerateMap(oldMap.Size, obj, generatorDef, obj.ExtraGenStepDefs, null);
                ShipInteriorMod.noSpaceWeather = false;
            }, "Orbit_Generate", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);

            oldMap
                .GetSpaceAtmosphereMapComponent()
                .DefinitionAt(parent.Position)
                .Move(oldMap, () => newMap, "Orbit", true, Handler, (AirShipWorldObject)obj)
                .Then(() =>
                {
                    HashSet<RoomGroup> processedGroups = new HashSet<RoomGroup>();
                    foreach(var room in newMap.regionGrid.allRooms)
                    {
                        var group = room.Group;
                        if(!processedGroups.Contains(group))
                        {
                            processedGroups.Add(group);
                            group.Temperature = 21f; // initialize life support
                        }
                    }
                }, "Orbit_Temperature", Handler)
                .Then(() =>
                {
                    //generate some meteors
                    ShipInteriorMod.Log("Generating meteors...");
                    int numMeteors = Rand.Range(ShipInteriorMod.instance.meteorMinCount.Value, ShipInteriorMod.instance.meteorMaxCount.Value);
                    for (int i = 0; i < numMeteors; i++)
                    {
                        if (!TryFindCell(out IntVec3 cell, newMap))
                        {
                            ShipInteriorMod.Log("Nowhere for meteor!?!");
                            continue;
                        }

                        ShipInteriorMod.Log("Found cell for meteor!");

                        List<Thing> list = new List<Thing>();
                        for (int m = 0; m < ShipInteriorMod.instance.meteorSizeMultiplier.Value; m++)
                        {
                            list.AddRange(ThingSetMakerDefOf.Meteorite.root.Generate());
                        }
                        ShipInteriorMod.Log("Meteor has " + list.Count + " chunks!");
                        for (int num = list.Count - 1; num >= 0; num--)
                        {
                            ShipInteriorMod.Log("Placing chunk!");
                            GenPlace.TryPlaceThing(list[num], cell, newMap, ThingPlaceMode.Near, (Thing thing, int count) =>
                            {
                                PawnUtility.RecoverFromUnwalkablePositionOrKill(thing.Position, thing.Map);
                            });
                        }
                    }
                }, "Orbit_Meteors", Handler)
                .Then(() => { Current.Game.CurrentMap = newMap; }, "Orbit_Swap", Handler)
                .Then(() => {
                    foreach(var cell in Find.CurrentMap.areaManager.Home.ActiveCells.ToList())
                    {
                        if(!Find.CurrentMap.thingGrid.ThingsAt(cell).Any(x => (x.def.building?.shipPart).GetValueOrDefault(false)))
                        {
                            Find.CurrentMap.areaManager.Home[cell] = false;
                        }
                    }
                    foreach(var pawn in Find.CurrentMap.mapPawns.AllPawns.Where(p => p.Faction == Faction.OfPlayer)) {
                        pawn.playerSettings.AreaRestriction = Find.CurrentMap.areaManager.Home;
                    }
                }, "Orbit_Swap", Handler);


            //do cool graphic?
        }

        private void Handler(Exception e) => ShipInteriorMod.instLogger.Error("Error during transition: {0}", e.Message);

        private void TryLaunch()
        {
            if (CanLaunchNow)
            {
                ShipCountdown.InitiateCountdown(parentBuilding);
            }
        }
    }
}
