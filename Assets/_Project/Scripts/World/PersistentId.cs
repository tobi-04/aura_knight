using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Stable string id for objects whose state is saved (chests, shortcuts, bosses). Never rename
    /// after release. Uniqueness is enforced by the Aura/Validate Rooms editor check.
    /// </summary>
    public sealed class PersistentId : MonoBehaviour
    {
        [SerializeField] string id;

        public string Id => id;
    }
}
