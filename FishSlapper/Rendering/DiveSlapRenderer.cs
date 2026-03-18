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
        // 原玩法“鱼拿在手上时扇鱼”用的出拳帧，只影响本体临时渲染。
        private const int CaughtFishPunchFrame = 278;

        // 跳水玩法里真正会用到的动作帧：
        // 1. 水中扇鱼时的左右出拳帧
        // 2. 起跳/飞行/回岸时的四向移动帧
        // 3. 水中待机或强制复位时的四向站立帧
        private const int DiveSlapPunchRightFrame = 274;
        private const int DiveSlapPunchLeftFrame = 278;
        // 跳水位移阶段，当前改用 docs 里的 carryRun 组。
        private const int DiveSlapMoveDownFrame = 128;
        private const int DiveSlapMoveRightFrame = 136;
        private const int DiveSlapMoveUpFrame = 144;
        private const int DiveSlapMoveLeftFrame = 152;
        private const int FarmerStandDownFrame = 0;
        private const int FarmerStandRightFrame = 8;
        private const int FarmerStandUpFrame = 16;
        private const int FarmerStandLeftFrame = 24;
        private const float CaughtFishHeldBaseYOffset = -36f;
        private const float CaughtFishMaxHorizontalTwitchVelocity = 0.8f;
        private const float CaughtFishInitialJumpVelocity = -11.4f;
        private const float CaughtFishBounceJumpVelocity = -4.2f;
        private const int CaughtFishStandResetTicks = 5;

        private const int CaughtFishSlapDurationTicks = 30;
        private const int DiveHitAnimationDurationTicks = 10;

        private const float HudBarWidth = 120f;
        private const float HudHitBarHeight = 11f;
        private const float HudTimeBarHeight = 8f;
        private const float HudBarGap = 3f;
        private const float HudBorderSize = 2f;
        private const float HudAboveFeetOffset = 160f;
        private const float HudSegmentGap = 2f;
        private const float HudTextScale = 1.1f;

        private static readonly Color HudBorderColor = new(40, 30, 20, 230);
        private static readonly Color HudHitFilledColor = new(255, 200, 50);
        private static readonly Color HudHitEmptyColor = new(60, 50, 40, 130);
        private static readonly Color HudTimeBarBgColor = new(30, 25, 20, 150);
        private static readonly Color HudTimeGreenColor = new(80, 220, 80);
        private static readonly Color HudTimeYellowColor = new(240, 220, 50);
        private static readonly Color HudTimeRedColor = new(230, 60, 50);
        private static readonly Color HudTextShadowColor = new(20, 15, 10, 180);

        private readonly List<BurstParticle> burstParticles = new();
        private readonly Random rng = new();
        private Texture2D? pixelTexture;
        private int caughtFishSlapTick = -1;
        private float fishTwitchOffsetX;
        private float fishTwitchOffsetY;
        private float fishTwitchRotation;
        private float fishTwitchRotationVelocity;
        private float fishTwitchVelocityX;
        private float fishTwitchVelocityY;
        private int fishTwitchBouncesRemaining;
        private int caughtFishStandFrameTicksRemaining;
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
            this.caughtFishStandFrameTicksRemaining = this.caughtFishSlapTick >= 0 ? CaughtFishStandResetTicks : 0;
            this.caughtFishSlapTick = 8;
            this.fishTwitchOffsetX = 0f;
            this.fishTwitchOffsetY = 0f;
            this.fishTwitchRotation = 0f;
            this.fishTwitchVelocityX = (float)(this.rng.NextDouble() * (CaughtFishMaxHorizontalTwitchVelocity * 2f) - CaughtFishMaxHorizontalTwitchVelocity);
            this.fishTwitchVelocityY = CaughtFishInitialJumpVelocity;
            this.fishTwitchRotationVelocity = this.fishTwitchVelocityX * 0.065f;
            this.fishTwitchBouncesRemaining = 1;
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

            if (
                this.fishTwitchVelocityX != 0f
                || this.fishTwitchVelocityY != 0f
                || this.fishTwitchOffsetX != 0f
                || this.fishTwitchOffsetY < 0f
                || this.fishTwitchRotation != 0f
                || this.fishTwitchRotationVelocity != 0f
            )
            {
                this.fishTwitchOffsetX += this.fishTwitchVelocityX;
                this.fishTwitchOffsetY += this.fishTwitchVelocityY;
                this.fishTwitchRotation += this.fishTwitchRotationVelocity;
                this.fishTwitchVelocityX *= 0.72f;
                this.fishTwitchVelocityY += 1.1f;
                this.fishTwitchRotationVelocity *= 0.78f;
                if (this.fishTwitchOffsetY >= 0f)
                {
                    this.fishTwitchOffsetY = 0f;
                    if (this.fishTwitchBouncesRemaining > 0)
                    {
                        this.fishTwitchVelocityY = CaughtFishBounceJumpVelocity;
                        this.fishTwitchRotationVelocity = -this.fishTwitchRotationVelocity * 0.6f;
                        this.fishTwitchBouncesRemaining--;
                    }
                    else
                    {
                        this.fishTwitchVelocityY = 0f;
                    }
                }

                if (MathF.Abs(this.fishTwitchOffsetX) < 0.05f && MathF.Abs(this.fishTwitchVelocityX) < 0.05f)
                {
                    this.fishTwitchOffsetX = 0f;
                    this.fishTwitchVelocityX = 0f;
                }

                if (MathF.Abs(this.fishTwitchRotation) < 0.005f && MathF.Abs(this.fishTwitchRotationVelocity) < 0.005f)
                {
                    this.fishTwitchRotation = 0f;
                    this.fishTwitchRotationVelocity = 0f;
                }
            }

            if (this.localPoseResetTicks > 0)
                this.localPoseResetTicks--;

            if (session is not null && session.SlapAnimationTicksRemaining > 0)
                session.SlapAnimationTicksRemaining--;

            foreach (var particle in this.burstParticles)
            {
                particle.WorldPos += particle.Velocity;
                particle.Velocity *= 0.91f;
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
                if (this.caughtFishStandFrameTicksRemaining > 0)
                {
                    this.caughtFishStandFrameTicksRemaining--;
                    Game1.player.FarmerSprite.setCurrentFrame(GetStandingFrame(Game1.player.FacingDirection));
                    return;
                }

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

        public bool TryDrawCaughtFishPreview(SpriteBatch spriteBatch, Farmer farmer, StardewValley.Tools.FishingRod rod)
        {
            if (this.caughtFishSlapTick < 0 || !rod.fishCaught || rod.whichFish is null || rod.whichFish.TypeIdentifier != "(O)")
                return false;

            Farmer drawFarmer = rod.lastUser ?? farmer;
            this.DrawCaughtFishPreview(spriteBatch, drawFarmer, rod);
            return true;
        }

        public void OnRenderedWorld(RenderedWorldEventArgs e, DiveSlapSession? session)
        {
            if (session is null)
                this.diveRenderFarmer = null;

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

            if (session is not null && session.State == DiveSlapState.Slapping)
                this.DrawSlapProgressHud(e.SpriteBatch, session);

            this.hideCaughtFishPreview = false;
        }

        public void OnRenderedActiveMenu(RenderedActiveMenuEventArgs e, DiveSlapSession? session)
        {
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

        private void DrawCaughtFishPreview(SpriteBatch spriteBatch, Farmer farmer, StardewValley.Tools.FishingRod rod)
        {
            if (rod.whichFish is null)
                return;

            float boardBobOffset = 4f * (float)Math.Round(
                Math.Sin(Game1.currentGameTime.TotalGameTime.TotalMilliseconds / 250.0),
                2
            );
            int standingPixelY = farmer.StandingPixel.Y;
            float boardLayerDepth = standingPixelY / 10000f + 0.06f;
            float iconLayerDepth = standingPixelY / 10000f + 0.0601f;
            var fishData = rod.whichFish.GetParsedOrErrorData();
            Texture2D fishTexture = fishData.GetTexture();
            Rectangle fishSourceRect = fishData.GetSourceRect(0, null);

            spriteBatch.Draw(
                Game1.mouseCursors,
                Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-120f, -288f + boardBobOffset)),
                new Rectangle(31, 1870, 73, 49),
                Color.White * 0.8f,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                boardLayerDepth
            );

            spriteBatch.Draw(
                fishTexture,
                Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-80f, -216f + boardBobOffset)),
                fishSourceRect,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                iconLayerDepth
            );

            if (rod.numberOfFishCaught > 1)
            {
                Utility.drawTinyDigits(
                    rod.numberOfFishCaught,
                    spriteBatch,
                    Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-28f, -168f + boardBobOffset)),
                    3f,
                    standingPixelY / 10000f + 0.061f,
                    Color.White
                );
            }

            this.DrawCaughtFishHeldSprite(
                spriteBatch,
                fishTexture,
                fishSourceRect,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    farmer.Position + new Vector2(this.fishTwitchOffsetX, CaughtFishHeldBaseYOffset + this.fishTwitchOffsetY)
                ),
                GetHeldFishBaseRotation(rod) + this.fishTwitchRotation,
                standingPixelY / 10000f + 0.062f
            );

            for (int i = 1; i < rod.numberOfFishCaught; i++)
            {
                float bonusRotation = i == 2 ? MathF.PI : 2.5132742f;
                this.DrawCaughtFishHeldSprite(
                    spriteBatch,
                    fishTexture,
                    fishSourceRect,
                    Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(-12f * i, CaughtFishHeldBaseYOffset)),
                    GetHeldFishBaseRotation(rod) > 0f ? bonusRotation : 0f,
                    standingPixelY / 10000f + 0.058f
                );
            }

            string fishName = fishData.DisplayName ?? "???";
            Vector2 fishNameSize = Game1.smallFont.MeasureString(fishName);
            spriteBatch.DrawString(
                Game1.smallFont,
                fishName,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    farmer.Position + new Vector2(26f - fishNameSize.X / 2f, -278f + boardBobOffset)
                ),
                rod.bossFish ? new Color(126, 61, 237) : Game1.textColor,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                boardLayerDepth
            );

            if (rod.fishSize == -1)
                return;

            string sizeLabel = Game1.content.LoadString(@"Strings\StringsFromCSFiles:FishingRod.cs.14082");
            spriteBatch.DrawString(
                Game1.smallFont,
                sizeLabel,
                Game1.GlobalToLocal(Game1.viewport, farmer.Position + new Vector2(20f, -214f + boardBobOffset)),
                Game1.textColor,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                boardLayerDepth
            );

            double displaySize = LocalizedContentManager.CurrentLanguageCode == LocalizedContentManager.LanguageCode.en
                ? rod.fishSize
                : Math.Round(rod.fishSize * 2.54);
            string sizeText = Game1.content.LoadString(@"Strings\StringsFromCSFiles:FishingRod.cs.14083", displaySize);
            Vector2 sizeTextSize = Game1.smallFont.MeasureString(sizeText);
            spriteBatch.DrawString(
                Game1.smallFont,
                sizeText,
                Game1.GlobalToLocal(
                    Game1.viewport,
                    farmer.Position + new Vector2(85f - sizeTextSize.X / 2f, -179f + boardBobOffset)
                ),
                rod.recordSize ? Color.Blue : Game1.textColor,
                0f,
                Vector2.Zero,
                1f,
                SpriteEffects.None,
                boardLayerDepth
            );
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
            renderFarmer.swimming.Value = false;
            renderFarmer.bathingClothes.Value = false;
            renderFarmer.yOffset = 0f;

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
                0 => FarmerStandUpFrame,
                1 => FarmerStandRightFrame,
                2 => FarmerStandDownFrame,
                _ => FarmerStandLeftFrame
            };
        }

        private static int GetDiveFrame(DiveSlapSession session)
        {
            int facingDirection = GetDiveFacingDirection(session);
            return session.State switch
            {
                DiveSlapState.Windup => GetDiveMoveFrame(facingDirection),
                DiveSlapState.Diving => GetDiveMoveFrame(facingDirection),
                DiveSlapState.Returning => GetDiveMoveFrame(facingDirection),
                DiveSlapState.Slapping when session.SlapAnimationTicksRemaining > 0 => session.FacingRight ? DiveSlapPunchRightFrame : DiveSlapPunchLeftFrame,
                _ => GetDiveIdleFrame(facingDirection)
            };
        }

        private static int GetDiveMoveFrame(int facingDirection)
        {
            return facingDirection switch
            {
                0 => DiveSlapMoveUpFrame,
                1 => DiveSlapMoveRightFrame,
                2 => DiveSlapMoveDownFrame,
                _ => DiveSlapMoveLeftFrame
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

        private static int GetDiveIdleFrame(int facingDirection)
        {
            return GetStandingFrame(facingDirection);
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

        private static float GetHeldFishBaseRotation(StardewValley.Tools.FishingRod rod)
        {
            if (rod.whichFish is null || rod.fishSize == -1)
                return 0f;

            string? itemId = rod.whichFish.QualifiedItemId;
            return itemId is not "(O)800" and not "(O)798" and not "(O)149" and not "(O)151"
                ? 2.3561945f
                : 0f;
        }

        private void DrawCaughtFishHeldSprite(
            SpriteBatch spriteBatch,
            Texture2D fishTexture,
            Rectangle fishSourceRect,
            Vector2 screenPosition,
            float rotation,
            float layerDepth
        )
        {
            spriteBatch.Draw(
                fishTexture,
                screenPosition,
                fishSourceRect,
                Color.White,
                rotation,
                new Vector2(8f, 8f),
                3f,
                SpriteEffects.None,
                layerDepth
            );
        }

        private void DrawSlapProgressHud(SpriteBatch spriteBatch, DiveSlapSession session)
        {
            this.EnsurePixelTexture();
            if (this.pixelTexture is null || session.RequiredHits <= 0)
                return;

            Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, session.RenderPosition);
            float centerX = screenPos.X + 32f;
            float barLeft = centerX - HudBarWidth / 2f;
            float hitBarTop = screenPos.Y - HudAboveFeetOffset;
            float timeBarTop = hitBarTop + HudHitBarHeight + HudBarGap;

            string hitsText = $"{session.CurrentHits}/{session.RequiredHits}";
            Vector2 textSize = Game1.smallFont.MeasureString(hitsText) * HudTextScale;
            Vector2 textPos = new(centerX - textSize.X / 2f, hitBarTop - textSize.Y - 2f);

            spriteBatch.DrawString(Game1.smallFont, hitsText,
                textPos + new Vector2(1f, 1f), HudTextShadowColor,
                0f, Vector2.Zero, HudTextScale, SpriteEffects.None, 1f);
            spriteBatch.DrawString(Game1.smallFont, hitsText,
                textPos, Color.White,
                0f, Vector2.Zero, HudTextScale, SpriteEffects.None, 1f);

            this.DrawHudRect(spriteBatch, barLeft - HudBorderSize, hitBarTop - HudBorderSize,
                HudBarWidth + HudBorderSize * 2f, HudHitBarHeight + HudBorderSize * 2f, HudBorderColor);

            float totalGaps = (session.RequiredHits - 1) * HudSegmentGap;
            float segWidth = (HudBarWidth - totalGaps) / session.RequiredHits;
            for (int i = 0; i < session.RequiredHits; i++)
            {
                float segX = barLeft + i * (segWidth + HudSegmentGap);
                Color segColor = i < session.CurrentHits ? HudHitFilledColor : HudHitEmptyColor;
                this.DrawHudRect(spriteBatch, segX, hitBarTop, segWidth, HudHitBarHeight, segColor);
            }

            this.DrawHudRect(spriteBatch, barLeft - HudBorderSize, timeBarTop - HudBorderSize,
                HudBarWidth + HudBorderSize * 2f, HudTimeBarHeight + HudBorderSize * 2f, HudBorderColor);
            this.DrawHudRect(spriteBatch, barLeft, timeBarTop, HudBarWidth, HudTimeBarHeight, HudTimeBarBgColor);

            float timeFraction = session.TotalSlapTicks > 0
                ? MathHelper.Clamp((float)session.RemainingSlapTicks / session.TotalSlapTicks, 0f, 1f)
                : 0f;
            if (timeFraction > 0f)
            {
                float fillWidth = HudBarWidth * timeFraction;
                Color timeColor = GetTimeBarColor(timeFraction);
                this.DrawHudRect(spriteBatch, barLeft, timeBarTop, fillWidth, HudTimeBarHeight, timeColor);
            }
        }

        private void DrawHudRect(SpriteBatch spriteBatch, float x, float y, float width, float height, Color color)
        {
            spriteBatch.Draw(
                this.pixelTexture!,
                new Vector2(x, y),
                sourceRectangle: null,
                color: color,
                rotation: 0f,
                origin: Vector2.Zero,
                scale: new Vector2(width, height),
                effects: SpriteEffects.None,
                layerDepth: 1f);
        }

        private static Color GetTimeBarColor(float fraction)
        {
            if (fraction > 0.5f)
                return Color.Lerp(HudTimeYellowColor, HudTimeGreenColor, (fraction - 0.5f) * 2f);
            if (fraction > 0.2f)
                return Color.Lerp(HudTimeRedColor, HudTimeYellowColor, (fraction - 0.2f) / 0.3f);
            return HudTimeRedColor;
        }

        private void SpawnBurstParticles(Vector2 impactWorldPos)
        {
            this.EnsurePixelTexture();

            Vector2 center = impactWorldPos + new Vector2(12f, 0f);
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
                    AlphaDecay = 0.03f + (float)(this.rng.NextDouble() * 0.008f),
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
                    AlphaDecay = 0.018f,
                    Width = 7f + (float)(this.rng.NextDouble() * 5f),
                    Height = 1.5f + (float)(this.rng.NextDouble() * 0.75f),
                    Rotation = angle,
                    RotationSpeed = 0f,
                    Color = palette[this.rng.Next(palette.Length)]
                });
            }
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
