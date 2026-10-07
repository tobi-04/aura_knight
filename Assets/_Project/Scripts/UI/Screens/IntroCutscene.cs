using System;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Slide-3 intro: four numbered cards typed out one after another. Tap completes the typing / advances,
    /// BỎ QUA skips everything. <see cref="Completed"/> fires once, whether finished or skipped.
    /// </summary>
    public sealed class IntroCutscene : UIScreen
    {
        const string KeyPrefix = "intro.";

        [SerializeField] CanvasGroup[] cards = new CanvasGroup[0];
        [SerializeField] ThemedText[] bodies = new ThemedText[0];
        [SerializeField] UIButton skipButton;
        [SerializeField] Button advanceArea;

        TypewriterSequence sequence;
        bool completed;

        public event Action Completed;

        public int ActiveCard => sequence?.Index ?? 0;
        public bool IsFinished => completed;

        public void Bind(CanvasGroup[] cardGroups, ThemedText[] bodyTexts, UIButton skip, Button advance)
        {
            cards = cardGroups;
            bodies = bodyTexts;
            skipButton = skip;
            advanceArea = advance;
        }

        protected override void Awake()
        {
            base.Awake();
            skipButton.onClick.AddListener(Skip);
            advanceArea.onClick.AddListener(Tap);
        }

        protected override void OnShowing()
        {
            completed = false;
            var lengths = new int[bodies.Length];
            for (int i = 0; i < bodies.Length; i++)
            {
                bodies[i].SetText(Localization.Get(KeyPrefix + (i + 1)));
                bodies[i].Text.ForceMeshUpdate();
                lengths[i] = bodies[i].Text.textInfo.characterCount;
                bodies[i].Text.maxVisibleCharacters = 0;
                cards[i].alpha = i == 0 ? 1f : 0f;
            }
            sequence = new TypewriterSequence(lengths);
        }

        protected override void Update()
        {
            base.Update();
            if (sequence == null || completed || !IsVisible) return;
            sequence.Tick(Time.unscaledDeltaTime);
            Paint();
            if (sequence.Finished) Complete();
        }

        public void Tap()
        {
            if (sequence == null) return;
            sequence.Tap();
            Paint();
            if (sequence.Finished) Complete();
        }

        public void Skip()
        {
            sequence?.Skip();
            Complete();
        }

        void Paint()
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                bool reached = sequence.Finished || i <= sequence.Index;
                cards[i].alpha = reached ? 1f : 0f;
                bodies[i].Text.maxVisibleCharacters = sequence.Finished || i < sequence.Index ? int.MaxValue
                    : i == sequence.Index ? sequence.VisibleChars : 0;
            }
        }

        void Complete()
        {
            if (completed) return;
            completed = true;
            Completed?.Invoke();
        }

        public override bool HandleBack()
        {
            Skip();
            return true;
        }
    }
}
