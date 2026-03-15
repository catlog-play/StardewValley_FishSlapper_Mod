using System;
using System.Collections.Generic;
using System.IO;
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

        private string SlapSoundId = "iwyxdxl.FishSlapper_SlapSound";

        // 巴掌动画状态（手动绘制，避免被鱼遮挡）
        private Texture2D? slapTexture;
        private bool showSlap;
        private float slapAlpha;
        private Vector2 slapWorldPos;

        // 爆破粒子系统
        private Texture2D? pixelTexture;
        private readonly List<BurstParticle> burstParticles = new();
        private readonly Random rng = new();

        private class BurstParticle
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

            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Content.AssetRequested += this.OnAssetRequested;
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

        // 将 slap.wav 注册为游戏内部音效
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            // 拦截游戏加载音频数据的时机
            if (e.NameWithoutLocale.IsEquivalentTo("Data/AudioChanges"))
            {
                e.Edit(asset =>
                {
                    // 获取游戏的音频字典
                    var data = asset.AsDictionary<string, StardewValley.GameData.AudioCueData>().Data;
                    
                    // 获取 slap.wav 在电脑里的绝对路径
                    string audioFilePath = Path.Combine(this.Helper.DirectoryPath, "assets", "slap.wav");

                    // 注册这段新音频
                    data[SlapSoundId] = new StardewValley.GameData.AudioCueData
                    {
                        Id = SlapSoundId,
                        FilePaths = new List<string> { audioFilePath },
                        Category = "Sound" // 归类为 Sound，受游戏内"音效"音量滑块控制
                    };
                });
            }
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !this.Config.SlapKey.JustPressed())
                return;

            if (Game1.player.CurrentTool is FishingRod rod && rod.fishCaught)
            {
                SlapTheFish();
            }
        }

        private void SlapTheFish()
        {
            Game1.playSound(SlapSoundId);

            slapWorldPos = Game1.player.Position + new Vector2(-16f, -64f);
            slapAlpha = 1f;
            showSlap = true;
            slapTexture ??= this.Helper.ModContent.Load<Texture2D>("assets/slap_animation.png");

            SpawnBurstParticles();
        }

        private void SpawnBurstParticles()
        {
            if (pixelTexture == null)
            {
                pixelTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
                pixelTexture.SetData(new[] { Color.White });
            }

            burstParticles.Clear();

            // 粒子中心 = 巴掌纹理的视觉中心（32×32 源图 × 1.5 缩放，中心偏移 24）
            // 如果粒子整体偏左上/右下，调整这里的 24f
            Vector2 center = slapWorldPos + new Vector2(24f, 24f);

            Color[] palette = { Color.Yellow, Color.Orange, Color.White, new Color(255, 255, 100) };

            // ── 火花：向四周飞散的小方块 ──
            int sparkCount = rng.Next(12, 18);       // 火花数量
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float speed = 3f + (float)(rng.NextDouble() * 7f);        // 飞散速度（×2）
                float size = 3f + (float)(rng.NextDouble() * 3.5f);      // 火花大小（正方形边长）
                burstParticles.Add(new BurstParticle
                {
                    WorldPos = center + new Vector2(
                        (float)(rng.NextDouble() * 6 - 3),
                        (float)(rng.NextDouble() * 6 - 3)),   // 初始随机偏移
                    Velocity = new Vector2(
                        MathF.Cos(angle) * speed,
                        MathF.Sin(angle) * speed),
                    Alpha = 1f,
                    AlphaDecay = 0.013f + (float)(rng.NextDouble() * 0.007f), // 淡出速度（原 0.04~0.06，÷3）
                    Width = size,
                    Height = size,
                    Rotation = (float)(rng.NextDouble() * MathHelper.TwoPi),
                    RotationSpeed = (float)(rng.NextDouble() * 0.3f - 0.15f),
                    Color = palette[rng.Next(palette.Length)]
                });
            }

            // ── 冲击线：从中心向外辐射的细长矩形 ──
            int lineCount = rng.Next(6, 10);          // 冲击线数量
            for (int i = 0; i < lineCount; i++)
            {
                float angle = (float)(rng.NextDouble() * MathHelper.TwoPi);
                float dist = 10f + (float)(rng.NextDouble() * 8f);       // 离中心的初始距离
                burstParticles.Add(new BurstParticle
                {
                    WorldPos = center + new Vector2(
                        MathF.Cos(angle) * dist,
                        MathF.Sin(angle) * dist),
                    Velocity = new Vector2(
                        MathF.Cos(angle) * 2.4f,
                        MathF.Sin(angle) * 2.4f),             // 向外扩散速度（×2）
                    Alpha = 0.9f,
                    AlphaDecay = 0.01f,                        // 淡出速度（原 0.03，÷3）
                    Width = 14f + (float)(rng.NextDouble() * 10f),  // 线长度
                    Height = 3f + (float)(rng.NextDouble() * 1.5f), // 线粗细
                    Rotation = angle,
                    RotationSpeed = 0f,
                    Color = palette[rng.Next(palette.Length)]
                });
            }
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!showSlap && burstParticles.Count == 0) return;

            if (showSlap)
            {
                slapAlpha -= 0.01f;                                      // 巴掌淡出速度（原 0.03）
                if (slapAlpha <= 0f)
                    showSlap = false;
            }

            foreach (var p in burstParticles)
            {
                p.WorldPos += p.Velocity;
                p.Velocity *= 0.96f;
                p.Alpha -= p.AlphaDecay;
                p.Rotation += p.RotationSpeed;
            }
            burstParticles.RemoveAll(p => p.Alpha <= 0f);
        }

        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (showSlap && slapTexture != null)
            {
                Vector2 slapScreen = Game1.GlobalToLocal(Game1.viewport, slapWorldPos);
                e.SpriteBatch.Draw(
                    slapTexture,
                    slapScreen,
                    new Rectangle(0, 0, 32, 32),
                    Color.White * slapAlpha,
                    0f,
                    Vector2.Zero,
                    1.5f,
                    SpriteEffects.None,
                    1f
                );
            }

            if (pixelTexture != null)
            {
                foreach (var p in burstParticles)
                {
                    Vector2 particleScreen = Game1.GlobalToLocal(Game1.viewport, p.WorldPos);
                    e.SpriteBatch.Draw(
                        pixelTexture,
                        particleScreen,
                        null,
                        p.Color * p.Alpha,
                        p.Rotation,
                        new Vector2(0.5f, 0.5f),
                        new Vector2(p.Width, p.Height),
                        SpriteEffects.None,
                        1f
                    );
                }
            }
        }
    }
}
