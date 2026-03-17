using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FishSlapper
{
    public class ModConfig
    {
        public KeybindList SlapKey { get; set; } = KeybindList.Parse("MouseRight, Space");
        public KeybindList DiveSlapKey { get; set; } = KeybindList.Parse("Q");
        public bool EnableDiveSlap { get; set; } = true;
        public int DiveSlapDurationTicks { get; set; } = 300;
        public int DiveSlapRequiredHits { get; set; } = 5;
        public bool CancelPerfectOnDiveSuccess { get; set; } = true;
    }
}
