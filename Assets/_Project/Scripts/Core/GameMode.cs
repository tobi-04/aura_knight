namespace AuraKnight.Core
{
    public enum GameMode
    {
        Menu,
        Playing,
        Paused,
        Cutscene,
        /// <summary>The world is being loaded and the player placed (new game / continue); no control yet.</summary>
        Loading
    }
}
