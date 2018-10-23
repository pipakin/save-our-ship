using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimWorld
{
    public class CompDefaultStuff : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            if (this.parent.Stuff == null)
            {
                int value = this.parent.MaxHitPoints - this.parent.HitPoints;
                this.parent.SetStuffDirect((props as CompProperties_DefaultStuff).defaultStuffDef);
                this.parent.HitPoints = this.parent.MaxHitPoints - value;
            }
        }
    }
}
