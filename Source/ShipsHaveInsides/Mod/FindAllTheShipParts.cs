using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using Verse;

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

        [HarmonyPostfix]
        public static void FindShipPartsReally(Building root, ref List<Building> __result)
        {
            if (root == null || ((Thing)root).Destroyed)
            {
                __result = new List<Building>();
            }
            else
            {
                ShipInteriorMod.closedSet.Clear();
                ShipInteriorMod.openSet.Clear();
                ShipInteriorMod.openSet.Add(root);
                while (ShipInteriorMod.openSet.Count > 0)
                {
                    Building open = ShipInteriorMod.openSet[ShipInteriorMod.openSet.Count - 1];
                    ShipInteriorMod.openSet.Remove(open);
                    ShipInteriorMod.closedSet.Add(open);
                    using (IEnumerator<IntVec3> enumerator1 = GenAdj.CellsAdjacentCardinal((Thing)open).GetEnumerator())
                    {
                        while (((IEnumerator)enumerator1).MoveNext())
                        {
                            IntVec3 current1 = enumerator1.Current;
                            Building edifice = GridsUtility.GetEdifice(current1, ((Thing)open).Map);
                            if (edifice != null && ((BuildingProperties)((ThingDef)((Thing)edifice).def).building).shipPart && !ShipInteriorMod.closedSet.Contains(edifice) && !ShipInteriorMod.openSet.Contains(edifice))
                            {
                                ShipInteriorMod.openSet.Add(edifice);
                            }
                            else
                            {
                                bool flag = false;
                                using (List<Thing>.Enumerator enumerator2 = GridsUtility.GetThingList(current1, ((Thing)open).Map).GetEnumerator())
                                {
                                    while (enumerator2.MoveNext())
                                    {
                                        Thing current2 = enumerator2.Current;
                                        if (current2 is Building)
                                        {
                                            Building building = current2 as Building;
                                            if (((BuildingProperties)((ThingDef)((Thing)building).def).building).shipPart && !ShipInteriorMod.closedSet.Contains(building) && !ShipInteriorMod.openSet.Contains(building))
                                            {
                                                ShipInteriorMod.openSet.Add(building);
                                                flag = true;
                                            }
                                        }
                                    }
                                }
                                if (flag)
                                {
                                    using (List<Thing>.Enumerator enumerator2 = GridsUtility.GetThingList(current1, ((Thing)open).Map).GetEnumerator())
                                    {
                                        while (enumerator2.MoveNext())
                                        {
                                            Thing current2 = enumerator2.Current;
                                            if (current2 is Building)
                                            {
                                                Building building = current2 as Building;
                                                if (!ShipInteriorMod.closedSet.Contains(building) && !ShipInteriorMod.openSet.Contains(building))
                                                    ShipInteriorMod.closedSet.Add(building);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                __result = ShipInteriorMod.closedSet;
            }
        }
    }
}
