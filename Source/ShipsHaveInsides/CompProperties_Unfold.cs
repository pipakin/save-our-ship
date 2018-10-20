using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class CompProperties_Unfold : CompProperties
    {
        public float extendRate;
        public float retractRate;
        public int retractTime;
        public IntVec3 extendDirection;
        public float startOffset;
        public float length;
        [NoTranslate]
        public string graphicPath;
        [Unsaved]
        public Material unfoldGraphic;

        public CompProperties_Unfold()
        {
            this.compClass = typeof(UnfoldComponent);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            LongEventHandler.ExecuteWhenFinished((Action)(() => this.unfoldGraphic = MaterialPool.MatFrom(this.graphicPath)));
        }
    }
}
