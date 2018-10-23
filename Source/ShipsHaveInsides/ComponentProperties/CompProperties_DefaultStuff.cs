using Verse;

namespace RimWorld
{
    public class CompProperties_DefaultStuff : CompProperties
    {
        [NoTranslate]
        public string defaultStuff;

        [Unsaved]
        public ThingDef defaultStuffDef;

        public CompProperties_DefaultStuff()
        {
            this.compClass = typeof(CompDefaultStuff);
        }

        public override void ResolveReferences(ThingDef parentDef)
        {
            base.ResolveReferences(parentDef);
            defaultStuffDef = (ThingDef)GenDefDatabase.GetDef(typeof(ThingDef), defaultStuff, true);
        }
    }
}
