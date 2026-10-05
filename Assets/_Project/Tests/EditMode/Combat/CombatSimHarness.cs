using System;
using System.Collections.Generic;
using AuraKnight.Combat;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.Tests.Player;
using UnityEngine;

namespace AuraKnight.Tests.Combat
{
    /// <summary>
    /// Real PlayerController + stats + combat + sword hitbox stepped at 50 Hz against static dummies.
    /// The player object is inactive (no Awake in edit mode), so enemy hits on the player go through its Hurtbox directly.
    /// </summary>
    public sealed class CombatSimHarness
    {
        readonly PlayerSimHarness _sim = new PlayerSimHarness();
        readonly List<GameObject> _objects = new List<GameObject>();

        public PlayerController Player => _sim.Player;
        public FakeInput Input => _sim.Input;
        public PlayerStats Stats { get; }
        public PlayerCombat Combat { get; }
        public Hurtbox PlayerHurtbox { get; }
        public Hitbox Sword { get; }
        public Vector2 Position => _sim.Position;
        public PlayerStateId State => _sim.State;
        public int RespawnCalls { get; private set; }
        public Vector2 RespawnPoint { get; set; } = new Vector2(30f, 0.97f);
        public bool RespawnSucceeds { get; set; } = true;

        public CombatSimHarness(int swordLevel = 1)
        {
            var go = Player.gameObject;
            go.AddComponent<Health>().InvulnerableAfterHit = 1f;
            Stats = go.AddComponent<PlayerStats>();
            PlayerHurtbox = go.AddComponent<Hurtbox>();
            PlayerHurtbox.Team = Team.Player;
            Combat = go.AddComponent<PlayerCombat>();

            var swordGo = new GameObject("Sword");
            _objects.Add(swordGo);
            swordGo.AddComponent<BoxCollider2D>().isTrigger = true;
            Sword = swordGo.AddComponent<Hitbox>();

            Stats.Initialize(new PlayerStatsSeed(5, 100, swordLevel));
            Combat.Initialize(Player, Stats, Sword);
            Combat.RespawnHandler = Respawn;
            _sim.Settle();
        }

        bool Respawn()
        {
            RespawnCalls++;
            if (!RespawnSucceeds) return false;
            AuraKnight.World.WorldTags.Teleport(Player.transform, RespawnPoint, keepVelocity: false); // same path CheckpointService uses
            EventBus.Publish(new PlayerRespawned());
            return true;
        }

        public Health AddDummy(float x, float y, int hp = 20, bool pogo = true, Team team = Team.Enemy, float size = 0.8f)
        {
            var go = new GameObject("Dummy");
            _objects.Add(go);
            go.transform.position = new Vector3(x, y, 0f);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(size, size);
            var health = go.AddComponent<Health>();
            health.Initialize(hp);
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = team;
            hurtbox.Health = health;
            hurtbox.AllowsPogo = pogo;
            _sim.Level.Sync();
            return health;
        }

        /// <summary>Spike-like hurtbox: pogo-able, no health.</summary>
        public Hurtbox AddHazard(float x, float y)
        {
            var go = new GameObject("Spikes");
            _objects.Add(go);
            go.transform.position = new Vector3(x, y, 0f);
            go.AddComponent<BoxCollider2D>().isTrigger = true;
            var hurtbox = go.AddComponent<Hurtbox>();
            hurtbox.Team = Team.Hazard;
            hurtbox.AllowsPogo = true;
            _sim.Level.Sync();
            return hurtbox;
        }

        public void Teleport(float x, float y) => _sim.Teleport(x, y);
        public void Frame() => _sim.Frame();
        public void Run(float seconds) => _sim.Run(seconds);

        public void PressAttack(Vector2? stick = null)
        {
            Input.Move = stick ?? Vector2.zero;
            Input.AttackPressed = true;
            Frame();
        }

        public HitOutcome EnemyHit(int amount = 1, Vector2? direction = null) =>
            PlayerHurtbox.Receive(new DamageInfo(amount, Team.Enemy, null, direction ?? Vector2.right));

        public void Dispose()
        {
            Combat.Unbind();
            Stats.Unbind();
            EventBus.Clear();
            foreach (var go in _objects) if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _sim.Dispose();
        }
    }
}
