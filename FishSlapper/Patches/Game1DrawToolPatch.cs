using HarmonyLib;
using StardewValley;
using FishSlapper.Gameplay;

namespace FishSlapper.Patches
{
    internal static class Game1DrawToolPatch
    {
        private static DiveSlapController? controller;

        public static void Initialize(DiveSlapController controller)
        {
            Game1DrawToolPatch.controller = controller;
        }

        public static void Apply(Harmony harmony)
        {
            Type[][] signatures =
            {
                new[] { typeof(Farmer) },
                new[] { typeof(Farmer), typeof(int) }
            };

            var prefix = new HarmonyMethod(typeof(Game1DrawToolPatch), nameof(PrefixDrawTool));
            foreach (Type[] signature in signatures)
            {
                var original = AccessTools.Method(typeof(Game1), "drawTool", signature);
                if (original is not null)
                    harmony.Patch(original, prefix: prefix);
            }
        }

        private static bool PrefixDrawTool(Farmer __0)
        {
            if (Game1DrawToolPatch.controller is null)
                return true;

            return !Game1DrawToolPatch.controller.ShouldSuppressToolDraw(__0);
        }
    }
}
