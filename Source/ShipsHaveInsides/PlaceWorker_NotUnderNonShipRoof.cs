using Verse;

namespace RimWorld
{
    public class PlaceWorker_NotUnderNonShipRoof : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            if (map.roofGrid.Roofed(loc) && map.roofGrid.RoofAt(loc).defName != "RoofShip")
                return new AcceptanceReport("MustPlaceUnroofed".Translate());
            return (AcceptanceReport)true;
        }
    }
}