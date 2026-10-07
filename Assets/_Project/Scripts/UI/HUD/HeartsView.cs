using System.Collections.Generic;
using AuraKnight.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>Row of heart icons: full hearts in aura/fire red, empty ones muted. Listens to HeartsChanged.</summary>
    public sealed class HeartsView : MonoBehaviour
    {
        [SerializeField] RectTransform container;
        [SerializeField] Sprite heartSprite;
        [SerializeField] float slotSize = 64f;
        [SerializeField] float spacing = 8f;

        readonly List<Image> slots = new();

        public int Current { get; private set; }
        public int Max { get; private set; }
        public int VisibleSlots
        {
            get
            {
                int n = 0;
                foreach (var s in slots) if (s.gameObject.activeSelf) n++;
                return n;
            }
        }

        public void Bind(RectTransform slotParent, Sprite heart, float size = 64f, float gap = 8f)
        {
            container = slotParent;
            heartSprite = heart;
            slotSize = size;
            spacing = gap;
        }

        void Awake()
        {
            if (container == null) return;
            foreach (Transform child in container)
                if (child.TryGetComponent<Image>(out var slot)) slots.Add(slot); // slots baked into the prefab
        }

        void OnEnable()
        {
            EventBus.Subscribe<HeartsChanged>(OnHearts);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;
            Set(state != null ? state.maxHearts : 5, state != null ? state.maxHearts : 5);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<HeartsChanged>(OnHearts);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnHearts(HeartsChanged e) => Set(e.Current, e.Max);

        void OnStateLoaded(GameStateLoaded e)
        {
            var state = GameManager.Instance != null ? GameManager.Instance.State : null;
            if (state != null) Set(state.maxHearts, state.maxHearts);
        }

        /// <summary>Editor/build time: bakes the slots so the prefab shows full hearts before the game runs.</summary>
        public void Preview(int count)
        {
            slots.Clear();
            Set(count, count);
        }

        void Set(int current, int max)
        {
            max = Mathf.Max(0, max);
            Current = Mathf.Clamp(current, 0, max);
            Max = max;
            while (slots.Count < max) slots.Add(CreateSlot(slots.Count));
            var theme = UITheme.Active;
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                slot.gameObject.SetActive(i < max);
                var color = theme.GetColor(i < Current ? UIColorToken.Fire : UIColorToken.TextMuted);
                color.a = i < Current ? 1f : 0.35f;
                slot.color = color;
            }
        }

        Image CreateSlot(int index)
        {
            var rect = UiFactory.Place(UiFactory.Rect(container, $"Heart{index + 1}"), new Vector2(0f, 0.5f),
                new Vector2(index * (slotSize + spacing), 0f), new Vector2(slotSize, slotSize));
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = heartSprite;
            image.raycastTarget = false;
            return image;
        }
    }
}
