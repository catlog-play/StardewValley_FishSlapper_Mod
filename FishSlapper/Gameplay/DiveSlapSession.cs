using Microsoft.Xna.Framework;
using StardewValley.Menus;
using StardewValley.Tools;

namespace FishSlapper.Gameplay
{
    internal sealed class DiveSlapSession
    {
        public FishingRod Rod { get; set; } = null!;
        public BobberBar BobberBar { get; set; } = null!;
        public Vector2 OriginalPlayerPosition { get; set; }
        public Vector2 TargetBobberPosition { get; set; }
        public int CastFacingDirection { get; set; }
        public bool FacingRight { get; set; }
        public int PreviousFacingDirection { get; set; }
        public bool PreviousCanMove { get; set; }
        public int PreviousFreezePause { get; set; }
        public int RequiredHits { get; set; }
        public int TotalSlapTicks { get; set; }
        public int RemainingSlapTicks { get; set; }
        public string TargetFishQualifiedItemId { get; set; } = string.Empty;
        public string TargetFishDisplayName { get; set; } = "???";
        public Vector2 SlapFishSurfacePosition { get; set; }
        public float SlapFishOffsetX { get; set; }
        public float SlapFishOffsetY { get; set; }
        public float SlapFishRotation { get; set; }
        public float SlapFishVelocityX { get; set; }
        public float SlapFishVelocityY { get; set; }
        public float SlapFishRotationVelocity { get; set; }
        public int SlapFishBouncesRemaining { get; set; }
        public Vector2 FailRetaliationStartPosition { get; set; }
        public Vector2 FailRetaliationImpactPosition { get; set; }
        public Vector2 FailRetaliationExitPosition { get; set; }
        public float FailRetaliationArcHeight { get; set; }
        public bool FailRetaliationImpactTriggered { get; set; }

        public DiveSlapState State { get; set; } = DiveSlapState.None;
        public Vector2 RenderPosition { get; set; }
        public Vector2 PhaseStartPosition { get; set; }
        public Vector2 PhaseTargetPosition { get; set; }
        public int PhaseTicksRemaining { get; set; }
        public int PhaseDurationTicks { get; set; }
        public int CurrentHits { get; set; }
        public int SlapAnimationTicksRemaining { get; set; }
        public bool OutcomeApplied { get; set; }
    }
}
