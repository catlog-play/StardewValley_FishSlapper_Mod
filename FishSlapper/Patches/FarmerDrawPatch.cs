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
            var farmerDraw = AccessTools.Method(typeof(Farmer), "draw", new[] { typeof(SpriteBatch) });
            if (farmerDraw is not null)
                harmony.Patch(farmerDraw, prefix: new HarmonyMethod(typeof(FarmerDrawPatch), nameof(PrefixFarmerDraw)));

            Type[][] inheritedSignatures =
            {
                new[] { typeof(SpriteBatch), typeof(float) },
                new[] { typeof(SpriteBatch), typeof(int), typeof(float) }
            };

            var inheritedPrefix = new HarmonyMethod(typeof(FarmerDrawPatch), nameof(PrefixCharacterDraw));
            foreach (Type[] signature in inheritedSignatures)
            {
                var original = AccessTools.DeclaredMethod(typeof(Character), "draw", signature);
                if (original is not null)
                    harmony.Patch(original, prefix: inheritedPrefix);
            }
        }

        private static bool PrefixFarmerDraw(Farmer __instance, SpriteBatch __0)
        {
            if (FarmerDrawPatch.controller is null)
                return true;

            return !FarmerDrawPatch.controller.TryDrawFarmerReplacement(__instance, __0);
        }

        private static bool PrefixCharacterDraw(Character __instance, SpriteBatch __0)
        {
            if (FarmerDrawPatch.controller is null || __instance is not Farmer farmer)
                return true;

            return !FarmerDrawPatch.controller.TryDrawFarmerReplacement(farmer, __0);
        }
    }
}
