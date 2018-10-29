using Harmony;
using ShipsHaveInsides.MapComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{ 
    [HarmonyPatch(typeof(ThingGrid), "Register", null)]
    public static class ThingGrid_Register
    {
        [HarmonyPostfix]
        public static void Register(ThingGrid __instance, Thing t)
        {
            Traverse.Create(__instance).Field<Map>("map").Value.GetComponent<SpaceAtmosphereMapComponent>().Register(t);
        }
    }
    [HarmonyPatch(typeof(ThingGrid), "Deregister", null)]
    public static class ThingGrid_Deregister
    {
        [HarmonyPrefix]
        public static void Deregister(ThingGrid __instance, Thing t)
        {
            Traverse.Create(__instance).Field<Map>("map").Value.GetComponent<SpaceAtmosphereMapComponent>().Deregister(t);
        }
    }
}
