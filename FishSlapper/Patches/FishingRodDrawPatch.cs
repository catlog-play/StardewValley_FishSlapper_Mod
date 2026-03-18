using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Tools;
using FishSlapper.Gameplay;

namespace FishSlapper.Patches
{
    internal static class FishingRodDrawPatch
    {
        private static DiveSlapController? controller;

        public static void Initialize(DiveSlapController controller)
        {
            FishingRodDrawPatch.controller = controller;
        }

        public static void Apply(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(FishingRod), nameof(FishingRod.draw), new[] { typeof(SpriteBatch) });
            if (original is null)
                return;

            harmony.Patch(original, prefix: new HarmonyMethod(typeof(FishingRodDrawPatch), nameof(PrefixDraw)));
        }

        private static bool PrefixDraw(FishingRod __instance, SpriteBatch b)
        {
            if (FishingRodDrawPatch.controller is null)
                return true;

            Farmer drawFarmer = __instance.lastUser ?? Game1.player;
            if (!FishingRodDrawPatch.controller.ShouldHideCaughtFishToolPreview(drawFarmer))
                return true;

            return !FishingRodDrawPatch.controller.TryDrawCaughtFishPreview(__instance, b, drawFarmer);
        }
    }
}
