using Verse;

namespace Assignment_Overrides
{
    public class ModSettings : Verse.ModSettings
    {
        public static bool EnableOverrideForAll = false;
        public static bool RenderFloatMenuAssignmentLabel = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref EnableOverrideForAll, "EnableOverrideForAll", false);
            Scribe_Values.Look(ref RenderFloatMenuAssignmentLabel, "RenderFloatMenuAssignmentLabel", true);
        }
    }
}
