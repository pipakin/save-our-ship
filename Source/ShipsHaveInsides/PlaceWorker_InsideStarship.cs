using Verse;

namespace RimWorld
{
    public class PlaceWorker_InsideStarship : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            if (GridsUtility.GetRoom(loc, map, (RegionType)7) == null)
                map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms();
            if (GridsUtility.GetRoom(loc, map, (RegionType)7) != null && GridsUtility.GetRoom(loc, map, RegionType.Set_All).Role.defName.Equals("ShipInside"))
                return true;
            if (GridsUtility.GetDoor(loc, map) != null && GridsUtility.GetDoor(loc, map).def.defName.Equals("ShipAirlock"))
                return true;
            return new AcceptanceReport(Translator.Translate("MustPlaceInsideShip"));
        }

        public PlaceWorker_InsideStarship()
        {
        }
    }
}
