using UnityEngine;

namespace AuraKnight.Enemies.Modifiers
{
    /// <summary>
    /// Ghost: ignores Ground by keeping the root solid collider off (the flyer has no gravity, so nothing else changes).
    /// Re-applied after every reset because the base re-enables its colliders; OnDisable restores a normal body.
    /// </summary>
    [RequireComponent(typeof(EnemyBase))]
    public sealed class PhaseThroughWalls : MonoBehaviour
    {
        EnemyBase _enemy;

        void Awake() => _enemy = GetComponent<EnemyBase>();

        void OnEnable() => _enemy.SolidBody = false;

        void OnDisable()
        {
            if (_enemy != null) _enemy.SolidBody = true;
        }
    }
}
