using HarmonyLib;
using StardewValley.Menus;
using FishSlapper.Gameplay;

namespace FishSlapper.Patches
{
    internal static class BobberBarPatch
    {
        private static DiveSlapController? controller;

        public static void Initialize(DiveSlapController controller)
        {
            BobberBarPatch.controller = controller;
        }

        public static void Apply(Harmony harmony)
        {
            var original = AccessTools.Method(typeof(BobberBar), nameof(BobberBar.update));
            if (original is null)
                return;

            harmony.Patch(original, prefix: new HarmonyMethod(typeof(BobberBarPatch), nameof(PrefixUpdate)));
        }

        private static bool PrefixUpdate(BobberBar __instance)
        {
            return BobberBarPatch.controller?.ShouldFreezeBobberBarUpdate(__instance) != true;
        }
    }
}
