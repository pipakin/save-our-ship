using ShipsHaveInsides.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class GenStep_MakeSpace : GenStep
    {
        public override int SeedPart => 826504671;

        public override void Generate(Map map, GenStepParams parms)
        {
            IntVec3 size = map.Size;
            MapGenFloatGrid elevation = MapGenerator.Elevation;
            foreach (IntVec3 allCell in map.AllCells)
            {
                elevation[allCell] = 0.0f;
            }
            MapGenFloatGrid fertility = MapGenerator.Fertility;
            foreach (IntVec3 allCell2 in map.AllCells)
            {
                fertility[allCell2] = 0.0f;
            }

            var terrainDef = TerrainDef.Named("HardVacuum");

            new MapScanner()
                .ForPoints((x, target) => target.terrainGrid.SetTerrain(x, terrainDef))
                .UnsafeExecute(map, IntVec3.Zero, map.Size);
        }
    }
}
