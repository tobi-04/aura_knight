using System.Collections.Generic;

namespace AuraKnight.Combat
{
    /// <summary>Remembers who was already hit during one hitbox activation so a swing lands once per target.</summary>
    public sealed class HitRegistry<T> where T : class
    {
        readonly HashSet<T> _hit = new HashSet<T>();

        public int Count => _hit.Count;

        /// <summary>Returns true the first time <paramref name="target"/> is seen since the last <see cref="Clear"/>.</summary>
        public bool TryRegister(T target) => target != null && _hit.Add(target);

        public void Clear() => _hit.Clear();
    }
}
