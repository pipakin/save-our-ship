using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(WorldObjectsHolder), "MapParentAt", null)]
    public static class WorldObjectsHolder_MapParentAt
    {
        [HarmonyPostfix]
        public static void AdjustReturnValue(WorldObjectsHolder __instance, int tile, MapParent __result)
        {
            //if there wasn't one, or it already wasn't the airship, get out.
            if (__result == null || !(__result is AirShipWorldObject)) return;

            //rewrite result
            __result = __instance.MapParents.FirstOrDefault(x => x.Tile == tile && !(x is AirShipWorldObject));
        }
    }

    [HarmonyPatch(typeof(Game), "FindMap", new Type[] { typeof(int) })]
    public static class Game_FindMap
    {
        [HarmonyPostfix]
        public static void AdjustReturnValue(Game __instance, int tile, Map __result)
        {
            //if there wasn't one, or it already wasn't the airship, get out.
            if (__result == null || !(__result.Parent is AirShipWorldObject)) return;

            //rewrite result
            __result = __instance.Maps.FirstOrDefault(x => x.Tile == tile && !(x.Parent is AirShipWorldObject));
        }
    }
}
