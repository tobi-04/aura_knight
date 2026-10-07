using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>
    /// Assembles the touch HUD layout of GDD 3.1 (dynamic stick left, swipe zone right, round buttons
    /// bottom-right, MAP/PAUSE top-right). Used by the editor generator to bake VirtualControls.prefab.
    /// The returned root is inactive so on-screen controls do not register devices while being assembled.
    /// Labels are themed TMP texts (localized, mono); the Aura buttons carry an <see cref="AuraButtonView"/> (colour, padlock)
    /// under an <see cref="AuraRingView"/>; <see cref="VirtualControlsStyler"/> applies the size/opacity settings.
    /// The canvas keeps a 960x540 reference on purpose: one canvas unit is about one dp, which the 64 dp rules rely on.
    /// </summary>
    public static class VirtualControlsBuilder
    {
        const float MinTouchDp = 64f;
        const float JumpDp = 96f;
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.35f);

        /// <summary>Legacy signature (Player asset generator): the font is ignored, labels use the UI theme's TMP fonts.</summary>
        public static GameObject Build(Sprite circle, Font font) => Build(circle);

        public static GameObject Build(Sprite circle)
        {
            var root = new GameObject("VirtualControls", typeof(RectTransform));
            root.SetActive(false);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f); // canvas unit ~ 1 dp on a typical phone
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();
            root.AddComponent<CanvasGroup>();

            var safe = Stretch(Child(root.transform, "SafeArea"), Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildJoystick(safe, circle);
            BuildSwipeZone(safe);

            AddButton(safe, "Jump", "ctl.jump", VirtualControlPaths.Jump, BottomRight(JumpDp, -28f, 28f), circle);
            AddButton(safe, "Attack", "ctl.attack", VirtualControlPaths.Attack, BottomRight(72f, -140f, 80f), circle);
            AddButton(safe, "Dash", "ctl.dash", VirtualControlPaths.Dash, BottomRight(72f, -224f, 28f), circle);
            AddButton(safe, "Skill", "ctl.skill", VirtualControlPaths.Skill, BottomRight(MinTouchDp, -236f, 112f), circle);
            var ring = new[]
            {
                AddAuraButton(safe, "AuraWind", "Wind", VirtualControlPaths.AuraWind, BottomRight(MinTouchDp, -28f, 140f), circle),
                AddAuraButton(safe, "AuraFire", "Fire", VirtualControlPaths.AuraFire, BottomRight(MinTouchDp, -28f, 212f), circle),
                AddAuraButton(safe, "AuraWater", "Water", VirtualControlPaths.AuraWater, BottomRight(MinTouchDp, -100f, 176f), circle)
            };
            root.AddComponent<AuraRingView>().Bind(ring);
            root.AddComponent<VirtualControlsStyler>();
            AddButton(safe, "Pause", "ctl.pause", VirtualControlPaths.Pause, TopRight(MinTouchDp, -16f, -16f), circle);
            AddButton(safe, "Map", "ctl.map", VirtualControlPaths.Map, TopRight(MinTouchDp, -88f, -16f), circle);
            return root;
        }

        /// <summary>EventSystem wired for the Input System UI module (pointer events drive the on-screen controls).</summary>
        public static GameObject CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return go;
        }

        static void BuildJoystick(RectTransform parent, Sprite circle)
        {
            var zone = Stretch(Child(parent, "JoystickZone"), Vector2.zero, new Vector2(0.5f, 1f));
            var hit = zone.gameObject.AddComponent<Image>();
            hit.color = Color.clear; // invisible but still raycast-able
            var ring = Child(zone, "Ring");
            ring.sizeDelta = new Vector2(160f, 160f);
            var ringImage = ring.gameObject.AddComponent<Image>();
            ringImage.sprite = circle;
            ringImage.color = new Color(1f, 1f, 1f, 0.25f);
            ringImage.raycastTarget = false;
            var handle = Child(ring, "Handle");
            handle.sizeDelta = new Vector2(72f, 72f);
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.sprite = circle;
            handleImage.color = new Color(1f, 1f, 1f, 0.6f);
            handleImage.raycastTarget = false;
            zone.gameObject.AddComponent<DynamicJoystick>().Configure(zone, ring, handle);
        }

        static void BuildSwipeZone(RectTransform parent)
        {
            var zone = Stretch(Child(parent, "SwipeZone"), new Vector2(0.5f, 0f), Vector2.one);
            zone.gameObject.AddComponent<Image>().color = Color.clear;
            zone.gameObject.AddComponent<SwipeDetector>();
        }

        static RectTransform AddButton(RectTransform parent, string name, string labelKey, string path,
            (Vector2 anchor, float size, Vector2 position) layout, Sprite circle)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = layout.anchor;
            rect.sizeDelta = new Vector2(layout.size, layout.size);
            rect.anchoredPosition = layout.position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circle;
            image.color = ButtonColor;
            rect.gameObject.AddComponent<OnScreenButton>().controlPath = path;
            rect.gameObject.AddComponent<PressScale>();

            var label = UiFactory.Text(rect, "Label", labelKey, UIFontRole.Mono, UIColorToken.TextPrimary, 16f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one);
            return rect;
        }

        static AuraButtonView AddAuraButton(RectTransform parent, string name, string auraId, string path,
            (Vector2 anchor, float size, Vector2 position) layout, Sprite circle)
        {
            var rect = AddButton(parent, name, "ctl.aura_" + auraId.ToLowerInvariant(), path, layout, circle);
            var view = rect.gameObject.AddComponent<AuraButtonView>();
            view.Bind(auraId, rect.GetComponent<Image>(), rect.Find("Label").GetComponent<TMP_Text>(), BuildPadlock(rect));
            return view;
        }

        /// <summary>Pixel padlock from plain rects (no sprite needed): shackle (3 bars) above a body block.</summary>
        static GameObject BuildPadlock(RectTransform parent)
        {
            var root = Child(parent, "Padlock");
            Stretch(root, Vector2.zero, Vector2.one);
            Block(root, "Body", new Vector2(0f, -6f), new Vector2(26f, 20f));
            Block(root, "ShackleTop", new Vector2(0f, 10f), new Vector2(18f, 4f));
            Block(root, "ShackleLeft", new Vector2(-7f, 4f), new Vector2(4f, 14f));
            Block(root, "ShackleRight", new Vector2(7f, 4f), new Vector2(4f, 14f));
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static void Block(RectTransform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            var image = rect.gameObject.AddComponent<Image>();
            image.raycastTarget = false;
            rect.gameObject.AddComponent<ThemedImage>().Configure(UIColorToken.TextPrimary);
        }

        static (Vector2, float, Vector2) BottomRight(float size, float x, float y) => (new Vector2(1f, 0f), size, new Vector2(x, y));
        static (Vector2, float, Vector2) TopRight(float size, float x, float y) => (new Vector2(1f, 1f), size, new Vector2(x, y));

        static RectTransform Child(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static RectTransform Stretch(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }
    }
}
