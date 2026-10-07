namespace AuraKnight.Progression
{
    /// <summary>The player talked to the shop keeper: the UI opens the shop with this line of dialogue (a localization key, may be empty).</summary>
    public readonly struct ShopRequested
    {
        public readonly string DialogueKey;

        public ShopRequested(string dialogueKey) { DialogueKey = dialogueKey; }
    }
}
