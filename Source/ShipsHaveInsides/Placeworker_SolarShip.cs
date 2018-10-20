using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class PlaceWorker_SolarShip : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
            IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
            IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
            GenDraw.DrawFieldEdges(new List<IntVec3>()
            {
            loc2,loc3,loc4
            }, GenTemperature.ColorSpotHot);

        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
            IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
            IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
            if (loc2.Impassable(map) || loc3.Impassable(map) || loc4.Impassable(map))
                return (AcceptanceReport)"MustPlaceSolarShipWithFreeSpaces".Translate();
            return (AcceptanceReport)true;
        }
    }
}
