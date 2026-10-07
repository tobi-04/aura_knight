using AuraKnight.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace AuraKnight.UI
{
    /// <summary>A room rectangle, an icon or Leo's marker on the map content. Built by <see cref="MapScreen"/> with the static creators.</summary>
    public sealed class MapCellView : MonoBehaviour
    {
        public const float DimAlpha = 0.22f, VisitedAlpha = 0.9f, IconSize = 30f;

        public string Id { get; private set; }
        public RoomVisibility Visibility { get; private set; }
        public MapIconKind IconKind { get; private set; }
        public Image Image { get; private set; }

        public static MapCellView CreateRoom(Transform parent, string roomId, string regionId, RoomVisibility visibility,
            Vector2 position, Vector2 size)
        {
            float alpha = visibility == RoomVisibility.Visited ? VisitedAlpha : DimAlpha;
            var image = UiFactory.Panel(parent, "Room_" + roomId, UITheme.RegionToken(regionId), alpha);
            Anchor(image.rectTransform, position, size);
            var view = image.gameObject.AddComponent<MapCellView>();
            view.Id = roomId;
            view.Visibility = visibility;
            view.Image = image;
            return view;
        }

        public static MapCellView CreateIcon(Transform parent, MapIcon icon, Vector2 center)
        {
            var image = UiFactory.Panel(parent, $"Icon_{icon.kind}_{icon.id}", ColorFor(icon.kind), 1f);
            Anchor(image.rectTransform, center - Vector2.one * (IconSize / 2f), Vector2.one * IconSize);
            var letter = UiFactory.Text(image.transform, "Letter", "map.icon." + icon.kind.ToString().ToLowerInvariant(), UIFontRole.Mono,
                UIColorToken.TextInk, 22f, TMPro.TextAlignmentOptions.Center);
            UiFactory.Stretch(letter.rectTransform);
            letter.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
            var view = image.gameObject.AddComponent<MapCellView>();
            view.Id = icon.id;
            view.IconKind = icon.kind;
            view.Image = image;
            return view;
        }

        public static MapCellView CreateMarker(Transform parent, Vector2 center)
        {
            const float size = 26f;
            var image = UiFactory.Panel(parent, "LeoMarker", UIColorToken.TextPrimary, 1f);
            Anchor(image.rectTransform, center - Vector2.one * (size / 2f), Vector2.one * size);
            var ring = UiFactory.Panel(image.transform, "Ring", UIColorToken.Night, 1f);
            UiFactory.Stretch(ring.rectTransform, 6f, 6f, 6f, 6f);
            var view = image.gameObject.AddComponent<MapCellView>();
            view.Id = "leo";
            view.Image = image;
            return view;
        }

        public static UIColorToken ColorFor(MapIconKind kind)
        {
            switch (kind)
            {
                case MapIconKind.Altar: return UIColorToken.Gold;
                case MapIconKind.Boss: return UIColorToken.Fire;
                case MapIconKind.Chest: return UIColorToken.Paper;
                default: return UIColorToken.Wind;
            }
        }

        static void Anchor(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
    }
}
