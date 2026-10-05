using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AuraKnight.Aura.Skills
{
    /// <summary>Fires a pooled <see cref="FireballProjectile"/> from Leo's hand toward the facing direction.</summary>
    public sealed class FireballSkill : AuraSkillBase
    {
        const float SpawnOffset = 0.9f;
        const int MaxPool = 6;

        [SerializeField] FireballProjectile projectilePrefab;
        [SerializeField, Range(1, MaxPool)] int prewarm = 3;

        readonly List<FireballProjectile> _pool = new List<FireballProjectile>(MaxPool);

        public override AuraId Aura => AuraId.Fire;
        public int PoolSize => _pool.Count;

        protected override void OnBound()
        {
            if (projectilePrefab == null) return;
            for (int i = _pool.Count; i < prewarm; i++) Grow();
        }

        public override void Cast()
        {
            var projectile = Acquire();
            if (projectile == null) return;
            projectile.Launch(Origin + new Vector2(Facing * SpawnOffset, 0f), Facing, Controller != null ? Controller.transform : null);
        }

        FireballProjectile Acquire()
        {
            for (int i = 0; i < _pool.Count; i++)
                if (!_pool[i].IsFlying) return _pool[i];
            return _pool.Count < MaxPool ? Grow() : null;
        }

        FireballProjectile Grow()
        {
            if (projectilePrefab == null) return null;
            var projectile = Instantiate(projectilePrefab); // world space: it must not follow Leo
            if (gameObject.scene.IsValid()) SceneManager.MoveGameObjectToScene(projectile.gameObject, gameObject.scene); // not whatever scene happens to be active
            projectile.gameObject.SetActive(false);
            _pool.Add(projectile);
            return projectile;
        }

        void OnDestroy()
        {
            foreach (var projectile in _pool)
                if (projectile != null) Destroy(projectile.gameObject);
        }
    }
}
