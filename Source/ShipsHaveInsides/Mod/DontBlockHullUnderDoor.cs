using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(GenConstruct), "BlocksConstruction", null)]
    class DontBlockHullUnderDoor
    {
        [HarmonyPostfix]
        public static void BlocksConstruction(Thing constructible, Thing t, ref bool __result)
        {
            if (__result)
            {

                ThingDef thingDef = !(constructible is Blueprint) ? (!(constructible is Frame) ? constructible.def.blueprintDef : constructible.def.entityDefToBuild.blueprintDef) : constructible.def;
                ThingDef entityDefToBuild = thingDef.entityDefToBuild as ThingDef;
                if (entityDefToBuild != null && entityDefToBuild.defName == "ShipHullTile" && t.def.defName == "ShipAirlock")
                {
                    __result = false;
                }
            }
        }
    }
}
