using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace FishSlapper
{
    public class ModConfig
    {
        public KeybindList SlapKey { get; set; } = KeybindList.Parse("MouseRight, Space");
    }
}
