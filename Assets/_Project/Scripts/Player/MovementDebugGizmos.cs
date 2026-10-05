using UnityEngine;

namespace AuraKnight.Player
{
    /// <summary>Scene-view measuring aids for tuning: 1-unit grid, jump apex heights, dash distance.</summary>
    public sealed class MovementDebugGizmos : MonoBehaviour
    {
        [SerializeField] PlayerMovementConfig config;
        [SerializeField] Transform player;
        [SerializeField] Vector2Int gridMin = new Vector2Int(-12, -2);
        [SerializeField] Vector2Int gridMax = new Vector2Int(72, 28);

        void OnDrawGizmos()
        {
            DrawGrid();
            if (config == null || player == null) return;
            var feet = (Vector2)player.position + Vector2.down * 0.95f;

            Gizmos.color = Color.green; // full jump apex
            DrawHorizontal(feet.x, feet.y + config.MaxJumpHeight, 2f);
            Gizmos.color = Color.yellow; // approximate tap-jump apex (cut at min hold)
            DrawHorizontal(feet.x, feet.y + TapJumpHeight(), 1.5f);
            Gizmos.color = Color.cyan; // dash reach in both directions
            float dash = config.DashSpeed * config.DashDuration;
            Gizmos.DrawLine(new Vector3(feet.x - dash, feet.y, 0f), new Vector3(feet.x + dash, feet.y, 0f));
        }

        float TapJumpHeight()
        {
            float t = config.JumpMinHoldTime, v0 = config.JumpVelocity, g = config.JumpGravity;
            float vCut = (v0 - g * t) * config.JumpCutMultiplier;
            return v0 * t - 0.5f * g * t * t + vCut * vCut / (2f * g);
        }

        static void DrawHorizontal(float x, float y, float halfWidth) =>
            Gizmos.DrawLine(new Vector3(x - halfWidth, y, 0f), new Vector3(x + halfWidth, y, 0f));

        void DrawGrid()
        {
            for (int x = gridMin.x; x <= gridMax.x; x++)
            {
                Gizmos.color = x % 5 == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.12f);
                Gizmos.DrawLine(new Vector3(x, gridMin.y, 0f), new Vector3(x, gridMax.y, 0f));
            }
            for (int y = gridMin.y; y <= gridMax.y; y++)
            {
                Gizmos.color = y % 5 == 0 ? new Color(1f, 1f, 1f, 0.35f) : new Color(1f, 1f, 1f, 0.12f);
                Gizmos.DrawLine(new Vector3(gridMin.x, y, 0f), new Vector3(gridMax.x, y, 0f));
            }
        }
    }
}
