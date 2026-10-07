using AuraKnight.Core;
using AuraKnight.World;
using UnityEngine;

namespace AuraKnight.Enemies
{
    public abstract partial class EnemyBase
    {
        const float TargetLookupInterval = 0.5f;
        const float LedgeProbeDepth = 0.6f;
        const float ProbeInset = 0.1f;
        /// <summary>Detect states keep chasing a little beyond the range they started at, so the edge of the range does not flicker.</summary>
        protected const float LoseRangeFactor = 1.3f;

        Transform _target;
        float _lookupLeft;

        protected Vector2 Position => _rb.position;
        protected bool HasTarget => _target != null;
        /// <summary>Leo's position (the pivot is near his centre); only meaningful while <see cref="HasTarget"/>.</summary>
        protected Vector2 TargetPosition => _target != null ? (Vector2)_target.position : _rb.position;
        protected Vector2 TargetDelta => TargetPosition - Position;

        void ResetSensing()
        {
            _target = null;
            _lookupLeft = 0f;
        }

        void RefreshTarget(float deltaTime)
        {
            if (_target != null) return;
            _lookupLeft -= deltaTime;
            if (_lookupLeft > 0f) return;
            _lookupLeft = TargetLookupInterval;
            var player = GameObject.FindGameObjectWithTag(WorldTags.Player);
            _target = player != null ? player.transform : null;
        }

        /// <summary>True when Leo is within <paramref name="range"/> tiles (and the variant's vertical tolerance).</summary>
        protected bool TargetInRange(float range) =>
            _target != null && EnemyDetection.InRange(TargetDelta, range, stats.verticalTolerance);

        /// <summary>Sets horizontal speed, keeping the vertical (gravity) component.</summary>
        protected void MoveX(float speed) => _rb.linearVelocity = new Vector2(speed, _rb.linearVelocity.y);

        protected bool IsGrounded()
        {
            var b = BodyBounds();
            if (_rb.linearVelocity.y > 0.1f) return false;
            return Physics2D.Raycast(b.center, Vector2.down, b.extents.y + 0.1f, PhysicsLayers.GroundMask).collider != null;
        }

        /// <summary>True when there is floor one step ahead in the given direction (ledge probe).</summary>
        protected bool GroundAhead(int direction)
        {
            var b = BodyBounds();
            var origin = new Vector2(b.center.x + direction * (b.extents.x + ProbeInset), b.min.y + ProbeInset);
            return Physics2D.Raycast(origin, Vector2.down, LedgeProbeDepth, PhysicsLayers.GroundMask).collider != null;
        }

        /// <summary>True when a Ground wall is within one body-width ahead, at chest or foot height.</summary>
        protected bool WallAhead(int direction)
        {
            var b = BodyBounds();
            float reach = b.extents.x + ProbeInset;
            int mask = PhysicsLayers.GroundMask;
            var dir = new Vector2(direction, 0f);
            var chest = new Vector2(b.center.x, b.center.y);
            var feet = new Vector2(b.center.x, b.min.y + 0.15f);
            return Physics2D.Raycast(chest, dir, reach, mask).collider != null || Physics2D.Raycast(feet, dir, reach, mask).collider != null;
        }

        /// <summary>True while the root collider touches Ground (flyer dives end on impact). Always false for a phasing body.</summary>
        protected bool TouchingGround() => _solidBody && body != null && body.IsTouchingLayers(PhysicsLayers.GroundMask);

        Bounds BodyBounds() => body != null ? body.bounds : new Bounds(_rb.position, new Vector3(0.8f, 0.8f, 0f));
    }
}
