using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Assignment_Overrides.Patches
{
    [HarmonyPatch(typeof(Pawn), nameof(Pawn.GetGizmos))]
    public static class PawnGetGizmosPatch
    {
        public static bool Prepare()
        {
            return !ModSettings.EnableOverrideForAll;
        }

        public static void Postfix(Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            __result = AppendOverrideGizmo(__result, __instance);
        }

        private static IEnumerable<Gizmo> AppendOverrideGizmo(IEnumerable<Gizmo> source, Pawn pawn)
        {
            foreach (var gizmo in source)
            {
                yield return gizmo;
            }

            if (pawn.Faction != Faction.OfPlayer || pawn.RaceProps.intelligence == Intelligence.Animal)
            {
                yield break;
            }

            var component = Current.Game.GetComponent<OverrideAssignmentGameComponent>();

            yield return new Command_Toggle
            {
                icon = ResourceAssets.AssignOverrideIcon,
                defaultLabel = "AssignOverride.GizmoLabel".Translate(),
                defaultDesc = "AssignOverride.GizmoDesc".Translate(),
                isActive = () => component.ShouldOverride(pawn),
                toggleAction = () =>
                {
                    if (component.ShouldOverride(pawn))
                    {
                        component.RemovePawn(pawn);
                    }
                    else
                    {
                        component.AddPawn(pawn);
                    }
                }
            };
        }
    }
}
