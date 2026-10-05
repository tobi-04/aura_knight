using UnityEngine;

namespace AuraKnight.Core
{
    /// <summary>
    /// The one singleton convention for scene managers: Awake asks <see cref="IsDuplicate"/> first and returns when it is true.
    /// A duplicate disables itself (so OnEnable never runs and it can never subscribe or register) and removes its component.
    /// </summary>
    public static class Singleton
    {
        public static bool IsDuplicate<T>(T current, T self) where T : MonoBehaviour
        {
            if (current == null || current == self) return false;
            Debug.LogWarning($"[{typeof(T).Name}] Duplicate instance on '{self.name}' ignored.", self);
            self.enabled = false;
            Object.Destroy(self);
            return true;
        }
    }
}
