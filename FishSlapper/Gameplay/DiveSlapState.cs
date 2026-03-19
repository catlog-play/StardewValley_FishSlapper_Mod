namespace FishSlapper.Gameplay
{
    internal enum DiveSlapState
    {
        None,
        Windup,
        Diving,
        Slapping,
        ResolveSuccess,
        ResolveFailPauseBefore,
        ResolveFail,
        ResolveFailPauseAfter,
        Returning
    }
}
