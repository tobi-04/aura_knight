using System;
using System.Collections.Generic;

namespace AuraKnight.Aura
{
    public enum SwitchResult { Switched, AlreadyCurrent, Locked, Cooldown }

    public enum UnlockResult { AlreadyUnlocked, Invalid, Unlocked, UnlockedAndSwitched }

    public enum CastResult { Cast, NoAura, SkillCooldown, NotEnoughEnergy, Blocked }

    /// <summary>
    /// Pure Aura rules: unlocked set, current Aura, 0.3 s switch cooldown, cycling and skill cooldown.
    /// <see cref="AuraManager"/> wraps it with events, save data and scene wiring.
    /// </summary>
    public sealed class AuraState
    {
        public const float SwitchCooldown = 0.3f;

        readonly bool[] _unlocked = new bool[AuraIds.All.Length];
        float _switchCooldown;
        float _skillCooldown;

        public AuraState() => _unlocked[(int)AuraId.None] = true;

        public AuraId Current { get; private set; } = AuraId.None;
        public float SwitchCooldownRemaining => _switchCooldown;
        public float SkillCooldownRemaining => _skillCooldown;

        /// <summary>Unlocked Auras other than None.</summary>
        public int UnlockedCount
        {
            get
            {
                int count = 0;
                for (int i = 1; i < _unlocked.Length; i++) if (_unlocked[i]) count++;
                return count;
            }
        }

        public bool IsUnlocked(AuraId id) => IsValid(id) && _unlocked[(int)id];

        public void Tick(float deltaTime)
        {
            if (_switchCooldown > 0f) _switchCooldown = Math.Max(0f, _switchCooldown - deltaTime);
            if (_skillCooldown > 0f) _skillCooldown = Math.Max(0f, _skillCooldown - deltaTime);
        }

        public SwitchResult TrySwitch(AuraId id)
        {
            if (!IsUnlocked(id)) return SwitchResult.Locked;
            if (id == Current) return SwitchResult.AlreadyCurrent;
            if (_switchCooldown > 0f) return SwitchResult.Cooldown;
            Current = id;
            _switchCooldown = SwitchCooldown;
            return SwitchResult.Switched;
        }

        /// <summary>
        /// Next (+1) or previous (-1) unlocked Aura in Wind, Fire, Water order, wrapping around.
        /// None is skipped once any real Aura exists. Returns the target, or the current Aura when nothing else is available.
        /// </summary>
        public AuraId PeekCycle(int direction)
        {
            int step = direction >= 0 ? 1 : -1;
            int ring = _unlocked.Length - 1; // positions 1..ring hold the real Auras
            int position = Current != AuraId.None ? (int)Current : (step > 0 ? 0 : 1);
            for (int n = 0; n < ring; n++)
            {
                position = ((position - 1 + step) % ring + ring) % ring + 1;
                if (_unlocked[position]) return (AuraId)position;
            }
            return Current;
        }

        /// <summary>Switches to the next/previous unlocked Aura, honouring the switch cooldown.</summary>
        public SwitchResult TryCycle(int direction)
        {
            var target = PeekCycle(direction);
            return target == Current ? SwitchResult.AlreadyCurrent : TrySwitch(target);
        }

        /// <summary>The first real Aura is equipped automatically (ignores the cooldown); later ones only unlock.</summary>
        public UnlockResult Unlock(AuraId id)
        {
            if (id == AuraId.None || !IsValid(id)) return UnlockResult.Invalid;
            if (_unlocked[(int)id]) return UnlockResult.AlreadyUnlocked;
            bool first = UnlockedCount == 0;
            _unlocked[(int)id] = true;
            if (!first) return UnlockResult.Unlocked;
            Current = id;
            return UnlockResult.UnlockedAndSwitched;
        }

        /// <summary>
        /// Resets from save data. Unknown ids are ignored; an invalid or locked current Aura falls back to
        /// the first unlocked real Aura, else None. Cooldowns are cleared.
        /// </summary>
        public void Load(IEnumerable<string> unlockedIds, string currentId)
        {
            for (int i = 1; i < _unlocked.Length; i++) _unlocked[i] = false;
            if (unlockedIds != null)
                foreach (var key in unlockedIds)
                    if (AuraIds.TryParse(key, out var id) && id != AuraId.None) _unlocked[(int)id] = true;

            var wanted = AuraIds.ParseOrNone(currentId);
            Current = _unlocked[(int)wanted] ? wanted : FirstUnlockedReal();
            _switchCooldown = 0f;
            _skillCooldown = 0f;
        }

        /// <summary>Appends the unlocked real Auras (never None) to <paramref name="target"/> after clearing it.</summary>
        public void ExportUnlocked(List<string> target)
        {
            target.Clear();
            for (int i = 1; i < _unlocked.Length; i++)
                if (_unlocked[i]) target.Add(AuraIds.ToKey((AuraId)i));
        }

        /// <summary>
        /// Starts a skill cast: needs a real Aura, no skill cooldown, and <paramref name="spendEnergy"/> to accept
        /// <paramref name="cost"/>. Energy is only spent when everything else allows the cast.
        /// </summary>
        public CastResult TryCast(float cost, float cooldown, Func<float, bool> spendEnergy)
        {
            if (Current == AuraId.None) return CastResult.NoAura;
            if (_skillCooldown > 0f) return CastResult.SkillCooldown;
            if (spendEnergy == null || !spendEnergy(cost)) return CastResult.NotEnoughEnergy;
            _skillCooldown = Math.Max(0f, cooldown);
            return CastResult.Cast;
        }

        AuraId FirstUnlockedReal()
        {
            for (int i = 1; i < _unlocked.Length; i++) if (_unlocked[i]) return (AuraId)i;
            return AuraId.None;
        }

        static bool IsValid(AuraId id) => (int)id >= 0 && (int)id < AuraIds.All.Length;
    }
}
