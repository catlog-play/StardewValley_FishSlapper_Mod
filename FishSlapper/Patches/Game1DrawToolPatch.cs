using HarmonyLib;
using StardewValley;
using StardewValley.Tools;
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
            var original = AccessTools.Method(typeof(Game1), "drawTool", new[] { typeof(Farmer), typeof(int) });
            if (original is null)
                return;

            harmony.Patch(original, prefix: new HarmonyMethod(typeof(Game1DrawToolPatch), nameof(PrefixDrawTool)));
        }

        private static bool PrefixDrawTool(Farmer __0)
        {
            if (Game1DrawToolPatch.controller is null)
                return true;

            if (Game1DrawToolPatch.controller.ShouldHideCaughtFishToolPreview(__0))
            {
                // Ensure FishingRod.draw() still runs so our FishingRodDrawPatch can render
                // the custom caught-fish preview while suppressing vanilla tool draw.
                if (__0.CurrentTool is FishingRod rod && Game1.spriteBatch is not null)
                    rod.draw(Game1.spriteBatch);

                return false;
            }

            return !Game1DrawToolPatch.controller.ShouldSuppressToolDraw(__0);
        }
    }
}
