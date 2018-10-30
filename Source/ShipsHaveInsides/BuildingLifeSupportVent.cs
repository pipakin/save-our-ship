using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimWorld
{
    public class GasMixture : IExposable
    {
        private float inertComponentsPartial;
        private float o2Partial;
        private float co2Partial;

        /// <summary>
        /// Only for loading via Scribe.
        /// Please don't use.
        /// </summary>
        public GasMixture()
        {
            
        }

        public GasMixture(float inert, float o2, float co2)
        {
            inertComponentsPartial = inert;
            o2Partial = o2;
            co2Partial = co2;
        }

        public float totalPressure {
            get => inertComponentsPartial + o2Partial + co2Partial;
        }
        public float InertComponentsPartial { get => inertComponentsPartial; }
        public float O2Partial { get => o2Partial; }
        public float Co2Partial { get => co2Partial; }

        public readonly static GasMixture EarthNorm = new GasMixture(81.02f, 20.265f, 0.04f);
        public readonly static GasMixture SpaceSuitNorm = new GasMixture(0.0f, 20.265f, 0.0f);
        public readonly static GasMixture Vacuum = new GasMixture(0.0f, 0.0f, 0.0f);
        public static GasMixture operator +(GasMixture m, float change)
        {
            if(m.totalPressure <= 0.0f)
            {
                return GasMixture.Vacuum;
            }

            var iWeight = m.inertComponentsPartial / m.totalPressure;
            var o2Weight = m.o2Partial / m.totalPressure;
            var co2Weight = m.co2Partial / m.totalPressure;

            var value = m.totalPressure + change;

            return new GasMixture(value * iWeight, value * o2Weight, value * co2Weight);
        }

        public static GasMixture atPressure(GasMixture m, float value)
        {
            if (m.totalPressure <= 0.0f)
            {
                return GasMixture.Vacuum;
            }

            var iWeight = m.inertComponentsPartial / m.totalPressure;
            var o2Weight = m.o2Partial / m.totalPressure;
            var co2Weight = m.co2Partial / m.totalPressure;
            
            return new GasMixture(value * iWeight, value * o2Weight, value * co2Weight);
        }

        public static GasMixture operator -(GasMixture m, float change)
        {
            return m + (-change);
        }

        public static GasMixture operator *(GasMixture m, float change)
        {
            return m + (m.totalPressure * change - m.totalPressure);
        }

        public static GasMixture operator +(GasMixture m, GasMixture m2)
        {
            return new GasMixture(
                Mathf.Max(0.0f, m.inertComponentsPartial + m2.inertComponentsPartial),
                Mathf.Max(0.0f, m.o2Partial + m2.o2Partial),
                Mathf.Max(0.0f, m.co2Partial + m2.co2Partial)
            );
        }

        public static GasMixture operator -(GasMixture m, GasMixture m2)
        {
            return new GasMixture(
                Mathf.Max(0.0f, m.inertComponentsPartial - m2.inertComponentsPartial),
                Mathf.Max(0.0f, m.o2Partial - m2.o2Partial),
                Mathf.Max(0.0f, m.co2Partial - m2.co2Partial)
            );
        }

        public static GasMixture Avg(ICollection<GasMixture> mixes)
        {
            var iTotal = 0.0f;
            var o2Total = 0.0f;
            var co2Total = 0.0f;

            foreach(var mix in mixes)
            {
                iTotal += mix.inertComponentsPartial;
                o2Total += mix.o2Partial;
                co2Total += mix.co2Partial;
            }

            return new GasMixture(
                iTotal / (float)mixes.Count,
                o2Total / (float)mixes.Count,
                co2Total / (float)mixes.Count
            );
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref inertComponentsPartial, "inertComponentsPartial", GasMixture.EarthNorm.inertComponentsPartial);
            Scribe_Values.Look(ref o2Partial, "o2Partial", GasMixture.EarthNorm.o2Partial);
            Scribe_Values.Look(ref co2Partial, "co2Partialco2Partial", GasMixture.EarthNorm.co2Partial);
        }
    }

    public class GasVolume : IExposable
    {
        public GasMixture mixture;
        public float metersCubed;

        /// <summary>
        /// Only for loading via Scribe.
        /// Please don't use.
        /// </summary>
        public GasVolume()
        {

        }

        public GasVolume(GasMixture mixture, float metersCubed = 1f)
        {
            this.mixture = mixture;
            this.metersCubed = metersCubed;
        }

        public GasVolume ExpandInto(float metersCubed)
        {
            return new GasVolume
            (
                mixture: mixture + (metersCubed <= 0.0f ? 0.0f : (((mixture.totalPressure * this.metersCubed)/metersCubed) - mixture.totalPressure)),
                metersCubed: metersCubed
            );
        }

        public void ExposeData()
        {
            throw new NotImplementedException();
        }

        public GasVolume addDirect(GasVolume second)
        {
            float newMetersCubed = metersCubed;
            return new GasVolume(mixture + second.ExpandInto(newMetersCubed).mixture, newMetersCubed);
        }

        public GasVolume removeDirect(GasVolume second, out float amount)
        {
            float newMetersCubed = metersCubed;
            float oldTotal = mixture.totalPressure;
            var expanded = second.ExpandInto(newMetersCubed).mixture;
            var newMixture = mixture - expanded;

            amount = (oldTotal - newMixture.totalPressure) / expanded.totalPressure;

            return new GasVolume(newMixture, newMetersCubed);
        }

        public static GasVolume operator+(GasVolume first, GasVolume second)
        {
            float newMetersCubed = first.metersCubed + second.metersCubed;
            return new GasVolume(first.ExpandInto(newMetersCubed).mixture + second.ExpandInto(newMetersCubed).mixture, newMetersCubed);
        }

        public static GasVolume operator-(GasVolume first, GasVolume second)
        {
            float newMetersCubed = first.metersCubed - second.metersCubed;
            return new GasVolume(first.ExpandInto(newMetersCubed).mixture - second.ExpandInto(newMetersCubed).mixture, newMetersCubed);
        }

        public static GasVolume Empty = new GasVolume(GasMixture.Vacuum, 0.0f);
    }
    public class BuildingLifeSupportVent : Building
    {
        public override void ExposeData()
        {
            base.ExposeData();
        }

        public override string GetInspectString()
        {
            var sb = new StringBuilder(base.GetInspectString());
            var gasMixture = Map.GetSpaceAtmosphereMapComponent().DefinitionAt(Position).GetGas(this.GetRoom()).mixture;

            sb.AppendLine("Pressure".Translate() + " : " + (gasMixture.totalPressure / 101.325).ToString("0.00") + "atm".Translate());
            sb.AppendLine("O2 Pressure".Translate() + " : " + (gasMixture.O2Partial / 101.325).ToString("0.00") + "atm".Translate() + " (" + (gasMixture.O2Partial / gasMixture.totalPressure).ToString("P") + ")");
            sb.Append("CO2 Pressure".Translate() + " : " + (gasMixture.Co2Partial / 101.325).ToString("0.00") + "atm".Translate() + " (" + (gasMixture.Co2Partial / gasMixture.totalPressure).ToString("P") + ")");

            return sb.ToString();
        }
    }
}
