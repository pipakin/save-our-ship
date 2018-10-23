using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class ShipNavigationComponent : ThingComp
    {
        private Building parentBuilding
        {
            get { return parent as Building; }
        }

        private bool CanLaunchNow => !ShipUtility.LaunchFailReasons(parentBuilding).Any();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_Action launch = new Command_Action
            {
                action = TryLaunch,
                defaultLabel = "CommandShipLaunch".Translate(),
                defaultDesc = "CommandShipLaunchDesc".Translate()
            };
            if (!CanLaunchNow)
            {
                launch.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                launch.Disable();
            }
            launch.hotKey = KeyBindingDefOf.Misc1;
            launch.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            Command_Action orbit = new Command_Action
            {
                action = TryOrbit,
                defaultLabel = "ShipInsideOrbit".Translate(),
                defaultDesc = "ShipInsideOrbitDesc".Translate()
            };
            if (!CanLaunchNow)
            {
                orbit.Disable(ShipUtility.LaunchFailReasons(parentBuilding).First());
            }
            if (ShipCountdown.CountingDown)
            {
                orbit.Disable();
            }
            orbit.hotKey = KeyBindingDefOf.Misc1;
            orbit.icon = ContentFinder<Texture2D>.Get("UI/Commands/LaunchShip");

            // Soon... so soon...
            return base.CompGetGizmosExtra().Concat(launch); //.Concat(orbit);
        }

        private void TryOrbit()
        {
            //create world map pawn for ship
            //generate orbit map
            //move ship there
            //do cool graphic?
        }

        private void TryLaunch()
        {
            if (CanLaunchNow)
            {
                ShipCountdown.InitiateCountdown(parentBuilding);
            }
        }
    }
}
