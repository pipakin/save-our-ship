using ShipsHaveInsides.Mod;
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
            if (!CanLaunchNow)
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
            if (!CanLaunchNow)
            {
                land.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                land.Disable();
            }
            land.hotKey = KeyBindingDefOf.Misc1;
            land.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            // Soon... so soon...
            List<Gizmo> gizmos = base.CompGetGizmosExtra().ToList();

            gizmos.Add(launch);

            if(parent.Map.terrainGrid.TerrainAt(IntVec3.Zero).defName == "HardVacuum")
            {
                gizmos.Add(land);
            }
            else
            {
                gizmos.Add(orbit);
            }

            return gizmos;
        }

        private void TryLand()
        {

            //move ship there
            Map oldMap = parent.Map;
            Map newMap = Find.Maps.First(x => x.Tile == oldMap.Tile && x != oldMap);
            if(newMap == null)
            {
                ///OH NOES!
                throw new Exception("WTF");
            }

            //create world map pawn for ship
            LongEventHandler.QueueLongEvent(() =>
            {
                List<Building> buildingList = ShipUtility.ShipBuildingsAttachedTo(parentBuilding);
                foreach (var item in buildingList)
                {
                    var loc = item.Position;
                    var map = oldMap;
                    item.DeSpawn();
                    item.SpawnSetup(newMap, false);
                    foreach (var nonBuilding in GridsUtility.GetThingList(loc, oldMap).Where(t => !(t is Building) && !(t is Mote)).ToList())
                    {
                        nonBuilding.DeSpawn();
                        nonBuilding.SpawnSetup(newMap, false);
                    }
                }

                Current.Game.CurrentMap = newMap;
            }, "GeneratingMap", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);

            LongEventHandler.QueueLongEvent(() =>
            {
                Find.World.worldObjects.Remove(oldMap.Parent);
                Find.Maps.Remove(oldMap);
            }, "Landing", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);


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

            Planet.MapParent obj = Planet.WorldObjectMaker.MakeWorldObject(GenDefDatabase.GetDef(typeof(WorldObjectDef), "OrbitShip") as WorldObjectDef) as Planet.MapParent;
            obj.Tile = oldMap.Tile;

            Find.World.worldObjects.Add(obj);

            //create world map pawn for ship
            LongEventHandler.QueueLongEvent(() =>
            {
                //generate orbit map
                var generatorDef = GenDefDatabase.GetDef(typeof(MapGeneratorDef), "Orbit") as MapGeneratorDef;
                newMap = MapGenerator.GenerateMap(oldMap.Size, obj, generatorDef, obj.ExtraGenStepDefs, null);
            }, "GeneratingMap", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
            LongEventHandler.QueueLongEvent(() =>
            {
                List<Building> buildingList = ShipUtility.ShipBuildingsAttachedTo(parentBuilding);
                foreach (var item in buildingList)
                {
                    var loc = item.Position;
                    var map = oldMap;
                    item.DeSpawn();
                    item.SpawnSetup(newMap, false);
                    foreach (var nonBuilding in GridsUtility.GetThingList(loc, oldMap).Where(t => !(t is Building) && !(t is Mote)).ToList())
                    {
                        nonBuilding.DeSpawn();
                        nonBuilding.SpawnSetup(newMap, false);
                    }
                }

                //generate some meteors
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
                Current.Game.CurrentMap = newMap;
            }, "GeneratingMap", doAsynchronously: true, exceptionHandler: GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);

            //do cool graphic?
        }

        private void TryLaunch()
        {
            if (CanLaunchNow)
            {
                ShipCountdown.InitiateCountdown(parentBuilding);
            }
        }
    }
}
