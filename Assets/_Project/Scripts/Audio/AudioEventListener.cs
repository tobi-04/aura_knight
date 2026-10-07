using AuraKnight.Aura;
using AuraKnight.Core;
using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Turns EventBus events into sounds so gameplay never names a clip: damage, death, respawn, Aura switch / unlock,
    /// skill casts (an energy drop), coin pickups and altars. Movement and sword sounds come from <see cref="PlayerSfxProbe"/>.
    /// Initial values published at startup (the first Aura, the coin total) only seed the trackers and stay silent.
    /// </summary>
    public sealed class AudioEventListener : MonoBehaviour
    {
        string _lastAura;
        float _lastEnergy = -1f;
        int _lastCoins = -1;
        int _unlockFrame = -1;
        bool _bound;

        void OnEnable()
        {
            if (_bound) return;
            _bound = true;
            EventBus.Subscribe<PlayerDamaged>(OnDamaged);
            EventBus.Subscribe<PlayerDied>(OnDied);
            EventBus.Subscribe<PlayerRespawned>(OnRespawned);
            EventBus.Subscribe<AuraChanged>(OnAuraChanged);
            EventBus.Subscribe<AuraUnlocked>(OnAuraUnlocked);
            EventBus.Subscribe<EnergyChanged>(OnEnergyChanged);
            EventBus.Subscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Subscribe<CheckpointReached>(OnCheckpoint);
            EventBus.Subscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnDisable()
        {
            if (!_bound) return;
            _bound = false;
            EventBus.Unsubscribe<PlayerDamaged>(OnDamaged);
            EventBus.Unsubscribe<PlayerDied>(OnDied);
            EventBus.Unsubscribe<PlayerRespawned>(OnRespawned);
            EventBus.Unsubscribe<AuraChanged>(OnAuraChanged);
            EventBus.Unsubscribe<AuraUnlocked>(OnAuraUnlocked);
            EventBus.Unsubscribe<EnergyChanged>(OnEnergyChanged);
            EventBus.Unsubscribe<CoinsChanged>(OnCoinsChanged);
            EventBus.Unsubscribe<CheckpointReached>(OnCheckpoint);
            EventBus.Unsubscribe<GameStateLoaded>(OnStateLoaded);
        }

        void OnDamaged(PlayerDamaged e) => Play(SfxId.PlayerHurt);
        void OnDied(PlayerDied e) => Play(SfxId.PlayerDie);
        void OnRespawned(PlayerRespawned e) => Play(SfxId.Respawn);
        void OnCheckpoint(CheckpointReached e) => Play(SfxId.Altar);

        void OnAuraChanged(AuraChanged e)
        {
            bool first = _lastAura == null;
            bool same = e.AuraId == _lastAura;
            _lastAura = e.AuraId;
            // the unlock jingle already covers the switch an unlock causes in the same frame
            if (first || same || _unlockFrame == Time.frameCount) return;
            Play(SfxEventMap.ForAuraChanged(e.AuraId));
        }

        void OnAuraUnlocked(AuraUnlocked e)
        {
            _unlockFrame = Time.frameCount;
            Play(SfxId.AuraUnlock);
        }

        void OnEnergyChanged(EnergyChanged e)
        {
            bool spent = SfxEventMap.IsEnergySpent(_lastEnergy, e.Current);
            _lastEnergy = e.Current;
            if (!spent) return;
            var manager = AuraManager.Instance;
            if (manager != null) Play(SfxEventMap.ForSkill(manager.Current));
        }

        void OnCoinsChanged(CoinsChanged e)
        {
            bool gained = SfxEventMap.IsCoinGain(_lastCoins, e.Coins);
            _lastCoins = e.Coins;
            if (gained) Play(SfxId.Coin);
        }

        void OnStateLoaded(GameStateLoaded e)
        {
            _lastAura = null;
            _lastEnergy = -1f;
            _lastCoins = -1;
        }

        static void Play(SfxId id) => Sfx.Play(id);
    }
}
