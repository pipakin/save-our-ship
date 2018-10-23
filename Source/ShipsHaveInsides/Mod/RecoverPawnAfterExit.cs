using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(Building_CryptosleepCasket), "EjectContents", null)]
    public static class RecoverPawnAfterExit
    {
        static List<Pawn> pawnsToRecover = new List<Pawn>();

        private static ShipInteriorMod Mod
        {
            get
            {
                return ShipInteriorMod.instance;
            }
        }

        [HarmonyPrefix]
        public static bool Prefix(Building_CryptosleepCasket __instance)
        {
            pawnsToRecover.Add(__instance.ContainedThing as Pawn);
            return true;
        }

        [HarmonyPostfix]
        public static void PostFix()
        {
            List<Pawn> newList = pawnsToRecover;
            pawnsToRecover = new List<Pawn>();
            foreach(Pawn pawn in newList)
            {
                //Note: Just for nimble, we'll feature flag this fix. :)
                if(pawn.Faction == Faction.OfPlayer || Mod.leaveCryptosleepBug.Value)
                {
                    Map m = pawn.Map;
                    IntVec3 loc = pawn.Position;
                    if (pawn.workSettings != null)
                        pawn.workSettings.EnableAndInitialize();
                    Find.StoryWatcher.watcherPopAdaptation.Notify_PawnEvent(pawn, PopAdaptationEvent.GainedColonist);
                    pawn.workSettings.Notify_UseWorkPrioritiesChanged();
                    pawn.mindState.Reset();
                    MoteMaker.ThrowAirPuffUp(pawn.DrawPos, m);
                }
            }
        }
    }
}
