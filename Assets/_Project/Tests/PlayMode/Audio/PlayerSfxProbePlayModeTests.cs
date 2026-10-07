using System.Collections;
using AuraKnight.Audio;
using AuraKnight.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode
{
    /// <summary>The probe turns real player state changes into sounds (it is added at runtime when the prefab lacks it).</summary>
    public sealed class PlayerSfxProbePlayModeTests : WorldPlayTestBase
    {
        [UnityTest]
        public IEnumerator StateChanges_PlayMovementAndSwordSounds()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            var controller = Player.GetComponent<PlayerController>();
            if (Player.GetComponent<PlayerSfxProbe>() == null) Player.AddComponent<PlayerSfxProbe>();
            yield return null; // Start binds the probe
            var audio = AudioManager.Instance;

            foreach (var (state, expected) in new[]
            {
                (PlayerStateId.Dash, SfxId.Dash), (PlayerStateId.Slide, SfxId.Slide),
                (PlayerStateId.WallSlide, SfxId.WallSlide), (PlayerStateId.Attack, SfxId.SwordSwing),
            })
            {
                yield return new WaitForSecondsRealtime(0.06f); // past the per-sound throttle
                Assert.IsTrue(controller.StateMachine.TryChange(state), state.ToString());
                Assert.AreEqual(expected, audio.LastPlayed, $"entering {state}");
            }
        }

        [UnityTest]
        public IEnumerator Probe_UnsubscribesWhenThePlayerIsDestroyed()
        {
            bool ok = false;
            yield return Enter(true, r => ok = r);
            Assert.IsTrue(ok);
            var controller = Player.GetComponent<PlayerController>();
            var probe = Player.GetComponent<PlayerSfxProbe>() ?? Player.AddComponent<PlayerSfxProbe>();
            yield return null;
            Object.Destroy(probe);
            yield return null;
            yield return WaitUntil(() => controller.Grounded, "Leo to settle on the ground");
            yield return new WaitForSecondsRealtime(0.1f); // let landing and respawn sounds finish
            int before = AudioManager.Instance.PlayCount;
            controller.StateMachine.TryChange(PlayerStateId.Dash);
            Assert.AreEqual(before, AudioManager.Instance.PlayCount, "no sound once the probe is gone");
        }
    }
}
