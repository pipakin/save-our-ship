using Harmony;
using RimWorld;
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
                else if (newDef.defName == "ShipInside_PassiveCooler" && oldDef.defName == "Ship_Beam_Modular")
                {
                    __result = true;
                }
            }
        }
    }
}
