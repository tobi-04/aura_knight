using System.Collections.Generic;
using AuraKnight.Enemies;
using UnityEngine;

namespace AuraKnight.Bosses
{
    /// <summary>
    /// Calls two spiderlings: the StoneSpider enemy prefab (Prefabs/Enemies) scaled down. They are tracked by the boss, so reset and death remove
    /// them. Not started while two or more spiderlings still live.
    /// </summary>
    public sealed class SummonSpiderlingsAttack : BossAttack
    {
        [SerializeField] GameObject spiderlingPrefab;
        [SerializeField, Min(1)] int count = 2;
        [SerializeField, Range(0.3f, 1f)] float scale = 0.6f;
        [SerializeField, Min(1f)] float spawnDistance = 3f;
        [SerializeField, Min(1)] int maxAlive = 2;

        readonly List<EnemyBase> _minions = new List<EnemyBase>();

        public int AliveMinions
        {
            get
            {
                int alive = 0;
                foreach (var minion in _minions)
                    if (minion != null && minion.isActiveAndEnabled && minion.IsAlive) alive++;
                return alive;
            }
        }

        public override bool CanStart(BossBase boss) => spiderlingPrefab != null && AliveMinions < maxAlive;

        protected override void OnExecute()
        {
            _minions.RemoveAll(m => m == null);
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = Mathf.Clamp(Boss.transform.position.x + side * spawnDistance, Boss.Playfield.min.x + 1f, Boss.Playfield.max.x - 1f);
                var go = Instantiate(spiderlingPrefab, Boss.SpawnRoot);
                go.transform.localScale = Vector3.one * scale;
                var enemy = go.GetComponent<EnemyBase>();
                enemy.SetSpawnPoint(new Vector2(x, Boss.FloorY + 0.5f));
                Boss.Track(go);
                _minions.Add(enemy);
            }
        }

        protected override void OnEnd(bool cancelled) { }
    }
}
