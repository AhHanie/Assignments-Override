using Verse;

namespace Assignment_Overrides
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool EnableOverrideForAll = false;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableOverrideForAll, "EnableOverrideForAll", false);
        }
    }
}
