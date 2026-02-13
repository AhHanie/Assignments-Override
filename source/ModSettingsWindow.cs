using UnityEngine;
using Verse;

namespace Assignment_Overrides
{
    public static class ModSettingsWindow
    {
        public static void Draw(Rect parent)
        {
            var listing = new Listing_Standard();
            listing.Begin(parent);
            listing.CheckboxLabeled(
                "AssignOverride.Settings.EnableAll.Label".Translate(),
                ref ModSettings.EnableOverrideForAll,
                "AssignOverride.Settings.EnableAll.Desc".Translate());
            listing.GapLine();
            listing.Label("AssignOverride.Settings.RestartNotice".Translate());
            listing.End();
        }
    }
}
