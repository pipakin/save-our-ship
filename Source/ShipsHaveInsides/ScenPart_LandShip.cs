using RimWorld;
using RimWorld.Planet;
using ShipsHaveInsides.Mod;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;

namespace Rimworld
{
    public class ScenPart_LandShip : ScenPart
    {
        public string shipFactionName;
        private IntVec3 highCorner;
        private IntVec3 lowCorner;

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
            string str1 = Path.Combine(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships"), this.shipFactionName + ".rwship");
            ((ScribeLoader)Scribe.loader).InitLoading(str1);
            string str2 = "";
            // ISSUE: cast to a reference type
            FactionDef factionDef = Faction.OfPlayer.def;
            ShipInteriorMod.Log((string)factionDef.fixedName);
            List<Thing> thingList1 = new List<Thing>();
            // ISSUE: cast to a reference type
            Scribe_Collections.Look<Thing>(ref thingList1, "things", (LookMode)2, new object[0]);
            List<Thing> thingList2 = new List<Thing>();
            this.lowCorner = new IntVec3(int.MaxValue, int.MaxValue, int.MaxValue);
            this.highCorner = new IntVec3(0, 0, 0);
            IntVec3 position;
            using (List<Thing>.Enumerator enumerator = thingList1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    position = enumerator.Current.Position;
                    if (position.x < this.lowCorner.x)
                        this.lowCorner.x = position.x;
                    else if (position.x > this.highCorner.x)
                        this.highCorner.x = position.x;
                    if (position.z < this.lowCorner.z)
                        this.lowCorner.z = position.z;
                    else if (position.z > this.highCorner.z)
                        this.highCorner.z = position.z;
                }
            }
            for (int x = (int)this.lowCorner.x; x <= this.highCorner.x; ++x)
            {
                for (int z = (int)this.lowCorner.z; z <= this.highCorner.z; ++z)
                {
                    position = new IntVec3(x, 0, z);
                    thingList2.AddRange(((ThingGrid)map.thingGrid).ThingsAt(position));
                }
            }
            using (List<Thing>.Enumerator enumerator = thingList2.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    enumerator.Current.Destroy((DestroyMode)0);
            }
            using (List<Thing>.Enumerator enumerator = thingList1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                    ((Entity)enumerator.Current).SpawnSetup(map, false);
            }
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);
            ((ScribeLoader)Scribe.loader).FinalizeLoading();
            for (int x = (int)this.lowCorner.x; x <= this.highCorner.x; ++x)
            {
                for (int z = (int)this.lowCorner.z; z <= this.highCorner.z; ++z)
                {
                    position = new IntVec3(x, 0, z);
                    ((FogGrid)map.fogGrid).Unfog(position);
                }
            }
            //Faction allFaction = (((FactionManager)Current.Game.World.factionManager).AllFactions as IList<Faction>)[GenCollection.FirstIndexOf<Faction>(((FactionManager)Current.Game.World.factionManager).AllFactions, (theFac => theFac.IsPlayer))];
            //allFaction.Name = this.shipFactionName;
            //allFaction.def = factionDef;
            Faction.OfPlayer.Name = this.shipFactionName;
        }

        public override string Summary(Scenario scen)
        {
            return ScenSummaryList.SummaryWithList(scen, "LandShip", Translator.Translate(nameof(ScenPart_LandShip)));
        }

        public override IEnumerable<string> GetSummaryListEntries(string tag)
        {
            if (tag == "LandShip")
                yield return this.shipFactionName;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            // ISSUE: cast to a reference type
            Scribe_Values.Look<string>(ref this.shipFactionName, "shipFactionName", null, false);
        }

        public override void DoEditInterface(Listing_ScenEdit listing)
        {
            if (!Widgets.ButtonText(listing.GetScenPartRect((ScenPart)this, ScenPart.RowHeight), this.shipFactionName, true, false, true))
                return;
            List<FloatMenuOption> floatMenuOptionList = new List<FloatMenuOption>();
            List<string> stringList = new List<string>();
            stringList.AddRange((IEnumerable<string>)Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            foreach (string str in stringList)
            {
                string ship = str;
                floatMenuOptionList.Add(new FloatMenuOption(Path.GetFileNameWithoutExtension(ship), (Action)(() => this.shipFactionName = Path.GetFileNameWithoutExtension(ship)), (MenuOptionPriority)4, (Action)null, (Thing)null, 0.0f, (Func<Rect, bool>)null, (WorldObject)null));
            }
            Find.WindowStack.Add((Window)new FloatMenu(floatMenuOptionList));
        }

        public override void Randomize()
        {
            List<string> stringList = new List<string>();
            stringList.AddRange((IEnumerable<string>)Directory.GetFiles(Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships")));
            this.shipFactionName = Path.GetFileNameWithoutExtension((string)GenCollection.RandomElement<string>(stringList));
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
