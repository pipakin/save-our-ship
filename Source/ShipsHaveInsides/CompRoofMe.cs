using RimWorld;
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
            ((RoofGrid)((Thing)this.parent).Map.roofGrid).SetRoof(((Thing)this.parent).Position, DefDatabase<RoofDef>.GetNamed("RoofShip", true));
            List<Thing> thingList1 = ((ThingGrid)((Thing)this.parent).Map.thingGrid).ThingsListAt(((Thing)this.parent).Position);
            List<Thing> thingList2 = new List<Thing>();
            using (List<Thing>.Enumerator enumerator = thingList1.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    Thing current = enumerator.Current;
                    if (current is Plant)
                        thingList2.Add(current);
                }
            }
            for (int index = 0; index < thingList2.Count; ++index)
                ((Entity)thingList2[index]).DeSpawn();

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
            position = null;
        }
    }
}
