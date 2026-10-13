using System.Collections.Generic;
using Verse;

namespace Assignment_Overrides
{
    public class OverrideAssignmentGameComponent : GameComponent
    {
        private HashSet<Pawn> overridePawns = new HashSet<Pawn>();

        public OverrideAssignmentGameComponent(Game game)
        {
        }

        public void AddPawn(Pawn pawn)
        {
            overridePawns.Add(pawn);
        }

        public void RemovePawn(Pawn pawn)
        {
            overridePawns.Remove(pawn);
        }

        public bool ShouldOverride(Pawn pawn)
        {
            if (ModSettings.EnableOverrideForAll)
            {
                return true;
            }

            return overridePawns.Contains(pawn);
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                overridePawns.RemoveWhere(IsInvalidPawn);
            }

            Scribe_Collections.Look(ref overridePawns, "overridePawns", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && overridePawns == null)
            {
                overridePawns = new HashSet<Pawn>();
            }
        }

        private static bool IsInvalidPawn(Pawn pawn)
        {
            return pawn == null || pawn.Dead || pawn.Destroyed;
        }
    }
}
