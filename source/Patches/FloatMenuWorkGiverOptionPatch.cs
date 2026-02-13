using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Assignment_Overrides.Patches
{
    [HarmonyPatch(typeof(FloatMenuOptionProvider_WorkGivers), "GetWorkGiverOption")]
    public static class FloatMenuWorkGiverOptionPatch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var code = new List<CodeInstruction>(instructions);
            var matcher = new CodeMatcher(code, generator);

            var displayClassType = AccessTools.TypeByName("RimWorld.FloatMenuOptionProvider_WorkGivers+<>c__DisplayClass17_0");
            var pawnField = AccessTools.Field(displayClassType, "pawn");
            var workSettingsField = AccessTools.Field(typeof(Pawn), "workSettings");
            var getPriorityMethod = AccessTools.Method(typeof(Pawn_WorkSettings), nameof(Pawn_WorkSettings.GetPriority));
            var shouldAllowMethod = AccessTools.Method(typeof(FloatMenuWorkGiverOptionPatch), nameof(ShouldAllowPriorityBranch));
            var appendLabelMethod = AccessTools.Method(typeof(FloatMenuWorkGiverOptionPatch), nameof(AppendFloatMenuLabelIfNeeded));
            var actionableDisplayClassType = AccessTools.TypeByName("RimWorld.FloatMenuOptionProvider_WorkGivers+<>c__DisplayClass17_1");
            var localJobField = AccessTools.Field(actionableDisplayClassType, "localJob");

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldfld, pawnField),
                new CodeMatch(OpCodes.Ldfld, workSettingsField),
                new CodeMatch(ci => ci.opcode == OpCodes.Ldloc_S || ci.opcode == OpCodes.Ldloc),
                new CodeMatch(OpCodes.Callvirt, getPriorityMethod),
                new CodeMatch(ci => ci.opcode == OpCodes.Brtrue || ci.opcode == OpCodes.Brtrue_S));

            if (!matcher.IsValid)
            {
                Logger.Error("Failed to find work priority check in GetWorkGiverOption transpiler patch.");
                return code;
            }

            var priorityCheckStart = matcher.Pos;
            var branchIndex = priorityCheckStart + 5;
            var branchInstruction = code[branchIndex];
            if (!(branchInstruction.operand is Label priorityNonZeroLabel))
            {
                Logger.Error("Failed to read branch target in GetWorkGiverOption transpiler patch.");
                return code;
            }

            if (shouldAllowMethod == null)
            {
                Logger.Error("Failed to resolve ShouldAllowPriorityBranch in GetWorkGiverOption transpiler patch.");
                return code;
            }
            if (appendLabelMethod == null)
            {
                Logger.Error("Failed to resolve AppendFloatMenuLabelIfNeeded in GetWorkGiverOption transpiler patch.");
                return code;
            }
            if (localJobField == null)
            {
                Logger.Error("Failed to resolve localJob field in GetWorkGiverOption transpiler patch.");
                return code;
            }

            // Replace the entire priority check block:
            // pawn.workSettings.GetPriority(workType) != 0
            // with:
            // ShouldAllowPriorityBranch(pawn, workType)
            var replacement = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, pawnField),
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Call, shouldAllowMethod),
                new CodeInstruction(branchInstruction.opcode == OpCodes.Brtrue_S ? OpCodes.Brtrue_S : OpCodes.Brtrue, priorityNonZeroLabel)
            };
            replacement[0].labels.AddRange(code[priorityCheckStart].labels);
            code.RemoveRange(priorityCheckStart, 6);
            code.InsertRange(priorityCheckStart, replacement);

            if (!ModSettings.RenderFloatMenuAssignmentLabel)
            {
                Logger.Message("Skipping float menu label insertion because the setting is disabled.");
                return code;
            }

            // Insert label append immediately before localJob is stored in
            // <>c__DisplayClass17_1. At this point, the actionable prioritize
            // text assignment block has already completed.
            var localJobStoreIndex = code.FindIndex(ci =>
                ci.opcode == OpCodes.Stfld &&
                Equals(ci.operand, localJobField));

            if (localJobStoreIndex < 0)
            {
                Logger.Error("Failed to locate actionable text merge point in GetWorkGiverOption transpiler patch.");
                return code;
            }

            code.InsertRange(localJobStoreIndex, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_2),
                new CodeInstruction(OpCodes.Ldloc_0),
                new CodeInstruction(OpCodes.Ldfld, pawnField),
                new CodeInstruction(OpCodes.Ldloc_S, 6),
                new CodeInstruction(OpCodes.Call, appendLabelMethod),
                new CodeInstruction(OpCodes.Stloc_2)
            });

            Logger.Message("GetWorkGiverOption transpiler patch applied.");
            return code;
        }

        private static string AppendFloatMenuLabelIfNeeded(string text, Pawn pawn, WorkTypeDef workType)
        {
            if (!ShouldPawnAssignmentOverride(pawn))
            {
                Logger.Message("Shouldn't assign no text");
                return text;
            }

            if (pawn.workSettings.GetPriority(workType) != 0)
            {
                Logger.Message("priority not 0 no text");
                return text;
            }

            Logger.Message("Adding text");
            return text + " " + "AssignOverride.FloatMenuLabel".Translate();
        }

        private static bool ShouldAllowPriorityBranch(Pawn pawn, WorkTypeDef workType)
        {
            if (pawn.WorkTypeIsDisabled(workType))
            {
                return false;
            }

            return ShouldPawnAssignmentOverride(pawn);
        }

        private static bool ShouldPawnAssignmentOverride(Pawn pawn)
        {
            if (ModSettings.EnableOverrideForAll)
            {
                return true;
            }

            var component = Current.Game.GetComponent<OverrideAssignmentGameComponent>();
            return component.ShouldOverride(pawn);
        }
    }
}
