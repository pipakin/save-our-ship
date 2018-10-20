using Verse;

namespace RimWorld
{
    public class PlaceWorker_InsideShipRoom : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null)
        {
            if (GridsUtility.GetRoom(loc, map, (RegionType)7) == null)
                ((RegionAndRoomUpdater)map.regionAndRoomUpdater).TryRebuildDirtyRegionsAndRooms();
            if (GridsUtility.GetDoor(loc, map) != null && GridsUtility.GetDoor(loc, map).def.defName.Equals("ShipAirlock"))
                return true;
            if (GridsUtility.GetRoom(loc, map, (RegionType)7) == null || !((string)((Def)GridsUtility.GetRoom(loc, map, (RegionType)7).Role).defName).Equals("ShipFramework"))
                return new AcceptanceReport(Translator.Translate("MustPlaceInsideShipFramework"));
            return true;
        }
    }
}
