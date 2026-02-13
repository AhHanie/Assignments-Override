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

            // Insert label append immediately after actionable prioritize text is assigned
            // in the final else block (stloc.2 followed by dup).
            var actionableTextStoreIndex = code.FindIndex(ci => ci.opcode == OpCodes.Stloc_2);
            while (actionableTextStoreIndex >= 0 &&
                   (actionableTextStoreIndex + 1 >= code.Count || code[actionableTextStoreIndex + 1].opcode != OpCodes.Dup))
            {
                actionableTextStoreIndex = code.FindIndex(actionableTextStoreIndex + 1, ci => ci.opcode == OpCodes.Stloc_2);
            }

            if (actionableTextStoreIndex < 0)
            {
                Logger.Error("Failed to locate actionable text assignment in GetWorkGiverOption transpiler patch.");
                return code;
            }

            code.InsertRange(actionableTextStoreIndex + 1, new[]
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
                return text;
            }

            if (pawn.workSettings.GetPriority(workType) != 0)
            {
                return text;
            }

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
