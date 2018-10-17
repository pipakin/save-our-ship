using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(ShipCountdown), "CountdownEnded", null)]
    public static class SaveShip
    {
        [HarmonyPrefix]
        public static bool SaveShipAndRemoveItemStacks()
        {
            if (ShipInteriorMod.saveShip)
            {
                string str1 = Path.Combine(GenFilePaths.SaveDataFolderPath, "Ships");
                DirectoryInfo directoryInfo = new DirectoryInfo(str1);
                if (!directoryInfo.Exists)
                    directoryInfo.Create();
                Faction playerFaction = (((FactionManager)Current.Game.World.factionManager).AllFactions as IList<Faction>)[GenCollection.FirstIndexOf<Faction>(((FactionManager)Current.Game.World.factionManager).AllFactions, (theFac => theFac.IsPlayer))];
                string name = playerFaction.Name;
                string str2 = Path.Combine(str1, name + ".rwship");
                List<Thing> toSave = new List<Thing>();
                List<Thing> thingList = new List<Thing>();
                List<Building> buildingList = ShipUtility.ShipBuildingsAttachedTo(ShipInteriorMod.shipRoot);
                using (List<Building>.Enumerator enumerator = buildingList.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Thing current = (Thing)enumerator.Current;
                        toSave.Add(current);
                    }
                }
                using (List<Building>.Enumerator enumerator1 = buildingList.GetEnumerator())
                {
                    while (enumerator1.MoveNext())
                    {
                        Building current1 = enumerator1.Current;
                        using (List<Thing>.Enumerator enumerator2 = GridsUtility.GetThingList(((Thing)current1).Position, ((Thing)current1).Map).GetEnumerator())
                        {
                            while (enumerator2.MoveNext())
                            {
                                Thing current2 = enumerator2.Current;
                                ((RoofGrid)current2.Map.roofGrid).SetRoof(current2.Position, (RoofDef)null);
                                if (((ZoneManager)current2.Map.zoneManager).ZoneAt(current2.Position) != null)
                                    ((ZoneManager)current2.Map.zoneManager).ZoneAt(current2.Position).Delete();
                                if (!(current2 is Building))
                                {
                                    thingList.Add(current2);
                                    if (!toSave.Contains(current2))
                                        toSave.Add(current2);
                                }
                            }
                        }
                    }
                }
                SafeSaver.Save(str2, "RWShip", (Action)(() =>
                {
                    ScribeMetaHeaderUtility.WriteMetaHeader();
                    // ISSUE: cast to a reference type
                    Scribe_Values.Look<FactionDef>(ref playerFaction.def, "playerFactionDef", null, false);
                    Scribe.EnterNode("things");
                    for (int index = 0; index < toSave.Count; ++index)
                    {
                        Thing thing = toSave[index];
                        // ISSUE: cast to a reference type
                        Scribe_Deep.Look<Thing>(ref thing, false, "thing", new object[0]);
                    }
                    Scribe.ExitNode();
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<ResearchManager>(ref Current.Game.researchManager, false, "researchManager", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<TaleManager>(ref Current.Game.taleManager, false, "taleManager", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<UniqueIDsManager>(ref Current.Game.uniqueIDsManager, false, "uniqueIDsManager", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<TickManager>(ref Current.Game.tickManager, false, "tickManager", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<DrugPolicyDatabase>(ref Current.Game.drugPolicyDatabase, false, "drugPolicyDatabase", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<OutfitDatabase>(ref Current.Game.outfitDatabase, false, "outfitDatabase", new object[0]);
                    // ISSUE: cast to a reference type
                    Scribe_Deep.Look<PlayLog>(ref Current.Game.playLog, false, "playLog", new object[0]);
                }));
                using (List<Thing>.Enumerator enumerator = thingList.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        Thing current = enumerator.Current;
                        if (!current.Destroyed)
                            current.Destroy((DestroyMode)0);
                    }
                }
            }
            return true;
        }
    }
}
