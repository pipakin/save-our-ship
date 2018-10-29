using Verse;

namespace RimWorld
{
    public class CompProperties_AirTank : CompProperties
    {
        public float maxPressure;

        public CompProperties_AirTank()
        {
            this.compClass = typeof(CompAirTank);
        }
    }
}
