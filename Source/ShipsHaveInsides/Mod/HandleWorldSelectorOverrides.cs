using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ShipsHaveInsides.Mod
{
    [HarmonyPatch(typeof(WorldSelector), "HandleWorldClicks", null)]
    public static class HandleWorldSelectorOverrides
    {
        [HarmonyPrefix]
        public static bool HandleClicks(WorldSelector __instance)
        {
            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 1 && __instance.SelectedObjects.Count > 0)
                {
                    bool found = false;
                    foreach(var ship in __instance.SelectedObjects.OfType<AirShipWorldObject>())
                    {
                        found = true;
                        ship.ClickedNewTile(GenWorld.MouseTile());
                    }
                    if (found)
                    {
                        Event.current.Use();
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
