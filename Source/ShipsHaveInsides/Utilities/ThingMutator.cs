using RimWorld;
using ShipsHaveInsides.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;

namespace ShipsHaveInsides.Utilities
{
    public class ThingMutator<T1> where T1 : Thing
    {
        List<Action<T1>> mutators;
        List<Func<T1, IEnumerable<T1>>> containerExpansion;
        Action<Exception> exceptionHandler = e => throw new Exception("Exception occured while processing entities", e);

        private ThingMutator(IEnumerable<Action<T1>> mutators, IEnumerable<Func<T1, IEnumerable<T1>>> containerExpansion)
        {
            this.mutators = new List<Action<T1>>(mutators);
            this.containerExpansion = new List<Func<T1, IEnumerable<T1>>>(containerExpansion);
        }

        public ThingMutator() : this(new List<Action<T1>>(), new List<Func<T1, IEnumerable<T1>>>())
        {
        }

        public ThingMutator<T1> ForAll(Action<T1> func)
        {
            return For(func);
        }

        public ThingMutator<T1> Move(Func<IntVec3, IntVec3> moveFunc)
        {
            return ForAll(t =>
            {
                t.Position = moveFunc(t.Position);
            }).For<Pawn>(pawn =>
            {
                if (pawn.pather != null)
                    pawn.pather.nextCell = moveFunc(pawn.pather.nextCell);
            });
        }

        public ThingMutator<T1> SetFaction(Faction f)
        {
            return ForAll(t =>
            {
                if (t.def.CanHaveFaction)
                    t.SetFactionDirect(f);
            });
        }

        public ThingMutator<T1> ClearOwnership()
        {
            return For<Pawn>(pawn =>
            {
                if (pawn.ownership != null)
                {
                    pawn.ownership.UnclaimAll();
                }
            });
        }

        public ThingMutator<T1> Destroy<T>(DestroyMode mode = DestroyMode.Vanish) where T: Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    Thing.allowDestroyNonDestroyable = true;
                    e.Destroy(mode);
                    Thing.allowDestroyNonDestroyable = false;
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> SetAsHome<T>(Func<T, bool> predicate = null) where T : Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T && (predicate == null || predicate(e as T)))
                {
                    e.Map.areaManager.Home[e.Position] = true;
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> DeSpawn<T>() where T : Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T && e.Spawned)
                {
                    e.DeSpawn();
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> SpawnInto<T>(Func<Map> map) where T : Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    e.SpawnSetup(map(), false);
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> For<T>(Action<T> func) where T : Thing
        {
            if (typeof(T) == typeof(T1))
            {
                return new ThingMutator<T1>(mutators.Concat(e =>
                {
                    func(e as T);
                }), containerExpansion);

            }
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    func(e as T);
                }
            }), containerExpansion);
        }


        public ThingMutator<T1> ForComp<T>(Action<T> func) where T : ThingComp
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (typeof(ThingWithComps).IsAssignableFrom(e.GetType()))
                {
                    foreach(var comp in (e as ThingWithComps).GetComps<T>())
                    {
                        func(comp);
                    }
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> ForContained<T, T2>(Func<T, T1> containedFunc, Action<T2> func)
            where T : class
            where T2 : class
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    T1 contained = containedFunc(e as T);
                    if (contained != null && contained is T2)
                    {
                        func(contained as T2);
                    }
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> ExpandContained<T, T2>(Func<T, T2> containedFunc)
            where T : class
            where T2 : T1
        {
            return new ThingMutator<T1>(mutators, containerExpansion.Concat(e =>
            {
                if (e is T)
                {
                    return new List<T1>() { containedFunc(e as T) };
                }
                return new List<T1>();
            }));
        }

        public ThingMutator<T1> ExpandContained<T, T2>(Func<T, IEnumerable<T2>> containedFunc)
            where T : class
            where T2 : T1
        {
            return new ThingMutator<T1>(mutators, containerExpansion.Concat(e =>
            {
                if (e is T)
                {
                    return containedFunc(e as T).Select(x => (T1)x);
                }
                return new List<T1>();
            }));
        }


        public HashSet<T1> UnsafeExecute(IEnumerable<T1> entities, Action<Exception> handler = null)
        {
            HashSet<T1> allEntities = new HashSet<T1>(entities);
            HashSet<T1> entitiesToAdd = new HashSet<T1>();

            foreach (Func<T1, IEnumerable<T1>> exp in containerExpansion)
            {
                foreach (T1 e in allEntities)
                {
                    entitiesToAdd.AddRange(exp(e).ToList());
                }
                allEntities.AddRange(entitiesToAdd);
            }

            foreach (T1 e in allEntities)
            {
                foreach(Action<T1> fn in mutators)
                {
                    try
                    {
                        if (e != null) fn(e);
                    }
                    catch (Exception ex)
                    {
                        (handler ?? exceptionHandler)(ex);
                    }
                }
            }

            return allEntities;
        }

        public ThenContainer<T1> QueueAsLongEvent(IEnumerable<T1> entities, string textKey, bool async, Action<Exception> handler)
        {
            var items = entities.ToList();
            var cont = new ThenContainer<T1>();
            exceptionHandler = handler;
            LongEventHandler.QueueLongEvent(() =>
            {
                ShipInteriorMod.Log(textKey.Translate() + "...");
                cont.Resolve(UnsafeExecute(items, handler), async);
            }, textKey, async, handler);

            return cont;
        }

        public class ThenContainer<TResult> where TResult : Thing
        {
            private HashSet<TResult> results;
            private bool async;

            public void Resolve(HashSet<TResult> results, bool async)
            {
                this.results = results;
                this.async = async;
            }

            public ThenContainer<TResult> Then(ThingMutator<TResult> mutator, string textKey, Action<Exception> handler)
            {
                var cont = new ThenContainer<TResult>();
                LongEventHandler.QueueLongEvent(() =>
                {
                    ShipInteriorMod.Log(textKey.Translate() + "...");
                    if(results != null)
                        cont.Resolve(mutator.UnsafeExecute(results), async);
                }, textKey, async, handler);
                return cont;
            }

            public ThenContainer<TResult> Then(Action action, string textKey, Action<Exception> handler)
            {
                LongEventHandler.QueueLongEvent(action, textKey, async, handler);
                return this;
            }
        }
    }
}
