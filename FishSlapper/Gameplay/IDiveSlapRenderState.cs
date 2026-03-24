using Microsoft.Xna.Framework;

namespace FishSlapper.Gameplay
{
    internal interface IDiveSlapRenderState
    {
        long OwnerPlayerId { get; }
        string LocationName { get; }
        Vector2 OriginalPlayerPosition { get; }
        Vector2 TargetBobberPosition { get; }
        int CastFacingDirection { get; }
        bool FacingRight { get; }
        int RequiredHits { get; }
        int TotalSlapTicks { get; }
        int RemainingSlapTicks { get; }
        string TargetFishQualifiedItemId { get; }
        string TargetFishDisplayName { get; }
        Vector2 SlapFishSurfacePosition { get; }
        Vector2 FailRetaliationStartPosition { get; }
        Vector2 FailRetaliationImpactPosition { get; }
        Vector2 FailRetaliationExitPosition { get; }
        float FailRetaliationArcHeight { get; }

        DiveSlapState State { get; }
        Vector2 RenderPosition { get; }
        Vector2 PhaseStartPosition { get; }
        Vector2 PhaseTargetPosition { get; }
        int PhaseTicksRemaining { get; }
        int PhaseDurationTicks { get; }
        int CurrentHits { get; }
        int SlapAnimationTicksRemaining { get; }
        bool OutcomeApplied { get; }

        float SlapFishOffsetX { get; }
        float SlapFishOffsetY { get; }
        float SlapFishRotation { get; }
        float SlapFishVelocityX { get; }
        float SlapFishVelocityY { get; }
        float SlapFishRotationVelocity { get; }
    }
}
