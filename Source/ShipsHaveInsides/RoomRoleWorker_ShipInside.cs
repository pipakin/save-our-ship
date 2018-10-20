using System.Collections;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
  public class RoomRoleWorker_ShipInside : RoomRoleWorker
  {
    public override float GetScore(Room room)
    {
      if (room.OpenRoofCount > 0)
        return 0.0f;
      using (IEnumerator<IntVec3> enumerator1 = room.Cells.GetEnumerator())
      {
        while (((IEnumerator) enumerator1).MoveNext())
        {
          IntVec3 current1 = enumerator1.Current;
          bool flag = false;
          using (List<Thing>.Enumerator enumerator2 = GridsUtility.GetThingList(current1, room.Map).GetEnumerator())
          {
            while (enumerator2.MoveNext())
            {
              Thing current2 = enumerator2.Current;
              if (current2 is Building && (current2 as Building).def.building.shipPart)
                flag = true;
            }
          }
          if (!flag)
            return 0.0f;
        }
      }
      return float.MaxValue;
    }
  }
}
