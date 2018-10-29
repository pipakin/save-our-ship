using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;


namespace RimWorld
{
    public class HediffGiver_Hypercapnia : HediffGiver
    {
        public override bool OnHediffAdded(Pawn pawn, Hediff hediff)
        {
            if (hediff.def == this.hediff)
            {
                SendLetter(pawn, hediff);
            }

            return base.OnHediffAdded(pawn, hediff);
        }
        public override void OnIntervalPassed(Pawn pawn, Hediff cause)
        {
            base.OnIntervalPassed(pawn, cause);
            var ship = pawn?.Map?.GetSpaceAtmosphereMapComponent()?.DefinitionAt(pawn.Position);
            HediffSet hediffSet = pawn.health.hediffSet;
            Hediff firstHediffOfDef = hediffSet.GetFirstHediffOfDef(hediff);

            //TODO: actually use a component on the headgear to determine time of o2
            if (pawn?.apparel?.WornApparel?.Any(x => x.def.defName == "Apparel_PowerArmorHelmet") == true)
            {
                if (firstHediffOfDef != null)
                {
                    float value = firstHediffOfDef.Severity * 0.027f;
                    value = Mathf.Clamp(value, 0.0015f, 0.015f);
                    firstHediffOfDef.Severity -= value;
                }
                return;
            }

            if (ship == null)
            {
                if (firstHediffOfDef != null)
                {
                    float value = firstHediffOfDef.Severity * 0.027f;
                    value = Mathf.Clamp(value, 0.0015f, 0.015f);
                    firstHediffOfDef.Severity -= value;
                }
            }
            else
            {
                if (pawn.Map != null && pawn.GetRoom() != null && ship.GetGas(pawn.GetRoom()).mixture.Co2Partial >= 5f)
                {
                    //increase severity
                    HealthUtility.AdjustSeverity(pawn, hediff, 0.000375f);
                }
                else if (firstHediffOfDef != null)
                {
                    float value = firstHediffOfDef.Severity * 0.027f;
                    value = Mathf.Clamp(value, 0.0015f, 0.015f);
                    firstHediffOfDef.Severity -= value;
                }
            }
        }
    }
}
