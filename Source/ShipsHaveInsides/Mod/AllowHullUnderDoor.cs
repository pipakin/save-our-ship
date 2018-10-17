using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(GenConstruct), "CanPlaceBlueprintOver", null)]
    public static class AllowHullUnderDoor
    {
        [HarmonyPostfix]
        public static void CanPlaceBlueprintOver(BuildableDef newDef, ThingDef oldDef, ref bool __result)
        {
            if(!__result)
            {
                if(newDef.defName == "ShipHullTile" && oldDef.defName == "ShipAirlock")
                {
                    __result = true;
                }
            }
        }
    }
}
