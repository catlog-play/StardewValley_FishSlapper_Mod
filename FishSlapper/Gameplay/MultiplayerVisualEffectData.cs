using Microsoft.Xna.Framework;

namespace FishSlapper.Gameplay
{
    internal sealed class MultiplayerVisualEffectData
    {
        public long OwnerPlayerId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public MultiplayerVisualEffectKind EffectKind { get; set; }
        public Vector2 WorldPosition { get; set; }
    }
}
