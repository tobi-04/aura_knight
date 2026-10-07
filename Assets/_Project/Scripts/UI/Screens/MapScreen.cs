using System.Collections.Generic;
using AuraKnight.Core;
using AuraKnight.Progression;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// World map: one coloured rectangle per room from the <see cref="RoomMapData"/> of every region (region colour, GDD 9.1). Visited rooms
    /// are solid; unvisited rooms are faint and only exist when that region's map was bought; icons show on visited rooms; Leo is a marker
    /// at his position in the current room. Drag, pinch and the +/- buttons move a <see cref="MapViewport"/>. Pauses the game while open.
    /// </summary>
    public sealed class MapScreen : UIScreen
    {
        public const float CellPx = 56f, CellGap = 4f;

        [SerializeField] UIRouter router;
        [SerializeField] RoomMapData[] regions = new RoomMapData[0];
        [SerializeField] RectTransform viewport;
        [SerializeField] RectTransform content;
        [SerializeField] MapInput input;
        [SerializeField] UIButton zoomInButton, zoomOutButton, centerButton, backButton;

        readonly MapViewport view = new();
        readonly Dictionary<string, MapCellView> rooms = new();
        readonly List<MapCellView> icons = new();
        RectInt union;
        bool ownsPause;

        public IReadOnlyDictionary<string, MapCellView> Rooms => rooms;
        public IReadOnlyList<MapCellView> Icons => icons;
        public MapCellView LeoMarker { get; private set; }
        public MapViewport Viewport => view;

        public void Bind(UIRouter uiRouter, RoomMapData[] data, RectTransform viewportRect, RectTransform contentRect, MapInput mapInput,
            UIButton zoomIn, UIButton zoomOut, UIButton center, UIButton back)
        {
            router = uiRouter;
            regions = data;
            viewport = viewportRect;
            content = contentRect;
            input = mapInput;
            zoomInButton = zoomIn;
            zoomOutButton = zoomOut;
            centerButton = center;
            backButton = back;
        }

        protected override void Awake()
        {
            base.Awake();
            zoomInButton.onClick.AddListener(() => ZoomFromButton(1.4f));
            zoomOutButton.onClick.AddListener(() => ZoomFromButton(1f / 1.4f));
            centerButton.onClick.AddListener(FocusOnLeo);
            backButton.onClick.AddListener(Close);
            input.Dragged += OnDragged;
            input.Pinched += OnPinched;
            input.Wheeled += OnWheeled;
        }

        protected override void OnShowing()
        {
            var pause = PauseController.Instance;
            ownsPause = pause != null && pause.Pause(false);
            var manager = GameManager.Instance;
            CaptureLeo(out string roomId, out var normalized);
            Rebuild(manager != null ? manager.State : null, roomId, normalized);
            view.Reset();
            FocusOnLeo();
        }

        protected override void OnHiding()
        {
            if (ownsPause) PauseController.Instance?.Resume();
            ownsPause = false;
        }

        public override bool HandleBack()
        {
            Close();
            return true;
        }

        void Close()
        {
            if (router != null && router.Contains(this)) router.Remove(this);
            else Hide();
        }

        /// <summary>Redraws everything for a save and Leo's room (null id = no marker). <paramref name="leoInRoom"/> is 0..1 inside that room.</summary>
        public void Rebuild(GameState state, string leoRoomId, Vector2 leoInRoom)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            rooms.Clear();
            icons.Clear();
            LeoMarker = null;
            union = RoomMapData.Union(regions);
            content.sizeDelta = new Vector2(union.width, union.height) * CellPx;
            view.SetContentSize(content.sizeDelta);

            foreach (var data in regions)
            {
                if (data == null) continue;
                foreach (var room in data.Rooms)
                {
                    var visibility = MapRules.Visibility(state, data.RegionId, room.roomId);
                    if (visibility == RoomVisibility.Hidden) continue;
                    var cells = data.ToWorldCells(room.cells);
                    rooms[room.roomId] = MapCellView.CreateRoom(content, room.roomId, data.RegionId, visibility, ToPixels(cells.min),
                        new Vector2(cells.width, cells.height) * CellPx - Vector2.one * CellGap);
                }
                foreach (var icon in data.Icons)
                    if (MapRules.IconVisible(state, data.RegionId, icon))
                        icons.Add(MapCellView.CreateIcon(content, icon, ToPixels(icon.cell + data.GridOffset) + Vector2.one * (CellPx / 2f)));
            }
            PlaceLeo(leoRoomId, leoInRoom);
            ApplyView();
        }

        void PlaceLeo(string roomId, Vector2 inRoom)
        {
            if (string.IsNullOrEmpty(roomId)) return;
            foreach (var data in regions)
            {
                if (data == null || !data.TryGetRoom(roomId, out var room)) continue;
                var cells = data.ToWorldCells(room.cells);
                var cell = new Vector2(cells.xMin + inRoom.x * cells.width, cells.yMin + inRoom.y * cells.height);
                LeoMarker = MapCellView.CreateMarker(content, ToPixels(cell));
                return;
            }
        }

        Vector2 ToPixels(Vector2 gridPoint) => (gridPoint - new Vector2(union.xMin, union.yMin)) * CellPx;

        Vector2 ToPixels(Vector2Int gridPoint) => ToPixels(new Vector2(gridPoint.x, gridPoint.y));

        static void CaptureLeo(out string roomId, out Vector2 normalized)
        {
            roomId = null;
            normalized = new Vector2(0.5f, 0.5f);
            var room = RoomManager.Instance != null ? RoomManager.Instance.Current : null;
            if (room == null) return;
            roomId = room.RoomId;
            var player = GameObject.FindGameObjectWithTag(WorldTags.Player);
            if (player == null || room.Bounds == null) return;
            var bounds = room.Bounds.bounds;
            normalized = MapRules.Normalize(player.transform.position, bounds.min, bounds.max);
        }

        void FocusOnLeo()
        {
            var centre = new Vector2(content.sizeDelta.x, content.sizeDelta.y) * 0.5f;
            var target = LeoMarker != null ? ((RectTransform)LeoMarker.transform).anchoredPosition : centre;
            view.FocusOn(target - centre);
            ApplyView();
        }

        void ZoomFromButton(float factor)
        {
            view.ZoomBy(factor, Vector2.zero);
            ApplyView();
        }

        void OnDragged(Vector2 screenDelta)
        {
            view.Drag(screenDelta / ScaleFactor());
            ApplyView();
        }

        void OnPinched(float previous, float distance, Vector2 screenMid)
        {
            view.Pinch(previous / ScaleFactor(), distance / ScaleFactor(), ToViewport(screenMid));
            ApplyView();
        }

        void OnWheeled(float factor, Vector2 screenPoint)
        {
            view.ZoomBy(factor, ToViewport(screenPoint));
            ApplyView();
        }

        float ScaleFactor()
        {
            var canvas = GetComponentInParent<Canvas>();
            return canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
        }

        Vector2 ToViewport(Vector2 screenPoint)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, screenPoint, null, out var local);
            return local;
        }

        void ApplyView()
        {
            content.localScale = new Vector3(view.Zoom, view.Zoom, 1f);
            content.anchoredPosition = view.Pan;
        }
    }
}
