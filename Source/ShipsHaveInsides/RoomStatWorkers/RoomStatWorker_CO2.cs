using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorld
{
    public class RoomStatWorker_CO2 : RoomStatWorker
    {
        public float GetMapBase(Map map)
        {
            if(map.IsSpace())
            {
                return GasMixture.Vacuum.Co2Partial;
            }
            return GasMixture.EarthNorm.Co2Partial;
        }
        public override float GetScore(Room room)
        {
            if(DefDatabase<RoomRoleDef>.GetNamed("ShipInside").Worker.GetScore(room) > 0.0f)
            {
                return room.Map.GetSpaceAtmosphereMapComponent().DefinitionAt(room.Cells.First()).GetGas(room).mixture.Co2Partial;
            }

            return GetMapBase(room.Map);
        }
    }
}
