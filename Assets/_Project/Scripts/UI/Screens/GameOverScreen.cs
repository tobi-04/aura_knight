namespace AuraKnight.UI
{
    /// <summary>
    /// "ÁNH SÁNG LỤI TẮT" overlay. Shown on PlayerDied and hidden on PlayerRespawned by <see cref="GameUiDirector"/>;
    /// respawn at the altar is automatic (CheckpointService), so the "HỒI SINH TẠI BÀN THỜ" line is a status, not a button.
    /// </summary>
    public sealed class GameOverScreen : UIScreen
    {
    }
}
