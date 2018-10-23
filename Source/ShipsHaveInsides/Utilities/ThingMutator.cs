using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace ShipsHaveInsides.Utilities
{
    public class ThingMutator<T1> where T1 : Thing
    {
        List<Action<T1>> mutators;
        List<Func<T1, IEnumerable<T1>>> containerExpansion;

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

        public ThingMutator<T1> Destroy<T>() where T: Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    Thing.allowDestroyNonDestroyable = true;
                    e.Destroy(DestroyMode.Vanish);
                    Thing.allowDestroyNonDestroyable = false;
                }
            }), containerExpansion);
        }

        public ThingMutator<T1> DeSpawn<T>() where T : Thing
        {
            return new ThingMutator<T1>(mutators.Concat(e =>
            {
                if (e is T)
                {
                    e.DeSpawn();
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

        public ThingMutator<T1> ExpandContained<T, T2>(Func<T, T1> containedFunc)
            where T : class
            where T2 : class
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

        public ThingMutator<T1> ExpandContained<T, T2>(Func<T, IEnumerable<T1>> containedFunc)
            where T : class
            where T2 : class
        {
            return new ThingMutator<T1>(mutators, containerExpansion.Concat(e =>
            {
                if (e is T)
                {
                    return containedFunc(e as T);
                }
                return new List<T1>();
            }));
        }

        public void UnsafeExecute(IEnumerable<T1> entities)
        {
            List<T1> entitiesToAdd = new List<T1>();

            foreach (Func<T1, IEnumerable<T1>> exp in containerExpansion)
            {
                foreach (T1 e in entities)
                {
                    entitiesToAdd.AddRange(exp(e));
                }
            }

            var list = entities.Concat(entitiesToAdd).ToList();

            foreach (T1 e in list)
            {
                foreach(Action<T1> fn in mutators)
                {
                    if(e != null)fn(e);
                }
            }
        }
    }
}
