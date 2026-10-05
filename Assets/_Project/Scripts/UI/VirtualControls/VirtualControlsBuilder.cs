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
    /// </summary>
    public static class VirtualControlsBuilder
    {
        const float MinTouchDp = 64f;
        const float JumpDp = 96f;
        static readonly Color ButtonColor = new Color(1f, 1f, 1f, 0.35f);

        public static GameObject Build(Sprite circle, Font font)
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

            var safe = Stretch(Child(root.transform, "SafeArea"), Vector2.zero, Vector2.one);
            safe.gameObject.AddComponent<SafeAreaFitter>();

            BuildJoystick(safe, circle);
            BuildSwipeZone(safe);

            AddButton(safe, "Jump", "JUMP", VirtualControlPaths.Jump, BottomRight(JumpDp, -28f, 28f), circle, font);
            AddButton(safe, "Attack", "ATK", VirtualControlPaths.Attack, BottomRight(72f, -140f, 80f), circle, font);
            AddButton(safe, "Dash", "DASH", VirtualControlPaths.Dash, BottomRight(72f, -224f, 28f), circle, font);
            AddButton(safe, "Skill", "SKILL", VirtualControlPaths.Skill, BottomRight(MinTouchDp, -236f, 112f), circle, font);
            AddButton(safe, "AuraWind", "G", VirtualControlPaths.AuraWind, BottomRight(MinTouchDp, -28f, 140f), circle, font);
            AddButton(safe, "AuraFire", "H", VirtualControlPaths.AuraFire, BottomRight(MinTouchDp, -28f, 212f), circle, font);
            AddButton(safe, "AuraWater", "T", VirtualControlPaths.AuraWater, BottomRight(MinTouchDp, -100f, 176f), circle, font);
            AddButton(safe, "Pause", "II", VirtualControlPaths.Pause, TopRight(MinTouchDp, -16f, -16f), circle, font);
            AddButton(safe, "Map", "MAP", VirtualControlPaths.Map, TopRight(MinTouchDp, -88f, -16f), circle, font);
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

        static void AddButton(RectTransform parent, string name, string label, string path,
            (Vector2 anchor, float size, Vector2 position) layout, Sprite circle, Font font)
        {
            var rect = Child(parent, name);
            rect.anchorMin = rect.anchorMax = rect.pivot = layout.anchor;
            rect.sizeDelta = new Vector2(layout.size, layout.size);
            rect.anchoredPosition = layout.position;
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = circle;
            image.color = ButtonColor;
            rect.gameObject.AddComponent<OnScreenButton>().controlPath = path;

            var text = Stretch(Child(rect, "Label"), Vector2.zero, Vector2.one).gameObject.AddComponent<Text>();
            text.text = label;
            text.font = font;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 18;
            text.color = Color.white;
            text.raycastTarget = false;
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
