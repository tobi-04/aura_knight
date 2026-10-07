using UnityEngine;

namespace AuraKnight.Audio
{
    /// <summary>
    /// Entry point for systems that play sounds without owning clips (enemies, bosses, UI):
    /// <c>Sfx.Play(SfxId.UiTap)</c>. Does nothing when no AudioManager is loaded (menus in tests, editor scenes).
    /// </summary>
    public static class Sfx
    {
        /// <summary>Plays <paramref name="id"/> in 2D, or positioned in the world when <paramref name="position"/> is given.</summary>
        public static void Play(SfxId id, Vector3? position = null)
        {
            var manager = AudioManager.Instance;
            if (manager != null) manager.Play(id, position);
        }
    }
}
