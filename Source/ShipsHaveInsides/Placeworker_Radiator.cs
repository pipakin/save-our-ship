using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class PlaceWorker_Radiator : PlaceWorker
    {
        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol)
        {
            Map currentMap = Find.CurrentMap;
            IntVec3 loc1 = center + IntVec3.North.RotatedBy(rot); 
            IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
            IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
            IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
            GenDraw.DrawFieldEdges(new List<IntVec3>()
            {
            loc1
            }, GenTemperature.ColorSpotCold);
            GenDraw.DrawFieldEdges(new List<IntVec3>()
            {
            loc2,loc3,loc4
            }, GenTemperature.ColorSpotHot);
            RoomGroup roomGroup1 = loc2.GetRoomGroup(currentMap);
            RoomGroup roomGroup2 = loc1.GetRoomGroup(currentMap);
            if (roomGroup1 == null || roomGroup2 == null)
                return;
            if (roomGroup1 == roomGroup2 && !roomGroup1.UsesOutdoorTemperature)
            {
                GenDraw.DrawFieldEdges(roomGroup1.Cells.ToList<IntVec3>(), new Color(1f, 0.7f, 0.0f, 0.5f));
            }
            else
            {
                if (!roomGroup1.UsesOutdoorTemperature)
                    GenDraw.DrawFieldEdges(roomGroup1.Cells.ToList<IntVec3>(), GenTemperature.ColorRoomHot);
                if (roomGroup2.UsesOutdoorTemperature)
                    return;
                GenDraw.DrawFieldEdges(roomGroup2.Cells.ToList<IntVec3>(), GenTemperature.ColorRoomCold);
            }
        }

        public override AcceptanceReport AllowsPlacing(BuildableDef def, IntVec3 center, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            IntVec3 loc1 = center + IntVec3.North.RotatedBy(rot);
            IntVec3 loc2 = center + IntVec3.South.RotatedBy(rot);
            IntVec3 loc3 = center + (IntVec3.South.RotatedBy(rot) * 2);
            IntVec3 loc4 = center + (IntVec3.South.RotatedBy(rot) * 3);
            if (loc1.Impassable(map) || loc2.Impassable(map) || loc3.Impassable(map) || loc4.Impassable(map))
                return (AcceptanceReport)"MustPlaceCoolerWithFreeSpaces".Translate();
            return (AcceptanceReport)true;
        }
    }
}
