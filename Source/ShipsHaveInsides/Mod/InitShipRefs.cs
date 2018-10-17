using Harmony;
using RimWorld;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(ShipCountdown), "InitiateCountdown", null)]
    public static class InitShipRefs
    {
        [HarmonyPrefix]
        public static bool SaveStatics(Building launchingShipRoot)
        {
            ShipInteriorMod.shipRoot = launchingShipRoot;
            ShipInteriorMod.saveShip = true;
            return true;
        }
    }
}
