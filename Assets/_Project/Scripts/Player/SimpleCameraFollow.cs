using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Minimal smoothed follow for the Test_Movement scene; real levels use Cinemachine (phase 6).</summary>
    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] float smoothTime = 0.12f;
        [SerializeField] Vector2 offset = new Vector2(0f, 2f);

        Vector3 _velocity;

        void LateUpdate()
        {
            if (target == null) return;
            var goal = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref _velocity, smoothTime);
        }
    }
}
