using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>
    /// Marks a solid collider as a smooth wall: it blocks Leo like any Ground collider but he cannot wall-slide on it or
    /// wall-jump off it (Cave entrance, GDD 7.2). Without this, chained wall jumps would climb any single wall and the
    /// 6-tile gate would not need the Wind double jump.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SmoothWall : MonoBehaviour
    {
    }
}
