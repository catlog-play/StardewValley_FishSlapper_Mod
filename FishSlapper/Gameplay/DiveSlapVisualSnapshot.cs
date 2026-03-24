using Microsoft.Xna.Framework;

namespace FishSlapper.Gameplay
{
    internal sealed class DiveSlapVisualSnapshot : IDiveSlapRenderState
    {
        public long OwnerPlayerId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public Vector2 OriginalPlayerPosition { get; set; }
        public Vector2 TargetBobberPosition { get; set; }
        public int CastFacingDirection { get; set; }
        public bool FacingRight { get; set; }
        public int RequiredHits { get; set; }
        public int TotalSlapTicks { get; set; }
        public int RemainingSlapTicks { get; set; }
        public string TargetFishQualifiedItemId { get; set; } = string.Empty;
        public string TargetFishDisplayName { get; set; } = "???";
        public Vector2 SlapFishSurfacePosition { get; set; }
        public Vector2 FailRetaliationStartPosition { get; set; }
        public Vector2 FailRetaliationImpactPosition { get; set; }
        public Vector2 FailRetaliationExitPosition { get; set; }
        public float FailRetaliationArcHeight { get; set; }
        public DiveSlapState State { get; set; } = DiveSlapState.None;
        public Vector2 RenderPosition { get; set; }
        public Vector2 PhaseStartPosition { get; set; }
        public Vector2 PhaseTargetPosition { get; set; }
        public int PhaseTicksRemaining { get; set; }
        public int PhaseDurationTicks { get; set; }
        public int CurrentHits { get; set; }
        public int SlapAnimationTicksRemaining { get; set; }
        public bool OutcomeApplied { get; set; }
        public float SlapFishOffsetX { get; set; }
        public float SlapFishOffsetY { get; set; }
        public float SlapFishRotation { get; set; }
        public float SlapFishVelocityX { get; set; }
        public float SlapFishVelocityY { get; set; }
        public float SlapFishRotationVelocity { get; set; }

        // Snapshot 和渲染接口保持一一对应；新增联机渲染字段时，扩接口即可强制同步更新这里。
        public static DiveSlapVisualSnapshot FromState(IDiveSlapRenderState state)
        {
            return new DiveSlapVisualSnapshot
            {
                OwnerPlayerId = state.OwnerPlayerId,
                LocationName = state.LocationName,
                OriginalPlayerPosition = state.OriginalPlayerPosition,
                TargetBobberPosition = state.TargetBobberPosition,
                CastFacingDirection = state.CastFacingDirection,
                FacingRight = state.FacingRight,
                RequiredHits = state.RequiredHits,
                TotalSlapTicks = state.TotalSlapTicks,
                RemainingSlapTicks = state.RemainingSlapTicks,
                TargetFishQualifiedItemId = state.TargetFishQualifiedItemId,
                TargetFishDisplayName = state.TargetFishDisplayName,
                SlapFishSurfacePosition = state.SlapFishSurfacePosition,
                FailRetaliationStartPosition = state.FailRetaliationStartPosition,
                FailRetaliationImpactPosition = state.FailRetaliationImpactPosition,
                FailRetaliationExitPosition = state.FailRetaliationExitPosition,
                FailRetaliationArcHeight = state.FailRetaliationArcHeight,
                State = state.State,
                RenderPosition = state.RenderPosition,
                PhaseStartPosition = state.PhaseStartPosition,
                PhaseTargetPosition = state.PhaseTargetPosition,
                PhaseTicksRemaining = state.PhaseTicksRemaining,
                PhaseDurationTicks = state.PhaseDurationTicks,
                CurrentHits = state.CurrentHits,
                SlapAnimationTicksRemaining = state.SlapAnimationTicksRemaining,
                OutcomeApplied = state.OutcomeApplied,
                SlapFishOffsetX = state.SlapFishOffsetX,
                SlapFishOffsetY = state.SlapFishOffsetY,
                SlapFishRotation = state.SlapFishRotation,
                SlapFishVelocityX = state.SlapFishVelocityX,
                SlapFishVelocityY = state.SlapFishVelocityY,
                SlapFishRotationVelocity = state.SlapFishRotationVelocity
            };
        }
    }
}
