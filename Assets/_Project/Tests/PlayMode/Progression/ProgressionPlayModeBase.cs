using System.Collections;
using System.Reflection;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.Progression;
using AuraKnight.Tests.PlayMode;
using AuraKnight.UI;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.PlayMode.Progression
{
    /// <summary>Real Core scene with the real UI_Root; helpers to enter the world and place chests.</summary>
    public abstract class ProgressionPlayModeBase : WorldPlayTestBase
    {
        protected static T Find<T>() where T : Object => Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);

        protected PlayerStats Stats => Player.GetComponent<PlayerStats>();

        protected IEnumerator EnterWorld(bool newGame)
        {
            bool ok = false;
            yield return Enter(newGame, r => ok = r);
            Assert.IsTrue(ok, newGame ? "new game entered" : "continue entered");
            yield return SettlePhysics();
        }

        protected static ShopItem Item(string id, ShopEffect effect, int amount, int[] prices, int max) =>
            ScriptableObject.CreateInstance<ShopItem>().Configure(id, "n", "d", effect, amount, prices, max);

        protected static TreasureChest MakeChest(string id, Vector3 position, float size = 1f)
        {
            var go = new GameObject("Chest_" + id) { layer = LayerMask.NameToLayer(PhysicsLayers.Interactable) };
            go.transform.position = position;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.one * size;
            var pid = go.AddComponent<PersistentId>();
            typeof(PersistentId).GetField("id", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(pid, id);
            return go.AddComponent<TreasureChest>();
        }
    }
}
