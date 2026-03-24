namespace FishSlapper.Gameplay
{
    internal sealed class CaughtFishSlapVisualData
    {
        public long OwnerPlayerId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public int FacingDirection { get; set; }
        public string FishQualifiedItemId { get; set; } = string.Empty;
        public string FishDisplayName { get; set; } = "???";
        public int NumberOfFishCaught { get; set; } = 1;
        public int FishSize { get; set; } = -1;
        public bool BossFish { get; set; }
        public bool RecordSize { get; set; }
    }
}
