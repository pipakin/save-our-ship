using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Verse;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(WeatherDecider), "StartInitialWeather", null)]
    public static class WeatherDecider_StartInitialWeather
    {
        public static FieldInfo mapField = typeof(WeatherDecider).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<WeatherDecider, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(WeatherDecider __instance)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum" || ShipInteriorMod.noSpaceWeather)
            {
                //No space weather
                getMap(__instance).weatherManager.lastWeather = WeatherDef.Named("NoneSpace");
                getMap(__instance).weatherManager.curWeather = WeatherDef.Named("NoneSpace");
            }
        }
    }

    [HarmonyPatch(typeof(WeatherDecider), "StartNextWeather", null)]
    public static class WeatherDecider_StartNextWeather
    {
        public static FieldInfo mapField = typeof(WeatherDecider).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<WeatherDecider, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(WeatherDecider __instance)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //No space weather
                getMap(__instance).weatherManager.lastWeather = WeatherDef.Named("NoneSpace");
                getMap(__instance).weatherManager.curWeather = WeatherDef.Named("NoneSpace");
            }
        }
    }

    [HarmonyPatch(typeof(MapTemperature), "get_OutdoorTemp", null)]
    public static class MapTemperature_OutdoorTemp
    {
        public static FieldInfo mapField = typeof(MapTemperature).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<MapTemperature, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(MapTemperature __instance, ref float __result)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //really cold. This is not accurate, just for gameplay.
                __result = -100f;
            }
        }
    }

    [HarmonyPatch(typeof(MapTemperature), "get_SeasonalTemp", null)]
    public static class MapTemperature_SeasonalTemp
    {
        public static FieldInfo mapField = typeof(MapTemperature).GetField("map", BindingFlags.Instance | BindingFlags.NonPublic);
        public static Func<MapTemperature, Map> getMap = wd => mapField.GetValue(wd) as Map;

        [HarmonyPostfix]
        public static void ChooseSpaceWeather(MapTemperature __instance, ref float __result)
        {
            if (getMap(__instance).terrainGrid.TerrainAt(IntVec3.Zero)?.defName == "HardVacuum")
            {
                //really cold. This is not accurate, just for gameplay.
                __result = -100f;
            }
        }
    }
}
