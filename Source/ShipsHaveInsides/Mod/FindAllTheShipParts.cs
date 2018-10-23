using Harmony;
using RimWorld;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Verse;
using System.Threading;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(ShipUtility), "ShipBuildingsAttachedTo", null)]
    public static class FindAllTheShipParts
    {
        
        [HarmonyPrefix]
        public static bool DisableOriginalMethod()
        {
            return false;
        }

        private static Mutex mutex = new Mutex();
        private static string ThingGeneratedFor;
        private static HashSet<Building> set;
        private static string ThingGettingFor;


        [HarmonyPostfix]
        public static void FindShipPartsReally(Building root, ref List<Building> __result)
        {
            if (root == null || ((Thing)root).Destroyed)
            {
                __result = new List<Building>();
            }
            else
            {
                try
                {
                    mutex.WaitOne();
                    if (ThingGeneratedFor == root.ThingID)
                    {

                        __result = set.ToList();
                    }
                    else if (ThingGeneratedFor != null)
                    {
                        ThingGeneratedFor = null;
                        __result = new List<Building>();
                    }
                    else
                    {
                        __result = new List<Building>();
                    }
                    if (ThingGettingFor == root.ThingID)
                    {
                        return;
                    }
                    ThingGettingFor = root.ThingID;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }

                ThreadPool.QueueUserWorkItem(s =>
                {
                    Building id = s as Building;

                    HashSet<Building> closedSet = new HashSet<Building>();
                    HashSet<Building> openSet = new HashSet<Building>();
                    openSet.Add(id);

                    while (openSet.Count > 0)
                    {
                        Building open = openSet.First();
                        openSet.Remove(open);
                        closedSet.Add(open);

                        foreach (var current1 in GenAdj.CellsAdjacentCardinal(open))
                        {
                            List<Building> buildings = GridsUtility.GetThingList(current1, open.Map)
                                .OfType<Building>()
                                .ToList();

                            if (buildings.Any(b => b.def.building.shipPart) && !closedSet.Contains(buildings.First(b => b.def.building.shipPart)))
                            {
                                openSet.Add(buildings.First(b => b.def.building.shipPart));
                                buildings.Remove(buildings.First(b => b.def.building.shipPart));
                            }
                            closedSet.AddRange(buildings);
                        }
                        openSet.ExceptWith(closedSet);
                    }

                    try
                    {
                        mutex.WaitOne();
                        if (ThingGettingFor == id.ThingID)
                        {
                            set = closedSet;
                            ThingGeneratedFor = id.ThingID;
                            ThingGettingFor = null;
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
            }, root);
            }
        }
    }
}
