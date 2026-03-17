using System.Collections.Generic;
using System.IO;
using FishSlapper.Gameplay;
using FishSlapper.Patches;
using FishSlapper.Rendering;
using FishSlapper.Vanilla;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace FishSlapper
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private DiveSlapController Controller = null!;
        private DiveSlapRenderer Renderer = null!;

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();
            this.Renderer = new DiveSlapRenderer();
            this.Controller = new DiveSlapController(
                helper,
                this.Monitor,
                this.Config,
                this.Renderer,
                new VanillaFishingBridge()
            );

            var harmony = new Harmony(this.ModManifest.UniqueID);
            BobberBarPatch.Initialize(this.Controller);
            FarmerDrawPatch.Initialize(this.Controller);
            Game1DrawToolPatch.Initialize(this.Controller);
            BobberBarPatch.Apply(harmony);
            FarmerDrawPatch.Apply(harmony);
            Game1DrawToolPatch.Apply(harmony);

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Display.RenderingWorld += this.OnRenderingWorld;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
            helper.Events.Display.RenderedActiveMenu += this.OnRenderedActiveMenu;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: this.ModManifest,
                reset: () =>
                {
                    this.Config = new ModConfig();
                    this.Controller.UpdateConfig(this.Config);
                },
                save: () =>
                {
                    this.Helper.WriteConfig(this.Config);
                    this.Controller.UpdateConfig(this.Config);
                }
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => this.Config.SlapKey,
                setValue: value => this.Config.SlapKey = value,
                name: () => this.Helper.Translation.Get("config.slap-key.name"),
                tooltip: () => this.Helper.Translation.Get("config.slap-key.tooltip")
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => this.Config.DiveSlapKey,
                setValue: value => this.Config.DiveSlapKey = value,
                name: () => this.Helper.Translation.Get("config.dive-slap-key.name"),
                tooltip: () => this.Helper.Translation.Get("config.dive-slap-key.tooltip")
            );
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
                return;

            e.Edit(asset =>
            {
                var data = asset.AsDictionary<string, StardewValley.GameData.AudioCueData>().Data;
                string audioFilePath = Path.Combine(this.Helper.DirectoryPath, "assets", "slap.wav");

                data[ModConstants.SlapSoundId] = new StardewValley.GameData.AudioCueData
                {
                    Id = ModConstants.SlapSoundId,
                    FilePaths = new List<string> { audioFilePath },
                    Category = "Sound"
                };
            });
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            this.Controller.OnButtonPressed(e);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            this.Controller.OnUpdateTicked();
        }

        private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
        {
            this.Renderer.OnRenderingWorld(this.Controller.ActiveSession);
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            this.Renderer.OnRenderedWorld(e, this.Controller.ActiveSession);
        }

        private void OnRenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            this.Renderer.OnRenderedActiveMenu(e, this.Controller.ActiveSession);
        }

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            this.Controller.OnMenuChanged(e);
        }
    }
}
