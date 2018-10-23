using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(SectionLayer), "DrawLayer", null)]
    [StaticConstructorOnStartup]
    public class HideLightingLayersInSpace
    {
        private static float oldSkyGlow;
        //private static Color oldSkyColor;
        [HarmonyPrefix]
        public static bool ShouldDraw(SectionLayer __instance)
        {
            TerrainDef def = TerrainDef.Named("HardVacuum");
            Map map = Find.CurrentMap;

            // if we aren't in space, abort!
            if (map.terrainGrid.TerrainAt(IntVec3.Zero).defName != def.defName)
            {
                return true;
            }

            if (__instance.GetType().Name == "SectionLayer_SunShadows")
                return false;

            if(__instance.GetType().Name == "SectionLayer_LightingOverlay")
            {
                oldSkyGlow = map.skyManager.CurSkyGlow;
                MatBases.LightOverlay.color = new Color(1.0f, 1.0f, 1.0f);
                map.skyManager.ForceSetCurSkyGlow(1.0f);
            }

            if (__instance.GetType().Name == "SectionLayer_Terrain")
            {
                float num = (float)UI.screenWidth / (float)UI.screenHeight;
                Vector3 center = Find.CameraDriver.GetComponent<Camera>().transform.position;
                float cellsHigh = UI.screenHeight / Find.CameraDriver.CellSizePixels;
                float cellsWide = cellsHigh * num;

                //recalculate uvs for planet texture.
                LayerSubMesh mesh = __instance.GetSubMesh(RenderPlanetBehindMap.PlanetMaterial);
                if(mesh != null)
                {
                    mesh.Clear(MeshParts.UVs);
                    for(int i=0;i<mesh.verts.Count;i++)
                    {
                        float xdiff = mesh.verts[i].x - center.x;
                        float xfromEdge = xdiff + cellsWide / 2f;
                        float zdiff = mesh.verts[i].z - center.z;
                        float zfromEdge = zdiff + cellsHigh / 2f;

                       mesh.uvs.Add(new Vector3(xfromEdge / cellsWide, zfromEdge / cellsHigh, 0.0f));
                    }
                    mesh.FinalizeMesh(MeshParts.UVs);
                }
            }

            return true;
        }

        [HarmonyPostfix]
        public static void Cleanup(SectionLayer __instance)
        {
            TerrainDef def = TerrainDef.Named("HardVacuum");
            Map map = Find.CurrentMap;

            // if we aren't in space, abort!
            if (map.terrainGrid.TerrainAt(IntVec3.Zero).defName != def.defName)
            {
                return;
            }

            if (__instance.GetType().Name == "SectionLayer_LightingOverlay")
            {
                map.skyManager.ForceSetCurSkyGlow(oldSkyGlow);
            }
        }
    }
}
