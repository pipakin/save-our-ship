using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(MapDrawer), "DrawMapMesh", null)]
    [StaticConstructorOnStartup]
    public class RenderPlanetBehindMap
    {
        static RenderTexture target = new RenderTexture(textureSize, textureSize, 16);
        static Texture2D virtualPhoto = new Texture2D(textureSize, textureSize, TextureFormat.RGB24, false);
        public static Material PlanetMaterial = MaterialPool.MatFrom(virtualPhoto);

        const int textureSize = 2048;
        const float altitude = 1100f;

        [HarmonyPrefix]
        public static void PreDraw()
        {
            TerrainDef def = TerrainDef.Named("HardVacuum");
            Map map = Find.CurrentMap;

            // if we aren't in space, abort!
            if(map.terrainGrid.TerrainAt(IntVec3.Zero).defName != def.defName)
            {
                return;
            }

            RenderTexture oldTexture = Find.WorldCamera.targetTexture;
            RenderTexture oldSkyboxTexture = RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture;

            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.Planet;
            Find.WorldCameraDriver.JumpTo(Find.CurrentMap.Tile);
            Find.WorldCameraDriver.altitude = altitude;
            Find.WorldCameraDriver.GetType()
                .GetField("desiredAltitude", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(Find.WorldCameraDriver, altitude);

            float num = (float)UI.screenWidth / (float)UI.screenHeight;

            Find.WorldCameraDriver.Update();
            Find.World.WorldUpdate();
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = target;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.aspect = num;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.Render();

            Find.WorldCamera.targetTexture = target;
            Find.WorldCamera.aspect = num;
            Find.WorldCamera.Render();


            RenderTexture.active = target;
            virtualPhoto.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
            virtualPhoto.Apply();
            RenderTexture.active = null;


            /*
            Matrix4x4 matrix = new Matrix4x4();
            Vector3 center = Find.CameraDriver.GetComponent<Camera>().transform.position;
            float cellsHigh = UI.screenHeight / Find.CameraDriver.CellSizePixels;

            foreach(IntVec3 cell in Find.CameraDriver.CurrentViewRect.Cells)
            {
                matrix.SetTRS(new Vector3(center.x, 0.0f, center.z), Quaternion.identity, new Vector3(cellsHigh * num, 1f, cellsHigh));
                Graphics.DrawMesh(MeshPool.plane10, matrix, MaterialPool.MatFrom(), layer: 0, camera: null, submeshIndex: 0, properties: null, castShadows: true, receiveShadows: false, useLightProbes: false);
            }
            */

            Find.WorldCamera.targetTexture = oldTexture;
            RimWorld.Planet.WorldCameraManager.WorldSkyboxCamera.targetTexture = oldSkyboxTexture;
            Find.World.renderer.wantedMode = RimWorld.Planet.WorldRenderMode.None;
            Find.World.WorldUpdate();

        }
    }
}
