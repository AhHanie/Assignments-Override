using HarmonyLib;
using UnityEngine;
using Verse;

namespace Assignment_Overrides
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "AssignOverride.LoadingLabel", doAsynchronously: true, null);
        }

        public void Init()
        {
            GetSettings<ModSettings>();
            new Harmony("sk.assignoverride").PatchAll();
        }

        public override string SettingsCategory()
        {
            return "AssignOverride.SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModSettingsWindow.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
