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

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude)
        {
            base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude);

            GraphicDatabase.Get<Graphic_Single>(graphicPath, ShaderTypeDefOf.EdgeDetect.Shader, new Vector2(1, 3), ghostCol)
                .DrawFromDef(GenThing.TrueCenter(center, rot, thingDef.Size, drawAltitude.AltitudeFor()) + (IntVec3.South * 2).RotatedBy(rot).ToVector3(), rot, thingDef);

            
        }
    }
}
