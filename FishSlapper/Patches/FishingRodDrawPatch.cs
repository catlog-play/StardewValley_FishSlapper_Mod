using HarmonyLib;
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
            var original = AccessTools.DeclaredMethod(typeof(FishingRod), nameof(FishingRod.draw), new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) });
            if (original is not null)
                harmony.Patch(original, prefix: new HarmonyMethod(typeof(FishingRodDrawPatch), nameof(PrefixDraw)));
        }

        private static bool PrefixDraw(FishingRod __instance)
        {
            if (FishingRodDrawPatch.controller is null)
                return true;

            return !FishingRodDrawPatch.controller.ShouldSuppressFishingRodDraw(__instance);
        }
    }
}
