using UnityEngine;
using Verse;

namespace Assignment_Overrides
{
    [StaticConstructorOnStartup]
    public static class ResourceAssets
    {
        public static readonly Texture2D AssignOverrideIcon;

        static ResourceAssets()
        {
            AssignOverrideIcon = ContentFinder<Texture2D>.Get("AssignOverride/GizmoIcon");
        }
    }
}
