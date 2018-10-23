using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    /// <summary>
    /// Currently only handles 1x1 with a 3 long extension. Should probably read from def, too. Will get there...
    /// </summary>
    public class UnfoldComponent : ThingComp
    {
        private float extension = 0.0f;
        private int timeTillRetract;
               
        public CompProperties_Unfold Props
        {
            get
            {
                return (CompProperties_Unfold)this.props;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(this.parent.DrawPos + (Props.extendDirection.RotatedBy(this.parent.Rotation).ToVector3() * Props.startOffset) + (Props.extendDirection.RotatedBy(this.parent.Rotation).ToVector3() * (Props.length / 2) * extension) + Altitudes.AltIncVect, this.parent.Rotation.AsQuat, new Vector3(1, 1f, Props.length * extension));
            Graphics.DrawMesh(MeshPool.plane10, matrix, Props.unfoldGraphic, 0);
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("UnfoldStatus".Translate());
           
            if(Mathf.Approximately(extension, Target))
            {
                if (Mathf.Approximately(extension, 0.0f))
                {
                    stringBuilder.Append("UnfoldRetracted".Translate());
                }
                else
                {
                    stringBuilder.Append("UnfoldExtended".Translate());
                }
            }
            else
            {
                if(extension < Target)
                {
                    stringBuilder.Append("UnfoldExtending".Translate());
                }
                else if(extension > Target)
                {
                    if(Mathf.Approximately(timeTillRetract, 0.0f))
                    {
                        stringBuilder.Append("UnfoldRetracting".Translate());
                    }
                    else
                    {
                        stringBuilder.Append("UnfoldExtended".Translate());
                    }
                }
            }

            return stringBuilder.ToString();
        }

        public override void CompTick()
        {
            base.CompTick();
            if (Target > extension)
            {
                extension += Props.extendRate;
                if (extension > Target) extension = Target;
                timeTillRetract = Props.retractTime;
            }
            else if (Target < extension)
            {
                timeTillRetract -= 1;
                if (timeTillRetract <= 0)
                {
                    extension -= Props.retractRate;
                    if (extension < Target) extension = Target;
                    timeTillRetract = 0;
                }
            }
            else
            {
                timeTillRetract = Props.retractTime;
            }
        }

        private float target = 0.0f;
        public float Target {
            set
            {
                target = value;
                if (target > 1.0f) target = 1.0f;
                if (target < 0.0f) target = 0.0f;
            }
            get
            {
                return target;
            }
        }

        public bool IsAtTarget
        {
            get
            {
                return Mathf.Approximately(extension, Target);
            }
        }
    }
}
