using AuraKnight.Core;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Editor
{
    /// <summary>
    /// Gives the standalone test scenes a way to respawn: a CheckpointService plus a sun altar at the spawn point under the
    /// Hub start id. Without a GameManager the service falls back to the altar found in the loaded scene.
    /// </summary>
    static class TestCheckpoint
    {
        public static void Build(Vector2 spawn)
        {
            var managers = new GameObject("TestCheckpoint");
            managers.AddComponent<CheckpointService>();
            var altar = new GameObject("TestAltar");
            altar.transform.SetParent(managers.transform, false);
            altar.transform.position = spawn;
            PlayerGeneratorUtil.SetLayer(altar, PhysicsLayers.Interactable);
            var trigger = altar.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(1f, 1f);
            var sunAltar = altar.AddComponent<SunAltar>();
            PlayerGeneratorUtil.SetString(sunAltar, "altarId", GameState.StartAltarId);
        }
    }
}
