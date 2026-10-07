using AuraKnight.Core;
using AuraKnight.Progression;
using UnityEngine;

namespace AuraKnight.UI
{
    /// <summary>
    /// Always-active listener in the Core scene (the screens themselves are inactive, so they cannot subscribe): opens the map on
    /// <see cref="MapRequested"/> (a second request closes it) and the shop on <see cref="ShopRequested"/>, only while the game is playing.
    /// </summary>
    public sealed class ShopMapLauncher : MonoBehaviour
    {
        [SerializeField] UIRouter router;
        [SerializeField] MapScreen map;
        [SerializeField] ShopScreen shop;

        public void Bind(UIRouter uiRouter, MapScreen mapScreen, ShopScreen shopScreen)
        {
            router = uiRouter;
            map = mapScreen;
            shop = shopScreen;
        }

        void OnEnable()
        {
            EventBus.Subscribe<MapRequested>(OnMap);
            EventBus.Subscribe<ShopRequested>(OnShop);
        }

        void OnDisable()
        {
            EventBus.Unsubscribe<MapRequested>(OnMap);
            EventBus.Unsubscribe<ShopRequested>(OnShop);
        }

        void OnMap(MapRequested evt)
        {
            if (router.Contains(map))
            {
                router.Remove(map);
                return;
            }
            if (CanOpen()) router.Push(map);
        }

        void OnShop(ShopRequested evt)
        {
            if (router.Contains(shop) || !CanOpen()) return;
            shop.SetDialogue(evt.DialogueKey);
            router.Push(shop);
        }

        static bool CanOpen()
        {
            var pause = PauseController.Instance;
            return pause != null ? pause.CanPause : GameManager.ModeAllowsControl;
        }
    }
}
