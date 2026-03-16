using System;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;

namespace FishSlapper
{
    public class ModEntry : Mod
    {
        private ModConfig Config = null!;
        private readonly string SlapSoundId = "iwyxdxl.FishSlapper_SlapSound";

        // 出拳帧动画
        private int slapTick = -1;
        private static bool hideCaughtFishPreview;
        private const int SlapPunchFrame = 278; //272 punchDown, 274 punchRight, 276 punchUp, 278 punchLeft
        private const int SlapDurationTicks = 30;

        // 爆破粒子系统
        private Texture2D? pixelTexture;
        private readonly List<BurstParticle> burstParticles = new();
        private readonly Random rng = new();

        private sealed class BurstParticle
        {
            public Vector2 WorldPos;
            public Vector2 Velocity;
            public float Alpha;
            public float AlphaDecay;
            public float Width;
            public float Height;
            public float Rotation;
            public float RotationSpeed;
            public Color Color;
        }

        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
                original: AccessTools.Method(typeof(Game1), "drawTool", new[] { typeof(Farmer), typeof(int) }),
                prefix: new HarmonyMethod(typeof(ModEntry), nameof(PrefixGame1DrawTool))
            );

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
            helper.Events.Display.RenderingWorld += this.OnRenderingWorld;
            helper.Events.Display.RenderedWorld += this.OnRenderedWorld;
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
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                getValue: () => this.Config.SlapKey,
                setValue: value => this.Config.SlapKey = value,
                name: () => this.Helper.Translation.Get("config.slap-key.name"),
                tooltip: () => this.Helper.Translation.Get("config.slap-key.tooltip")
            );
        }

        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, StardewValley.GameData.AudioCueData>().Data;
                    string audioFilePath = Path.Combine(this.Helper.DirectoryPath, "assets", "slap.wav");

                    data[this.SlapSoundId] = new StardewValley.GameData.AudioCueData
                    {
                        Id = this.SlapSoundId,
                        FilePaths = new List<string> { audioFilePath },
                        Category = "Sound"
                    };
                });
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !this.Config.SlapKey.JustPressed())
                return;

            if (Game1.player.CurrentTool is FishingRod rod && rod.fishCaught)
                this.SlapTheFish();
        }

        private void SlapTheFish()
        {
            Game1.playSound(this.SlapSoundId);
            Game1.player.jump(4f);

            this.slapTick = 8;

            Vector2 impactPos = Game1.player.Position + new Vector2(-16f, -64f);
            this.SpawnBurstParticles(impactPos);
        }

        private void SpawnBurstParticles(Vector2 impactWorldPos)
        {
            if (this.pixelTexture == null)
            {
                this.pixelTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                this.pixelTexture.SetData(new[] { Color.White });
            }

            this.burstParticles.Clear();

            Vector2 center = impactWorldPos + new Vector2(12f, 12f);
            Color[] palette = { Color.Yellow, Color.Orange, Color.White, new Color(255, 255, 100) };

            int sparkCount = this.rng.Next(12, 18);
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = (float)(this.rng.NextDouble() * MathHelper.TwoPi);
                float speed = 3f + (float)(this.rng.NextDouble() * 7f);
                float size = 3f + (float)(this.rng.NextDouble() * 3.5f);
                this.burstParticles.Add(new BurstParticle
                {
                    WorldPos = center + new Vector2(
                        (float)(this.rng.NextDouble() * 6 - 3),
                        (float)(this.rng.NextDouble() * 6 - 3)),
                    Velocity = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed),
                    Alpha = 1f,
                    AlphaDecay = 0.013f + (float)(this.rng.NextDouble() * 0.007f),
                    Width = size,
                    Height = size,
                    Rotation = (float)(this.rng.NextDouble() * MathHelper.TwoPi),
                    RotationSpeed = (float)(this.rng.NextDouble() * 0.3f - 0.15f),
                    Color = palette[this.rng.Next(palette.Length)]
                });
            }

            int lineCount = this.rng.Next(6, 10);
            for (int i = 0; i < lineCount; i++)
            {
                float angle = (float)(this.rng.NextDouble() * MathHelper.TwoPi);
                float dist = 10f + (float)(this.rng.NextDouble() * 8f);
                this.burstParticles.Add(new BurstParticle
                {
                    WorldPos = center + new Vector2(MathF.Cos(angle) * dist, MathF.Sin(angle) * dist),
                    Velocity = new Vector2(MathF.Cos(angle) * 2.4f, MathF.Sin(angle) * 2.4f),
                    Alpha = 0.9f,
                    AlphaDecay = 0.01f,
                    Width = 14f + (float)(this.rng.NextDouble() * 10f),
                    Height = 3f + (float)(this.rng.NextDouble() * 1.5f),
                    Rotation = angle,
                    RotationSpeed = 0f,
                    Color = palette[this.rng.Next(palette.Length)]
                });
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (this.slapTick < 0 && this.burstParticles.Count == 0)
                return;

            if (this.slapTick >= 0)
            {
                this.slapTick++;
                if (this.slapTick > SlapDurationTicks)
                    this.slapTick = -1;
            }

            foreach (var particle in this.burstParticles)
            {
                particle.WorldPos += particle.Velocity;
                particle.Velocity *= 0.96f;
                particle.Alpha -= particle.AlphaDecay;
                particle.Rotation += particle.RotationSpeed;
            }

            this.burstParticles.RemoveAll(p => p.Alpha <= 0f);
        }

        // 在世界绘制前的最后一刻覆盖出拳帧
        private void OnRenderingWorld(object? sender, RenderingWorldEventArgs e)
        {
            if (this.slapTick < 0)
                return;

            hideCaughtFishPreview = true;
            Game1.player.FarmerSprite.setCurrentFrame(SlapPunchFrame);
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            hideCaughtFishPreview = false;

            if (this.pixelTexture == null)
                return;

            foreach (var particle in this.burstParticles)
            {
                Vector2 particleScreen = Game1.GlobalToLocal(Game1.viewport, particle.WorldPos);
                e.SpriteBatch.Draw(
                    this.pixelTexture,
                    particleScreen,
                    sourceRectangle: null,
                    color: particle.Color * particle.Alpha,
                    rotation: particle.Rotation,
                    origin: new Vector2(0.5f, 0.5f),
                    scale: new Vector2(particle.Width, particle.Height),
                    effects: SpriteEffects.None,
                    layerDepth: 1f
                );
            }
        }

        private static bool PrefixGame1DrawTool(Farmer __0)
        {
            if (!hideCaughtFishPreview)
                return true;

            if (__0 != Game1.player || __0.CurrentTool is not FishingRod rod || !rod.fishCaught)
                return true;

            if (Game1.spriteBatch is null)
                return true;

            rod.draw(Game1.spriteBatch);
            return false;
        }
    }
}
