using HarmonyLib;
using RimWorld;
using Verse;

namespace Assignment_Overrides.Patches
{
    // Wraps the single Pawn_WorkSettings.GetPriority call in GetWorkGiverOption so a zero priority
    // reads as non-zero for pawns allowed to override. Disabled work types stay disabled.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetWorkGiverOption")]
    public static class FloatMenuWorkGiverOptionPatch
    {
        [HarmonyInfix(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority), typeof(WorkTypeDef))]
        public static int Postfix(int __result, WorkTypeDef w, [HarmonyOuter] Pawn pawn)
        {
            if (pawn.WorkTypeIsDisabled(w))
            {
                return 0;
            }

            if (__result != 0)
            {
                return __result;
            }

            return ShouldPawnAssignmentOverride(pawn) ? 1 : 0;
        }

        public static bool ShouldPawnAssignmentOverride(Pawn pawn)
        {
            if (ModSettings.EnableOverrideForAll)
            {
                return true;
            }

            return Current.Game.GetComponent<OverrideAssignmentGameComponent>().ShouldOverride(pawn);
        }
    }

    // Wraps the single FloatMenuUtility.DecoratePrioritizedTask call, which runs before vanilla
    // dedupes option labels, so the marker takes part in that check like the old in-text suffix did.
    [HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetWorkGiverOption")]
    public static class FloatMenuWorkGiverOptionLabelPatch
    {
        public static bool Prepare()
        {
            return ModSettings.RenderFloatMenuAssignmentLabel;
        }

        [HarmonyInfix(typeof(FloatMenuUtility), nameof(FloatMenuUtility.DecoratePrioritizedTask))]
        public static void Prefix(FloatMenuOption option, Pawn pawn, [HarmonyOuter] WorkGiverDef workGiver)
        {
            if (option.action == null)
            {
                return;
            }

            if (pawn.workSettings.GetPriority(workGiver.workType) != 0)
            {
                return;
            }

            if (!FloatMenuWorkGiverOptionPatch.ShouldPawnAssignmentOverride(pawn))
            {
                return;
            }

            option.Label = option.Label + " " + "AssignOverride.FloatMenuLabel".Translate();
        }
    }
}
