using RimWorld;
using RimWorld.Planet;
using ShipsHaveInsides.Mod;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using System.Linq;
using ShipsHaveInsides.Utilities;

namespace RimWorld
{
    public class ScenPart_LandShip : ScenPart
    {
        public string shipFactionName;
        private IntVec3 highCorner;
        private IntVec3 lowCorner;

        private ShipInteriorMod Mod
        {
            get
            {
                return ShipInteriorMod.instance;
            }
        }

        public override void GenerateIntoMap(Map map)
        {
        }

        public override void PostWorldGenerate()
        {
            Find.GameInitData.startingPawnCount = 0;
        }

        public override void PostMapGenerate(Map map)
        {
            if (Find.GameInitData == null)
                return;
            string str1 = Path.Combine(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships"), shipFactionName + ".rwship");
            Scribe.loader.InitLoading(str1);

            FactionDef factionDef = Faction.OfPlayer.def;
            ShipInteriorMod.Log(factionDef.fixedName);
            List<Thing> thingList1 = new List<Thing>();

            ShipInteriorMod.Log("Loading base managers...");
            Scribe_Deep.Look(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.tickManager, false, "tickManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
            Scribe_Deep.Look(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);

            //Advancing time
            ShipInteriorMod.Log("Advancing time...");
            Current.Game.tickManager.DebugSetTicksGame(Current.Game.tickManager.TicksAbs + 3600000 * Rand.RangeInclusive(Mod.minTravelTime.Value, Mod.maxTravelTime.Value));

            Scribe_Collections.Look(ref thingList1, "things", (LookMode)2, new object[0]);
            
            this.highCorner = thingList1.Aggregate(new IntVec3(0, 0, 0), 
                (highCorner, t) => new IntVec3(Math.Max(highCorner.x, t.Position.x), 0, Math.Max(highCorner.z, t.Position.z)));
            this.lowCorner = thingList1.Aggregate(new IntVec3(int.MaxValue, 0, int.MaxValue), 
                (lowCorner, t) => new IntVec3(Math.Min(lowCorner.x, t.Position.x), 0, Math.Min(lowCorner.z, t.Position.z)));

            IntVec3 spot = MapGenerator.PlayerStartSpot;
            int width = highCorner.x - lowCorner.x;
            int height = highCorner.z - lowCorner.z;

            //try to position us over the start location
            spot.x -= width / 2;
            spot.z -= height / 2;

            //now offset the corners and the parts to the spot.
            int offsetx = spot.x - lowCorner.x;
            int offsety = spot.z - lowCorner.z;

            lowCorner.x += offsetx;
            lowCorner.z += offsety;
            highCorner.x += offsetx;
            highCorner.z += offsety;

            ShipInteriorMod.Log("Low Corner: " + lowCorner.x + ", " + lowCorner.y + ", " + lowCorner.z);
            ShipInteriorMod.Log("High Corner: " + highCorner.x + ", " + highCorner.y + ", " + highCorner.z);
            ShipInteriorMod.Log("Map Size: " + map.Size.x + ", " + map.Size.y + ", " + map.Size.z);

            ShipInteriorMod.Log("Adapting ship for world...");
            new ThingMutator<Thing>()
                .ExpandContained<Building_CryptosleepCasket, Pawn>(casket => casket.ContainedThing)
                .Move(oldPos => new IntVec3(oldPos.x + offsetx, oldPos.y, oldPos.z + offsety))
                .SetFaction(Faction.OfPlayer)
                .ClearOwnership()
                .UnsafeExecute(thingList1);

            ShipInteriorMod.Log("Killing Everyone stowed away...");
            new ThingMutator<Thing>()
                .For<Pawn>(p =>
                {
                    p.Kill(null);
                })
                .UnsafeExecute(thingList1);

            ShipInteriorMod.Log("Cleaning map...");
            new MapScanner()
                .DestroyThings(-2, 2)
                .ForPoints((point, m) => m.roofGrid.SetRoof(point, null), -2, 2)
                .ForPoints((point, m) => m.terrainGrid.SetTerrain(point, TerrainDefOf.Gravel))
                .Unfog(-3, 3)
                .UnsafeExecute(map, lowCorner, highCorner);

            ShipInteriorMod.Log("Spawning ship...");
            new ThingMutator<Thing>()
                .For<Thing>(x => x.SpawnSetup(map, false))
                .UnsafeExecute(thingList1);

            ShipInteriorMod.Log("Loading managers...");
            Scribe_Deep.Look(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.taleManager, false, "taleManager", new object[0]);
            Scribe_Deep.Look(ref Current.Game.playLog, false, "playLog", new object[0]);
            Scribe.loader.FinalizeLoading();

            Faction.OfPlayer.Name = this.shipFactionName;

            ShipInteriorMod.Log("Done.");
        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(scen, "LandShip", Translator.Translate(nameof(ScenPart_LandShip)));
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "LandShip")
                yield return shipFactionName;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref shipFactionName, "shipFactionName", null, false);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            if (!Widgets.ButtonText(listing.GetScenPartRect(this, RowHeight), shipFactionName, true, false, true))
                return;
            List<FloatMenuOption> floatMenuOptionList = new List<FloatMenuOption>();
            List<string> stringList = new List<string>();
            stringList.AddRange(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            foreach (string str in stringList)
            {
                string ship = str;
                floatMenuOptionList.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(ship), (Action)(() => this.shipFactionName = Path.GetFileNameWithoutExtension(ship)), (MenuOptionPriority)4, (Action)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null));
            }
            Find.WindowStack.Add(new FloatMenu(floatMenuOptionList));
        }

        public override void Randomize()
        {
            List<string> stringList = new List<string>();
            stringList.AddRange(Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            shipFactionName = Path.GetFileNameWithoutExtension(stringList.RandomElement());
        }

        public override bool CanCoexistWith(ScenPart other)
        {
            if (other is ScenPart_LandShip || other is ScenPart_StartingAnimal || (other is ScenPart_StartingThing_Defined || other is ScenPart_ScatterThingsNearPlayerStart))
                return false;
            if (other is ScenPart_ConfigPage_ConfigureStartingPawns)
                Find.Scenario.RemovePart(other);
            if (other is ScenPart_PlayerPawnsArriveMethod)
                Find.Scenario.RemovePart(other);
            return true;
        }
    }
}
