using AuraKnight.Audio;
using AuraKnight.Core;
using AuraKnight.Player;
using AuraKnight.Progression;
using UnityEngine;

namespace AuraKnight.World
{
    /// <summary>
    /// Tư Tế Sol in the hub. While Leo stands in her zone a prompt shows; pushing the stick up talks: she picks a line by progress
    /// (<see cref="DialogueByProgress"/>) and the shop opens with that line on top (<see cref="ShopRequested"/>).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class NpcSol : MonoBehaviour
    {
        /// <summary>Stick Y at or above this counts as "up" (same dead zone as the up-slash).</summary>
        public const float UpThreshold = 0.6f;

        [SerializeField] DialogueByProgress dialogue;
        [SerializeField] GameObject prompt;

        IPlayerInput input;
        float lastY;

        public bool PlayerInRange { get; private set; }

        /// <summary>True on the frame the stick crosses the threshold upward (a held stick does not retrigger).</summary>
        public static bool UpPressed(float previousY, float currentY) => previousY < UpThreshold && currentY >= UpThreshold;

        void Reset() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!WorldTags.IsPlayer(other)) return;
            input = other.GetComponentInParent<PlayerController>()?.Input;
            lastY = input != null ? input.Move.y : 0f;
            SetInRange(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (WorldTags.IsPlayer(other)) SetInRange(false);
        }

        void OnDisable() => SetInRange(false);

        void Update()
        {
            if (!PlayerInRange || input == null) return;
            float y = input.Move.y;
            bool up = UpPressed(lastY, y);
            lastY = y;
            if (up && GameManager.ModeAllowsControl) Talk();
        }

        /// <summary>Opens the shop with the line for the current progress.</summary>
        public void Talk()
        {
            var manager = GameManager.Instance;
            string line = dialogue != null ? dialogue.Select(manager != null ? manager.State : null) : string.Empty;
            Sfx.Play(SfxId.UiTap);
            EventBus.Publish(new ShopRequested(line));
        }

        void SetInRange(bool inRange)
        {
            PlayerInRange = inRange;
            if (!inRange) input = null;
            if (prompt != null) prompt.SetActive(inRange);
        }
    }
}
