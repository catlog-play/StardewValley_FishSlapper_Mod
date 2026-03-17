using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewValley;
using FishSlapper.Gameplay;

namespace FishSlapper.Rendering
{
    internal sealed class DiveSlapRenderer
    {
        private const int CaughtFishPunchFrame = 278;
        private const int DivePunchRightFrame = 274;
        private const int DivePunchLeftFrame = 278;
        private const int DiveRunDownFrame = 32;
        private const int DiveIdleRightFrame = 8;
        private const int DiveIdleLeftFrame = 24;
        private const int DiveRunRightFrame = 40;
        private const int DiveRunUpFrame = 48;
        private const int DiveRunLeftFrame = 56;
        private const int StandDownFrame = 0;
        private const int StandRightFrame = 8;
        private const int StandUpFrame = 16;
        private const int StandLeftFrame = 24;
        private const int CaughtFishSlapDurationTicks = 30;
        private const int DiveHitAnimationDurationTicks = 10;
        private static readonly RasterizerState WaterMaskRasterizerState = new()
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        private readonly List<BurstParticle> burstParticles = new();
        private readonly Random rng = new();
        private Texture2D? pixelTexture;
        private int caughtFishSlapTick = -1;
        private int localPoseResetTicks;
        private int localPoseResetFacingDirection = 2;
        private bool hideCaughtFishPreview;
        private Farmer? diveRenderFarmer;
        private Farmer? toolSuppressedFarmer;

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

        public bool ShouldHideCaughtFishToolPreview => this.hideCaughtFishPreview;

        public bool ShouldSuppressToolDraw(Farmer farmer)
        {
            return ReferenceEquals(farmer, this.toolSuppressedFarmer)
                || (ReferenceEquals(farmer, Game1.player) && this.localPoseResetTicks > 0 && this.caughtFishSlapTick < 0);
        }

        public void ResetLocalPlayerPose(int facingDirection)
        {
            this.localPoseResetFacingDirection = facingDirection;
            this.localPoseResetTicks = 4;
            Game1.player.faceDirection(facingDirection);
            this.ApplyPose(Game1.player, GetStandingFrame(facingDirection));
        }

        public int DiveHitTickDuration => DiveHitAnimationDurationTicks;

        public void PlayCaughtFishSlap()
        {
            Game1.playSound(ModConstants.SlapSoundId);
            Game1.player.jump(4f);
            this.caughtFishSlapTick = 8;
            this.SpawnBurstParticles(Game1.player.Position + new Vector2(-16f, -64f));
        }

        public void PlayDiveSlap(Vector2 impactWorldPos)
        {
            Game1.playSound(ModConstants.SlapSoundId);
            this.SpawnBurstParticles(impactWorldPos);
        }

        public void PlayDiveWaterEntry()
        {
            Game1.playSound(ModConstants.DiveWaterEntrySoundId);
        }

        public void PlayDiveWaterExit()
        {
            Game1.playSound(ModConstants.DiveWaterExitSoundId);
        }

        public void PlayDiveJump()
        {
            Game1.playSound(ModConstants.DiveJumpSoundId);
        }

        public void OnUpdateTicked(DiveSlapSession? session)
        {
            if (this.caughtFishSlapTick >= 0)
            {
                this.caughtFishSlapTick++;
                if (this.caughtFishSlapTick > CaughtFishSlapDurationTicks)
                    this.caughtFishSlapTick = -1;
            }

            if (this.localPoseResetTicks > 0)
                this.localPoseResetTicks--;

            if (session is not null && session.SlapAnimationTicksRemaining > 0)
                session.SlapAnimationTicksRemaining--;

            foreach (var particle in this.burstParticles)
            {
                particle.WorldPos += particle.Velocity;
                particle.Velocity *= 0.96f;
                particle.Alpha -= particle.AlphaDecay;
                particle.Rotation += particle.RotationSpeed;
            }

            this.burstParticles.RemoveAll(p => p.Alpha <= 0f);
        }

        public void OnRenderingWorld(DiveSlapSession? session)
        {
            if (this.caughtFishSlapTick >= 0)
            {
                this.hideCaughtFishPreview = true;
                // 这里故意不清原版“手里举鱼”的状态，只临时覆写一帧出拳姿势。
                // 如果把动画栈整个清掉，会把老玩法里“拿着鱼无限扇”的行为打断。
                Game1.player.FarmerSprite.setCurrentFrame(CaughtFishPunchFrame);
                return;
            }

            this.hideCaughtFishPreview = false;

            if (this.localPoseResetTicks > 0)
                this.ApplyPose(Game1.player, GetStandingFrame(this.localPoseResetFacingDirection));
        }

        public bool TryDrawDiveSession(SpriteBatch spriteBatch, DiveSlapSession? session)
        {
            if (session is null)
            {
                this.diveRenderFarmer = null;
                return false;
            }

            this.DrawDiveSession(spriteBatch, session);
            return true;
        }

        public void OnRenderedWorld(RenderedWorldEventArgs e, DiveSlapSession? session)
        {
            if (session is null)
                this.diveRenderFarmer = null;
            else if (ShouldDrawDiveWaterOverlay(session))
                this.DrawDiveWaterOverlay(e.SpriteBatch, session);

            if (this.pixelTexture is not null)
            {
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

            this.hideCaughtFishPreview = false;
        }

        public void OnRenderedActiveMenu(RenderedActiveMenuEventArgs e, DiveSlapSession? session)
        {
            if (session is null)
                return;

            string title = $"Dive slap {session.CurrentHits}/{session.RequiredHits}";
            string detail = session.State switch
            {
                DiveSlapState.Windup => "Jumping...",
                DiveSlapState.Diving => "Diving...",
                DiveSlapState.Slapping => $"Time {session.RemainingSlapTicks / 60f:0.0}s",
                DiveSlapState.ResolveSuccess => "Caught!",
                DiveSlapState.ResolveFail => "Escaped!",
                DiveSlapState.Returning => "Returning...",
                _ => string.Empty
            };

            Utility.drawTextWithShadow(e.SpriteBatch, title, Game1.dialogueFont, new Vector2(64f, 64f), Game1.textColor);
            if (detail.Length > 0)
                Utility.drawTextWithShadow(e.SpriteBatch, detail, Game1.smallFont, new Vector2(64f, 108f), Game1.textColor);
        }

        private void DrawDiveSession(SpriteBatch spriteBatch, DiveSlapSession session)
        {
            Farmer renderFarmer = this.PrepareDiveRenderFarmer(session);
            this.toolSuppressedFarmer = renderFarmer;
            try
            {
                renderFarmer.draw(spriteBatch);
            }
            finally
            {
                this.toolSuppressedFarmer = null;
            }
        }

        private Farmer PrepareDiveRenderFarmer(DiveSlapSession session)
        {
            this.diveRenderFarmer ??= Game1.player.CreateFakeEventFarmer();

            Farmer renderFarmer = this.diveRenderFarmer;
            // 跳水时不真的移动玩家本体，而是把原版 farmer.draw 替换成这只 fake farmer。
            // 这样能吃到原版的环境着色、图层和农夫外观，但不会干扰玩家真实位置和碰撞。
            renderFarmer.currentLocation = Game1.currentLocation;
            renderFarmer.Position = session.RenderPosition;
            renderFarmer.faceDirection(GetDiveFacingDirection(session));
            renderFarmer.UsingTool = false;
            renderFarmer.canReleaseTool = false;

            int frame = GetDiveFrame(session);
            this.ApplyPose(renderFarmer, frame);
            return renderFarmer;
        }

        private void ApplyPose(Farmer farmer, int frame)
        {
            // fake farmer 可能残留上一帧的动作状态；每次绘制前都先清掉，再强制切到目标帧。
            farmer.completelyStopAnimatingOrDoingAction();
            farmer.FarmerSprite.StopAnimation();
            farmer.FarmerSprite.ClearAnimation();
            farmer.FarmerSprite.setCurrentFrame(frame, 0, 0, 1, false, false);
        }

        private static int GetStandingFrame(int facingDirection)
        {
            return facingDirection switch
            {
                0 => StandUpFrame,
                1 => StandRightFrame,
                2 => StandDownFrame,
                _ => StandLeftFrame
            };
        }

        private static int GetDiveFrame(DiveSlapSession session)
        {
            int facingDirection = GetDiveFacingDirection(session);
            return session.State switch
            {
                DiveSlapState.Windup => GetWindupFrame(facingDirection),
                DiveSlapState.Diving => GetTravelFrame(facingDirection),
                DiveSlapState.Returning => GetTravelFrame(facingDirection),
                DiveSlapState.Slapping when session.SlapAnimationTicksRemaining > 0 => session.FacingRight ? DivePunchRightFrame : DivePunchLeftFrame,
                _ => GetIdleFrame(facingDirection)
            };
        }

        private static int GetWindupFrame(int facingDirection)
        {
            return GetTravelFrame(facingDirection);
        }

        private static int GetTravelFrame(int facingDirection)
        {
            return facingDirection switch
            {
                0 => DiveRunUpFrame,
                1 => DiveRunRightFrame,
                2 => DiveRunDownFrame,
                _ => DiveRunLeftFrame
            };
        }

        private static int GetDiveFacingDirection(DiveSlapSession session)
        {
            if (session.State == DiveSlapState.Slapping && session.SlapAnimationTicksRemaining > 0)
                return session.FacingRight ? 1 : 3;

            return session.State == DiveSlapState.Returning
                ? GetOppositeFacingDirection(session.CastFacingDirection)
                : session.CastFacingDirection;
        }

        private static int GetIdleFrame(int facingDirection)
        {
            return facingDirection switch
            {
                0 => StandUpFrame,
                1 => DiveIdleRightFrame,
                2 => StandDownFrame,
                _ => DiveIdleLeftFrame
            };
        }

        private static int GetOppositeFacingDirection(int facingDirection)
        {
            return facingDirection switch
            {
                0 => 2,
                1 => 3,
                2 => 0,
                _ => 1
            };
        }

        private void SpawnBurstParticles(Vector2 impactWorldPos)
        {
            this.EnsurePixelTexture();
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

        private void DrawDiveWaterOverlay(SpriteBatch spriteBatch, DiveSlapSession session)
        {
            GameLocation? location = Game1.currentLocation;
            if (location is null)
                return;

            // 遮罩不再画纯色块，而是回刷当前位置真正的水面 tile，
            // 这样不同地图的水体材质和动效都能保持一致。
            Rectangle worldMask = GetDiveWaterMaskWorldRectangle(session.RenderPosition);
            Rectangle screenMask = GetDiveWaterMaskScreenRectangle(session.RenderPosition);
            GraphicsDevice device = Game1.graphics.GraphicsDevice;
            Rectangle scissor = Rectangle.Intersect(screenMask, device.Viewport.Bounds);
            if (scissor.Width <= 0 || scissor.Height <= 0)
                return;

            int tileLeft = Math.Max(0, worldMask.Left / Game1.tileSize);
            int tileRight = Math.Max(tileLeft, (worldMask.Right - 1) / Game1.tileSize);
            int tileTop = Math.Max(0, worldMask.Top / Game1.tileSize);
            int tileBottom = Math.Max(tileTop, (worldMask.Bottom - 1) / Game1.tileSize);

            Rectangle previousScissor = device.ScissorRectangle;
            RasterizerState previousRasterizerState = device.RasterizerState;

            try
            {
                device.ScissorRectangle = scissor;
                device.RasterizerState = WaterMaskRasterizerState;

                for (int tileY = tileTop; tileY <= tileBottom; tileY++)
                {
                    for (int tileX = tileLeft; tileX <= tileRight; tileX++)
                    {
                        if (location.isWaterTile(tileX, tileY))
                            location.drawWaterTile(spriteBatch, tileX, tileY);
                    }
                }
            }
            finally
            {
                device.ScissorRectangle = previousScissor;
                device.RasterizerState = previousRasterizerState;
            }
        }

        private static Rectangle GetDiveWaterMaskWorldRectangle(Vector2 renderPosition)
        {
            return new Rectangle(
                (int)renderPosition.X - 12,
                (int)renderPosition.Y + 38,
                92,
                26
            );
        }

        private static Rectangle GetDiveWaterMaskScreenRectangle(Vector2 renderPosition)
        {
            Vector2 localPosition = Game1.GlobalToLocal(Game1.viewport, renderPosition);
            return new Rectangle(
                (int)localPosition.X - 12,
                (int)localPosition.Y + 38,
                92,
                26
            );
        }

        private static bool ShouldDrawDiveWaterOverlay(DiveSlapSession session)
        {
            return session.State is DiveSlapState.Slapping or DiveSlapState.ResolveSuccess or DiveSlapState.ResolveFail;
        }

        private void EnsurePixelTexture()
        {
            if (this.pixelTexture != null)
                return;

            this.pixelTexture = new Texture2D(Game1.graphics.GraphicsDevice, 1, 1);
            this.pixelTexture.SetData(new[] { Color.White });
        }
    }
}
