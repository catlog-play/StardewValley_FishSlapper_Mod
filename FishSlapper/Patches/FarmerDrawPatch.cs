using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using FishSlapper.Gameplay;

namespace FishSlapper.Patches
{
    internal static class FarmerDrawPatch
    {
        private static DiveSlapController? controller;

        public static void Initialize(DiveSlapController controller)
        {
            FarmerDrawPatch.controller = controller;
        }

        public static void Apply(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(Farmer), "draw", new[] { typeof(SpriteBatch) });
            if (original is null)
                return;

            harmony.Patch(original, prefix: new HarmonyMethod(typeof(FarmerDrawPatch), nameof(PrefixDraw)));
        }

        private static bool PrefixDraw(Farmer __instance, SpriteBatch b)
        {
            if (FarmerDrawPatch.controller is null)
                return true;

            return !FarmerDrawPatch.controller.TryDrawLocalPlayerReplacement(__instance, b);
        }
    }
}
