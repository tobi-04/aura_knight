namespace AuraKnight.UI
{
    /// <summary>What <see cref="ScreenStack"/> needs from a screen. <see cref="UIScreen"/> implements it.</summary>
    public interface IRoutedScreen
    {
        void Show(bool instant = false);
        void Hide(bool instant = false);
        /// <summary>The Back key reached this screen on top of the stack. True = handled here (do not pop).</summary>
        bool HandleBack();
    }
}
