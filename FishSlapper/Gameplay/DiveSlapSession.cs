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
