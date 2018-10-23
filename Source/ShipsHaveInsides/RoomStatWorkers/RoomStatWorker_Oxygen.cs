using System.Collections.Generic;
using Verse;

namespace RimWorld
{
    public class RoomStatWorker_Oxygen : RoomStatWorker
    {
        public float GetMapBase(Map map)
        {
            if(map.terrainGrid.TerrainAt(0).defName == "HardVacuum")
            {
                return 0.0f;
            }
            return 19.5f;
        }
        public override float GetScore(Room room)
        {
            if(room.Group.AnyRoomTouchesMapEdge)
            {
                return GetMapBase(room.Map);
            }
            if(room.OpenRoofCountStopAt(1) > 0)
            {
                return GetMapBase(room.Map);
            }
            if(DefDatabase<RoomRoleDef>.GetNamed("ShipInside").Worker.GetScore(room) > 0.0f)
            {
                //check for open doors, then spread from those rooms.
                return 19.5f;
            }

            return GetMapBase(room.Map);
        }
    }
}
