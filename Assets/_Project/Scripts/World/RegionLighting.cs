using AuraKnight.Core;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace AuraKnight.World
{
    /// <summary>
    /// Lives in a region scene next to that scene's Global Light 2D. Two region scenes can be loaded at once and URP 2D wants one
    /// global light per blend style, so the light is on only while Leo's current room belongs to this region. The intensity comes
    /// from <see cref="RegionLightingTable"/> (GDD 10).
    /// </summary>
    public sealed class RegionLighting : MonoBehaviour
    {
        [SerializeField] string regionId;
        [SerializeField] Light2D globalLight;

        public string RegionId => regionId;

        void Awake()
        {
            if (globalLight == null) globalLight = GetComponent<Light2D>();
            if (globalLight != null) globalLight.intensity = RegionLightingTable.IntensityOf(regionId);
        }

        void OnEnable()
        {
            EventBus.Subscribe<RoomEntered>(OnRoomEntered);
            var current = RoomManager.Instance != null ? RoomManager.Instance.Current : null;
            Apply(current != null && current.RegionId == regionId);
        }

        void OnDisable() => EventBus.Unsubscribe<RoomEntered>(OnRoomEntered);

        void OnRoomEntered(RoomEntered evt) => Apply(evt.RegionId == regionId);

        void Apply(bool active)
        {
            if (globalLight != null) globalLight.enabled = active;
        }
    }
}
