using RimWorld;
using ShipsHaveInsides.Utilities;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
    public class CompRoofMe : ThingComp
    {
        private IntVec3? position;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            parent.Map.roofGrid.SetRoof(parent.Position, DefDatabase<RoofDef>.GetNamed("RoofShip", true));
            parent.Map.terrainGrid.SetTerrain(parent.Position, DefDatabase<TerrainDef>.GetNamed("GravityPlating", true));
            List<Thing> thingList1 = ((ThingGrid)((Thing)this.parent).Map.thingGrid).ThingsListAt(((Thing)this.parent).Position);
            new ThingMutator<Thing>()
                .DeSpawn<Plant>()
                .Destroy<Building_SteamGeyser>()
                .UnsafeExecute(thingList1);

            position = parent.Position;
        }

        public override void PostDeSpawn(Map map)
        {
            base.PostDeSpawn(map);
            if (position == null)
                return;

            List<Thing> thingList1 = (map.thingGrid).ThingsListAt(position.Value);
            if (thingList1.Any(t => t.TryGetComp<CompRoofMe>() != null)) {
                position = null;
                return;
            }
            
            map.roofGrid.SetRoof(position.Value, null);
            map.terrainGrid.SetTerrain(position.Value, TerrainDefOf.Gravel);
            position = null;
        }
    }
}
