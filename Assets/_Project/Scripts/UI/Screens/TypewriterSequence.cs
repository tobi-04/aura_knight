using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Timing of the intro cutscene: card after card, each typed out at a fixed speed, then held. A tap completes the
    /// current typing, or (when already complete) moves on. Pure logic, no Unity objects.
    /// </summary>
    public sealed class TypewriterSequence
    {
        readonly int[] lengths;
        readonly float charsPerSecond;
        readonly float holdSeconds;
        float elapsed;
        float held;

        public TypewriterSequence(int[] cardLengths, float charsPerSecond = 40f, float holdSeconds = 1.6f)
        {
            lengths = cardLengths ?? new int[0];
            this.charsPerSecond = charsPerSecond;
            this.holdSeconds = holdSeconds;
            Finished = lengths.Length == 0;
        }

        public int Index { get; private set; }
        public bool Finished { get; private set; }
        public int CardCount => lengths.Length;
        public bool TypingDone => Finished || VisibleChars >= lengths[Index];
        public int VisibleChars => Finished ? 0 : UITween.Typewriter(elapsed, charsPerSecond, lengths[Index]);

        public void Tick(float dt)
        {
            if (Finished) return;
            elapsed += dt;
            if (!TypingDone) return;
            held += dt;
            if (held >= holdSeconds) Advance();
        }

        /// <summary>Tap: finish the typing now, or advance to the next card when it is already complete.</summary>
        public void Tap()
        {
            if (Finished) return;
            if (!TypingDone)
            {
                elapsed = lengths[Index] / Mathf.Max(0.01f, charsPerSecond) + 0.001f;
                return;
            }
            Advance();
        }

        public void Skip() => Finished = true;

        void Advance()
        {
            Index++;
            elapsed = 0f;
            held = 0f;
            if (Index >= lengths.Length) Finished = true;
        }
    }
}
