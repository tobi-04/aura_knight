using System.Collections;
using AuraKnight.Aura;
using AuraKnight.Core;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AuraKnight.Tests.PlayMode.Levels
{
    /// <summary>The three gates in the real hub: the barricade (Fire), the seal gate (all three Auras) and the shortcut doors.</summary>
    public sealed class GatePlayModeTests : LevelsPlayModeBase
    {
        [UnityTest]
        public IEnumerator TheBarricadeBlocksUntilTheFireSkillBurnsIt()
        {
            yield return NewGame();
            var gate = ById<BurnableGate>("gate_hub_barricade");
            var blocker = gate.transform.Find("Blocker");
            var center = (Vector2)blocker.position;
            Physics2D.SyncTransforms();
            Assert.IsNotNull(Physics2D.OverlapPoint(center, PhysicsLayers.GroundMask), "a solid wall of wood stands in the corridor");

            Assert.IsFalse(gate.TryInteract(AuraInteraction.Burn, AuraId.Wind), "Wind does not burn it");
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Burn, AuraId.Water));
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Burn, AuraId.None));
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Extinguish, AuraId.Fire), "the wrong interaction does not work either");
            Assert.IsFalse(gate.IsOpen);
            Assert.IsTrue(blocker.gameObject.activeSelf);

            Assert.IsTrue(gate.TryInteract(AuraInteraction.Burn, AuraId.Fire));
            Physics2D.SyncTransforms();
            Assert.IsTrue(gate.IsOpen);
            Assert.IsNull(Physics2D.OverlapPoint(center, PhysicsLayers.GroundMask), "the way to the City is open");
            Assert.IsTrue(Manager.State.HasOpenedGate("gate_hub_barricade"));
        }

        [UnityTest]
        public IEnumerator ABurnedBarricadeStaysBurnedAfterContinue()
        {
            yield return NewGame();
            Assert.IsTrue(ById<BurnableGate>("gate_hub_barricade").TryInteract(AuraInteraction.Burn, AuraId.Fire));
            Assert.IsTrue(Manager.Save());
            yield return Enter(false, ok => Assert.IsTrue(ok));
            yield return null;
            Assert.IsTrue(ById<BurnableGate>("gate_hub_barricade").IsOpen);
        }

        [UnityTest]
        public IEnumerator TheSealGateOpensOnlyWhenAllThreeSealsAreLit()
        {
            yield return NewGame();
            var gate = ById<SealGate>("gate_hub_castle");
            var wind = ById<AuraSeal>("seal_wind");
            var fire = ById<AuraSeal>("seal_fire");
            var water = ById<AuraSeal>("seal_water");
            var blocker = gate.transform.Find("Blocker").gameObject;
            var auras = AuraManager.Instance;
            Assert.IsFalse(gate.IsOpen);
            Assert.IsFalse(wind.TryLight(auras.Current), "without an Aura no seal answers");

            yield return Unlock(AuraId.Wind);
            Assert.AreEqual(AuraId.Wind, auras.Current);
            Assert.IsTrue(wind.TryLight(auras.Current), "Wind seal lights under Wind");
            Assert.IsFalse(fire.TryLight(auras.Current), "wearing Wind does not light the Fire seal");
            Assert.IsFalse(water.TryLight(auras.Current));
            Assert.IsFalse(gate.Evaluate(), "one seal is not enough");

            yield return Unlock(AuraId.Fire);
            yield return Seconds(0.4f);
            Assert.IsTrue(auras.TrySwitch(AuraId.Fire), "switch to Fire after its unlock");
            Assert.IsTrue(fire.TryLight(auras.Current), "Fire seal lights under Fire");
            Assert.IsFalse(gate.Evaluate(), "two seals are not enough");
            Assert.IsTrue(blocker.activeSelf);

            Assert.IsFalse(auras.TrySwitch(AuraId.Water), "an Aura that is not earned cannot be worn, so its seal stays dark");
            yield return Unlock(AuraId.Water);
            yield return Seconds(0.4f);
            Assert.IsTrue(auras.TrySwitch(AuraId.Water), "switch to Water after its unlock");
            Assert.IsTrue(water.TryLight(auras.Current), "Water seal lights under Water");
            Assert.IsTrue(gate.IsOpen, "all three Auras lit all three seals");
            Assert.IsFalse(blocker.activeSelf);
            Assert.IsTrue(Manager.State.HasOpenedGate("gate_hub_castle"), "gate saved");
            Assert.IsTrue(Manager.State.HasOpenedGate("seal_wind"), "seal saved");
        }

        [UnityTest]
        public IEnumerator ALitSealAndAnOpenGateSurviveAContinue()
        {
            yield return NewGame();
            var auras = AuraManager.Instance;
            yield return Unlock(AuraId.Wind);
            Assert.IsTrue(ById<AuraSeal>("seal_wind").TryLight(auras.Current));
            Assert.IsTrue(Manager.Save());
            yield return Enter(false, ok => Assert.IsTrue(ok));
            yield return null;
            Assert.IsTrue(ById<AuraSeal>("seal_wind").IsLit);
            Assert.IsFalse(ById<AuraSeal>("seal_fire").IsLit);
            Assert.IsFalse(ById<SealGate>("gate_hub_castle").IsOpen);
        }

        /// <summary>From the altar room before the boss through the tunnel into the entry room's pocket, walk to the door, check it opens for good.</summary>
        IEnumerator ShortcutRoundTrip(string region)
        {
            yield return StartAt($"{region}_altar_02", "Wind", "Fire", "Water");
            MakeInvulnerable();
            yield return Through($"{region}_01");
            var shortcut = ById<Shortcut>($"shortcut_{region}");
            Assert.IsFalse(shortcut.IsOpen, "arriving in the pocket does not open it by itself");
            Teleport(shortcut.GetComponent<Collider2D>().bounds.center);
            yield return WaitUntil(() => shortcut.IsOpen, "Leo walks up to the door from the inside");
            Assert.IsTrue(Manager.State.HasOpenedShortcut($"shortcut_{region}"));
            Assert.IsFalse(shortcut.transform.Find("DoorBlocker").gameObject.activeSelf);
            Assert.IsTrue(Manager.Save());

            yield return Enter(false, ok => Assert.IsTrue(ok));
            yield return null;
            Assert.IsTrue(ById<Shortcut>($"shortcut_{region}").IsOpen, "still open after Continue");
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheForestShortcutOpensFromInsideAndStaysOpen() => ShortcutRoundTrip("forest");

        [UnityTest, Timeout(120000)]
        public IEnumerator TheCaveShortcutOpensFromInsideAndStaysOpen() => ShortcutRoundTrip("cave");

        [UnityTest, Timeout(120000)]
        public IEnumerator TheCityShortcutOpensFromInsideAndStaysOpen() => ShortcutRoundTrip("city");

        [UnityTest, Timeout(120000)]
        public IEnumerator TheCastleShortcutOpensFromInsideAndStaysOpen() => ShortcutRoundTrip("castle");

        [UnityTest]
        public IEnumerator AShortcutIsClosedFromTheOutside()
        {
            yield return StartAt("cave_altar_01", "Wind");
            var shortcut = ById<Shortcut>("shortcut_cave");
            Assert.IsFalse(shortcut.IsOpen);
            var blocker = shortcut.transform.Find("DoorBlocker");
            Assert.IsTrue(blocker.gameObject.activeSelf);
            Assert.AreEqual(LayerMask.NameToLayer(PhysicsLayers.Ground), blocker.gameObject.layer, "solid");
        }
    }
}
