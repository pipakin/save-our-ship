using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace ShipsHaveInsides.MapComponents
{
    public class SpaceAtmosphereMapComponent : MapComponent
    {
        private List<ShipDefinition> shipDefinitions = new List<ShipDefinition>();

        public SpaceAtmosphereMapComponent(Map map) : base(map)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();

            //save ships
            Scribe_Collections.Look(ref shipDefinitions, "shipDefinitions", LookMode.Deep);
            if (shipDefinitions == null) shipDefinitions = new List<ShipDefinition>();
        }

        public override void MapGenerated()
        {
            base.MapGenerated();
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksAbs % 75 != 0) return;

            //move atmosphere around!
            foreach (var def in shipDefinitions)
            {
                var shipRooms = map.regionGrid.allRooms.Where(x => x.Cells.Any(p => def.Positions.Contains(p)));
                var shipRoomGroups = shipRooms.Select(x => x.Group).Distinct();
                var vents = def.Things.OfType<BuildingLifeSupportVent>().ToList();
                var roomGroupsWithVents = vents
                    .Select(x => x.GetRoom(RegionType.Set_All).Group);

                var ventingGroups = shipRoomGroups.Where(x => x.OpenRoofCount > 0 || x.AnyRoomTouchesMapEdge).ToList();

                var gasCalc = new ShipDefinition.GasCalculator(def);

                foreach (var ventGroup in ventingGroups)
                {
                    gasCalc.Equalize(ventGroup, map.IsSpace() ? GasMixture.Vacuum : GasMixture.EarthNorm);
                }

                foreach(var group in shipRoomGroups)
                {
                    gasCalc.Equalize(group);
                }

                gasCalc.Equalize(roomGroupsWithVents);

                var breathingPawns = map.mapPawns.
                    PawnsInFaction(Faction.OfPlayer).Where(p => p.def.race.FleshType == FleshTypeDefOf.Normal)
                    .Select(x => new Pair<Pawn, RoomGroup>(x, x.GetRoomGroup()));

                foreach(var pawn in breathingPawns)
                {
                    //do some calc to decide the amount?
                    gasCalc.GasExchange(
                        rg: pawn.Second, 
                        removed: new GasVolume(new GasMixture(0f, 20f, 0f), 0.3f), 
                        added: new GasVolume(new GasMixture(0f, 16f, 4f), 0.3f)
                        );
                }

                var plants = def.Things.OfType<Building_PlantGrower>();

                foreach (var plantGrower in plants)
                {
                    //do some calc to decide the amount?
                    gasCalc.GasExchange(
                        rg: plantGrower.GetRoomGroup(),
                        removed: new GasVolume(new GasMixture(0f, 0f, 2f * plantGrower.PlantsOnMe.Count()), 0.3f),
                        added: new GasVolume(new GasMixture(0f, 2f * plantGrower.PlantsOnMe.Count(), 0f), 0.3f)
                        );
                }

                var doorRegion = def.Things.OfType<Building_Door>().ToList()
                    .Where(x => x.Open || !map.IsSpace())
                    .Select(x => x.GetRegion(RegionType.Portal))
                    .Where(x => x != null)
                    .SelectMany(x => x.links)
                    .Where(x => x.RegionA.Room.Group != x.RegionB.Room.Group);

                foreach(var reg in doorRegion)
                {
                    gasCalc.Equalize(new RoomGroup[] { reg.RegionA.Room.Group, reg.RegionB.Room.Group });
                }                

                gasCalc.Execute();

                foreach ( var room in shipRooms)
                {
                    //update stats
                    room.Notify_BedTypeChanged();
                }
            }
        }

        public bool processUpdates = false;
        public void Register(Thing t)
        {
            if (!processUpdates || !(t is Building)) return;
            ShipDefinition.ProcessNewThing(shipDefinitions, t);
        }

        public void Deregister(Thing t)
        {
            if (!processUpdates || !(t is Building)) return;
            List<ShipDefinition> defsToRemove = new List<ShipDefinition>();
            List<ShipDefinition> defsToAdd = new List<ShipDefinition>();
            foreach (var def in shipDefinitions)
            {
                var newDefs = def.RemoveFromShip(t);

                if (newDefs != null)
                {
                    defsToRemove.Add(def);
                    defsToAdd.AddRange(newDefs);
                }
            }

            shipDefinitions.RemoveAll(p => defsToRemove.Contains(p));
            shipDefinitions.AddRange(defsToAdd);
        }

        public override void MapComponentUpdate()
        {
            if (ShipsHaveInsides.Mod.ShipInteriorMod.instance.drawDebugShipRegions.Value)
            {
                if (Find.CurrentMap == map)
                {

                    //add debug flag.
                    foreach (var def in shipDefinitions)
                    {
                        GenDraw.DrawFieldEdges(def.Positions.ToList(), Color.blue);
                    }
                }
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();

            if (ShipsHaveInsides.Mod.ShipInteriorMod.instance.drawDebugShipRegions.Value)
            {
                if (Find.CurrentMap == map)
                {

                    //add debug flag.
                    foreach (var def in shipDefinitions)
                    {
                        Vector3 drawPos = def.Center.ToVector3();
                        drawPos.z += -0.4f;
                        Vector2 result = Find.Camera.WorldToScreenPoint(drawPos) / Prefs.UIScale;
                        result.y = (float)UI.screenHeight - result.y;

                        Text.Font = GameFont.Medium;
                        Vector2 vector = Text.CalcSize(def.Name);
                        float x = vector.x;
                        Rect position = new Rect(result.x - x / 2f - 4f, result.y, x + 8f, 12f);
                        GUI.DrawTexture(position, TexUI.GrayTextBG);
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.UpperCenter;
                        Rect rect = new Rect(result.x - x / 2f, result.y - 3f, x, 999f);
                        Widgets.Label(rect, def.Name);
                        GUI.color = Color.white;
                        Text.Anchor = TextAnchor.MiddleCenter;
                        Text.Font = GameFont.Small;
                    }
                }
            }
        }

        public override void FinalizeInit()
        {
            processUpdates = true;
            var things = map.listerThings.AllThings.Where(p => p.def.building?.shipPart == true).ToList();
            for (int i = 0; i < things.Count; i++)
            {
                float pct = (float)i / (float)things.Count;
                LongEventHandler.SetCurrentEventText("Ship Construction: " + pct.ToString("P"));

                Register(things[i]);
            }
        }

        public ShipDefinition DefinitionAt(IntVec3 position)
        {
            return shipDefinitions.FirstOrDefault(p => p.Positions.Contains(position));
        }

        public void AddWholeShip(ShipDefinition def)
        {
            shipDefinitions.Add(def);
        }

        public void RemoveWholeShip(ShipDefinition def)
        {
            shipDefinitions.Remove(def);
        }
    }
}
