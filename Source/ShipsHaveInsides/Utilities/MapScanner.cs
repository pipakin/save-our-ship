using ShipsHaveInsides.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ShipsHaveInsides.Utilities
{
    public class MapScanner
    {
        private List<MapScannerAction> actions;

        private enum MapScannerActionType
        {
            Point,
            Thing
        }

        private class MapScannerAction
        {
            public MapScannerActionType type;
            public Action<IntVec3, Map> pointAction;
            public Action<Thing> thingAction;
            public int minOffset;
            public int maxOffset;
        }

        private MapScanner(IEnumerable<MapScannerAction> actions)
        {
            this.actions = actions.ToList();
        }

        public MapScanner() : this(new List<MapScannerAction>()) { }

        public MapScanner ForPoints(Action<IntVec3, Map> fn, int minOffset = 0, int maxOffset = 0)
        {
            return new MapScanner(actions.Concat(new MapScannerAction()
            {
                type = MapScannerActionType.Point,
                pointAction = fn,
                minOffset = minOffset,
                maxOffset = maxOffset
            }));
        }

        public MapScanner Unfog(int minOffset = 0, int maxOffset = 0)
        {
            return ForPoints((point, map) =>
            {
                try
                {
                    map.fogGrid.Unfog(point);
                }
                catch (Exception e)
                {
                    ShipInteriorMod.Log("Whoops, couldn't unfog: " + e.Message);
                }
            }, minOffset, maxOffset);
        }

        public MapScanner For<T>(Action<T> fn, int minOffset = 0, int maxOffset = 0) where T : Thing
        {
            if(typeof(T) == typeof(Thing))
            {
                return new MapScanner(actions.Concat(new MapScannerAction()
                {
                    type = MapScannerActionType.Thing,
                    thingAction = t => fn(t as T),
                    minOffset = minOffset,
                    maxOffset = maxOffset
                }));
            }
            return new MapScanner(actions.Concat(new MapScannerAction()
            {
                type = MapScannerActionType.Thing,
                thingAction = t => {
                    if (t is T) fn(t as T);
                }
            }));
        }

        public MapScanner DestroyThings(int minOffset = 0, int maxOffset = 0)
        {
            return For<Thing>(t =>
            {
                Thing.allowDestroyNonDestroyable = true;
                t.Destroy(DestroyMode.Vanish);
                Thing.allowDestroyNonDestroyable = false;
            }, minOffset, maxOffset);
        }

        public void QueueAsLongEvent(Map map, IntVec3 min, IntVec3 max, string textKey, bool async, Action<Exception> handler)
        {
            LongEventHandler.QueueLongEvent(() =>
            {
                ShipInteriorMod.Log(textKey.Translate() + "...");
                UnsafeExecute(map, min, max);
            }, textKey, async, handler);
        }

        public void UnsafeExecute(Map map, IntVec3 min, IntVec3 max)
        {
            int minminOffset = actions.Min(a => a.minOffset);
            int maxmaxOffset = actions.Max(a => a.maxOffset);
            bool needThings = actions.Any(a => a.type == MapScannerActionType.Thing);

            for(int x = min.x + minminOffset;x < max.x + maxmaxOffset;x++)
            {
                for (int z = min.z + minminOffset; z < max.z + maxmaxOffset; z++)
                {
                    IEnumerable<Thing> things = null;
                    IntVec3 position = new IntVec3(x, 0, z);

                    if (needThings)
                        things = map.thingGrid.ThingsAt(position);

                    foreach (MapScannerAction action in actions)
                    {
                        if (x < min.x + action.minOffset) continue;
                        if (x > max.x + action.maxOffset) continue;
                        if (z < min.z + action.minOffset) continue;
                        if (z > max.z + action.maxOffset) continue;
                        switch (action.type)
                        {
                            case MapScannerActionType.Point:
                                action.pointAction(position, map);
                                break;
                            case MapScannerActionType.Thing:
                                foreach(Thing t in things)
                                {
                                    action.thingAction(t);
                                }
                                break;
                        }
                    }
                }
            }
        }
    }

}
