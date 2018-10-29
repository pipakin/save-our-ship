using ShipsHaveInsides.MapComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public static class ShipInside_MapExtensions
    {
        public static SpaceAtmosphereMapComponent GetSpaceAtmosphereMapComponent(this Map map)
        {
            return map.GetComponent<SpaceAtmosphereMapComponent>();
        }

        public static bool IsSpace(this Map map)
        {
            return map.terrainGrid.TerrainAt(IntVec3.Zero).defName == "HardVacuum";
        }
    }
}
