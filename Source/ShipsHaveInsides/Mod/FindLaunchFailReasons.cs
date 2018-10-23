using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(ShipUtility), "LaunchFailReasons", null)]
    public static class FindLaunchFailReasons
    {
        [HarmonyPrefix]
        public static bool DisableOriginalMethod()
        {
            return false;
        }

        [HarmonyPostfix]
        public static void FindLaunchFailReasonsReally(Building rootBuilding, ref List<string> __result)
        {
            __result = new List<string>();
            List<Building> shipParts = ShipUtility.ShipBuildingsAttachedTo(rootBuilding);

            if(shipParts.Count == 0)
            {
                __result.Add("Checking ship. Please wait...");
                return;
            }

            if (!FindLaunchFailReasons.FindEitherThing(shipParts, (ThingDef)ThingDefOf.Ship_CryptosleepCasket, ThingDef.Named("ShipInside_CryptosleepCasket")))
                __result.Add(Translator.Translate("ShipReportMissingPart") + ": " + (string)((Def)ThingDefOf.Ship_CryptosleepCasket).label);
            if (!FindLaunchFailReasons.FindTheThing(shipParts, (ThingDef)ThingDefOf.Ship_ComputerCore))
                __result.Add(Translator.Translate("ShipReportMissingPart") + ": " + (string)((Def)ThingDefOf.Ship_ComputerCore).label);
            if (!FindLaunchFailReasons.FindTheThing(shipParts, (ThingDef)ThingDefOf.Ship_Reactor))
                __result.Add(Translator.Translate("ShipReportMissingPart") + ": " + (string)((Def)ThingDefOf.Ship_Reactor).label);
            if (!FindLaunchFailReasons.FindTheThing(shipParts, (ThingDef)ThingDefOf.Ship_Engine))
                __result.Add(Translator.Translate("ShipReportMissingPart") + ": " + (string)((Def)ThingDefOf.Ship_Engine).label);
            bool flag = false;
            using (List<Building>.Enumerator enumerator = shipParts.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Building current = enumerator.Current;
                    if (((Thing)current).def == ThingDefOf.Ship_CryptosleepCasket || ((Thing)current).def == ThingDef.Named("ShipInside_CryptosleepCasket"))
                    {
                        Building_CryptosleepCasket cryptosleepCasket = current as Building_CryptosleepCasket;
                        if (cryptosleepCasket != null && ((Building_Casket)cryptosleepCasket).HasAnyContents)
                        {
                            flag = true;
                            break;
                        }
                    }
                }
            }
            if (flag)
                return;
            __result.Add(Translator.Translate("ShipReportNoFullPods"));
        }

        private static bool FindTheThing(List<Building> shipParts, ThingDef theDef)
        {
            return GenCollection.Any<Building>(shipParts, (pa => ((Thing)pa).def == theDef));
        }

        private static bool FindEitherThing(List<Building> shipParts, ThingDef theDef, ThingDef theOtherDef)
        {
            return GenCollection.Any<Building>(shipParts, (pa => ((Thing)pa).def == theDef)) || GenCollection.Any<Building>(shipParts, (pa => ((Thing)pa).def == theOtherDef));
        }
    }
}
