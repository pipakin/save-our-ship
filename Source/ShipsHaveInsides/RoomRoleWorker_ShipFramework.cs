using System.Collections;
using System.Collections.Generic;
using Verse;

namespace RimWorld
{
  public class RoomRoleWorker_ShipFramework : RoomRoleWorker
  {
    public override float GetScore(Room room)
    {
      using (IEnumerator<IntVec3> enumerator = room.BorderCells.GetEnumerator())
      {
        while (((IEnumerator) enumerator).MoveNext())
        {
          Building edifice = GridsUtility.GetEdifice(enumerator.Current, room.Map);
          if (edifice == null || !edifice.def.building.shipPart)
            return 0.0f;
        }
      }
      return 1.701412E+38f;
    }
  }
}
