using Harmony;
using UnityEngine;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(SectionLayer), "ClearSubMeshes", null)]
    public static class GenerateSpaceSubMesh
    { 
        
        [HarmonyPostfix]
        public static void GenerateMesh(SectionLayer __instance)
        {
            if (__instance.GetType().Name != "SectionLayer_Terrain")
                return;

            Section section = __instance.GetType().GetField("section", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(__instance) as Section;
            foreach (IntVec3 cell in section.CellRect.Cells)
            {
                TerrainDef terrain1 = section.map.terrainGrid.TerrainAt(cell);
                if (terrain1.defName == "HardVacuum")
                {
                    Printer_Mesh.PrintMesh(__instance, cell.ToVector3() + new Vector3(0.5f, 0f, 0.5f), MeshMakerPlanes.NewPlaneMesh(1f), RenderPlanetBehindMap.PlanetMaterial);
                }
            }
        }
    }
}
