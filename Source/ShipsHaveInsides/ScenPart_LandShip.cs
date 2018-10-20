using RimWorld;
using RimWorld.Planet;
using ShipsHaveInsides.Mod;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Verse;
using System.Linq;

namespace RimWorld
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

            FactionDef factionDef = Faction.OfPlayer.def;
            ShipInteriorMod.Log((string)factionDef.fixedName);
            List<Thing> thingList1 = new List<Thing>();
            
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

            //TODO: Find optimal placement near start position
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

            using (List<Thing>.Enumerator enumerator = thingList1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    IntVec3 oldPos = enumerator.Current.Position;
                    enumerator.Current.Position = new IntVec3(oldPos.x + offsetx, oldPos.y, oldPos.z + offsety);

                    if(enumerator.Current is Pawn)
                    {
                        (enumerator.Current as Pawn).SetFactionDirect(Faction.OfPlayer);
                        (enumerator.Current as Pawn).pather.nextCell.x += offsetx;
                        (enumerator.Current as Pawn).pather.nextCell.z += offsety;
                        if ((enumerator.Current as Pawn).ownership == null)
                        {
                            ShipInteriorMod.Log("No ownership for: " + (enumerator.Current as Pawn).Name.ToStringFull);
                        }
                        else
                        {
                            ShipInteriorMod.Log("Clearing ownership for: " + (enumerator.Current as Pawn).Name.ToStringFull);
                            (enumerator.Current as Pawn).ownership.UnclaimAll();
                        }
                    } else if(enumerator.Current is Thing)
                    {
                        Thing t = (enumerator.Current as Thing);
                        if(t.def.CanHaveFaction)
                            t.SetFactionDirect(Faction.OfPlayer);

                        if(t is Building_CryptosleepCasket)
                        {
                            Building_CryptosleepCasket casket = (t as Building_CryptosleepCasket);
                            Thing contained = casket.ContainedThing;

                            if(contained != null && contained is Pawn)
                            {
                                if (contained.def.CanHaveFaction)
                                    contained.SetFactionDirect(Faction.OfPlayer);

                                if ((contained as Pawn).ownership == null)
                                {
                                    ShipInteriorMod.Log("No ownership for: " + (contained as Pawn).Name.ToStringFull);
                                }
                                else
                                {
                                    ShipInteriorMod.Log("Clearing ownership for: " + (contained as Pawn).Name.ToStringFull);
                                    (contained as Pawn).ownership.UnclaimAll();
                                }
                            }
                        }
                    }
                }
            }


            ShipInteriorMod.Log("Getting conflicting items...");
            for (int x = (int)this.lowCorner.x - 1; x <= this.highCorner.x + 1; ++x)
            {
                for (int z = (int)this.lowCorner.z - 1; z <= this.highCorner.z + 1; ++z)
                {
                    position = new IntVec3(x, 0, z);
                    //ShipInteriorMod.Log("Getting Item at: " + position.x + ", " + position.y + ", " + position.z);
                    thingList2.AddRange(((ThingGrid)map.thingGrid).ThingsAt(position));
                }
            }
            using (List<Thing>.Enumerator enumerator = thingList2.GetEnumerator())
            {

                ShipInteriorMod.Log("Deleting conflicting items...");
                while (enumerator.MoveNext())
                    enumerator.Current.Destroy((DestroyMode)0);
            }

            ShipInteriorMod.Log("Removing conflicting roofs...");
            for (int x = (int)this.lowCorner.x - 1; x <= this.highCorner.x + 1; ++x)
            {
                for (int z = (int)this.lowCorner.z - 1; z <= this.highCorner.z + 1; ++z)
                {
                    position = new IntVec3(x, 0, z);
                    map.roofGrid.SetRoof(position, null);
                }
            }

            ShipInteriorMod.Log("Patting down terrain...");
            for (int x = (int)this.lowCorner.x - 1; x <= this.highCorner.x + 1; ++x)
            {
                for (int z = (int)this.lowCorner.z - 1; z <= this.highCorner.z + 1; ++z)
                {
                    position = new IntVec3(x, 0, z);
                    map.terrainGrid.SetTerrain(position, TerrainDefOf.Gravel);
                }
            }
            ShipInteriorMod.Log("Clearing wildlife...");
            IEnumerable<Pawn> doomedPawns = map.mapPawns.AllPawns.Where((Pawn x) => 
                x.Position.x >= lowCorner.x &&
                x.Position.x <= highCorner.x &&
                x.Position.z >= lowCorner.z &&
                x.Position.z <= highCorner.z);

            foreach(Pawn pawn in doomedPawns)
            {
                pawn.Destroy();
            }

            ShipInteriorMod.Log("Spawning ship...");
            using (List<Thing>.Enumerator enumerator = thingList1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    ((Entity)enumerator.Current).SpawnSetup(map, false);
                }
            }
            ShipInteriorMod.Log("Done.");
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
            // ISSUE: cast to a reference type
            Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);
            ((ScribeLoader)Scribe.loader).FinalizeLoading();
            for (int x = (int)this.lowCorner.x - 2; x <= this.highCorner.x + 2; ++x)
            {
                for (int z = (int)this.lowCorner.z - 2; z <= this.highCorner.z + 2; ++z)
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
