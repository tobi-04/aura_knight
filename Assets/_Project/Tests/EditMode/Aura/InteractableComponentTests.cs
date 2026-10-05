using AuraKnight.Aura;
using AuraKnight.Aura.Skills;
using AuraKnight.Combat;
using AuraKnight.World;
using NUnit.Framework;
using UnityEngine;

namespace AuraKnight.Tests.Aura
{
    public sealed class InteractableComponentTests : AuraTestBase
    {
        T NewGate<T>(out GameObject blocker, Vector2? position = null) where T : OneTimeAuraGate
        {
            var root = MakeBox("Gate", position ?? Vector2.zero, new Vector2(2f, 3f), true);
            blocker = MakeBox("Blocker", root.transform.position, new Vector2(1f, 3f), false);
            blocker.transform.SetParent(root.transform, true);
            var gate = root.AddComponent<T>();
            SetObjects(gate, "switches.deactivateOnOpen", blocker);
            return gate;
        }

        [Test]
        public void BurnGateOpensOnlyForFireBurn()
        {
            var gate = NewGate<BurnableGate>(out var blocker);
            Assert.AreEqual(AuraId.Fire, gate.RequiredAura);
            foreach (var other in new[] { AuraId.None, AuraId.Wind, AuraId.Water })
            {
                Assert.IsFalse(gate.TryInteract(AuraInteraction.Burn, other), $"{other} must not burn");
                Assert.IsFalse(gate.IsOpen);
            }
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Extinguish, AuraId.Fire));
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Freeze, AuraId.Water));
            Assert.IsTrue(blocker.activeSelf);

            Assert.IsTrue(gate.TryInteract(AuraInteraction.Burn, AuraId.Fire));
            Assert.IsTrue(gate.IsOpen);
            Assert.IsFalse(blocker.activeSelf);
            Assert.IsTrue(gate.TryInteract(AuraInteraction.Burn, AuraId.Fire), "opening again is harmless");
        }

        [Test]
        public void ExtinguishGateOpensOnlyForWater()
        {
            var gate = NewGate<ExtinguishableGate>(out _);
            Assert.AreEqual(AuraId.Water, gate.RequiredAura);
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Extinguish, AuraId.Fire));
            Assert.IsFalse(gate.TryInteract(AuraInteraction.Burn, AuraId.Fire));
            Assert.IsTrue(gate.TryInteract(AuraInteraction.Extinguish, AuraId.Water));
        }

        [Test]
        public void PresenceDrivenZonesAreNotSkillTargets()
        {
            var zone = MakeBox("Lift", Vector2.zero, new Vector2(2f, 3f), true).AddComponent<WindLiftZone>();
            var vent = MakeBox("Vent", Vector2.zero, new Vector2(2f, 3f), true).AddComponent<HeatVent>();
            Assert.IsNull(zone.GetComponent<IAuraInteractable>(), "lift zones follow presence, not skills");
            Assert.IsNull(vent.GetComponent<IAuraInteractable>());
            Assert.AreEqual(AuraId.Wind, zone.RequiredAura);
            Assert.AreEqual(AuraInteraction.HeatVent, vent.Interaction);
        }

        [Test]
        public void WindLiftZoneStaysClosedWithoutTheWindAura()
        {
            var blocker = Make("Blocker", Vector2.zero);
            var zone = MakeBox("Lift", Vector2.zero, new Vector2(2f, 3f), true).AddComponent<WindLiftZone>();
            SetObjects(zone, "switches.deactivateOnOpen", blocker);
            zone.SetPlayerInside(true); // no AuraManager in the scene: current Aura is None
            Assert.IsFalse(zone.IsOpen);
            Assert.IsTrue(blocker.activeSelf);
            zone.SetPlayerInside(false);
            Assert.IsFalse(zone.IsOpen);
        }

        [Test]
        public void LavaFreezesForFourSecondsThenThawsAndRefreezeExtendsIt()
        {
            var root = MakeBox("Lava", Vector2.zero, new Vector2(6f, 2f), true);
            var lava = Make("LavaHazard", Vector2.zero);
            var platform = Make("Platform", Vector2.zero);
            platform.SetActive(false);
            var freezable = root.AddComponent<LavaFreezable>();
            SetRef(freezable, "lava", lava);
            SetRef(freezable, "platform", platform);

            Assert.IsFalse(freezable.TryInteract(AuraInteraction.Freeze, AuraId.Fire));
            Assert.IsFalse(freezable.TryInteract(AuraInteraction.Burn, AuraId.Water));
            Assert.IsFalse(freezable.IsFrozen);

            Assert.IsTrue(freezable.TryInteract(AuraInteraction.Freeze, AuraId.Water));
            Assert.IsTrue(freezable.IsFrozen);
            Assert.IsFalse(lava.activeSelf);
            Assert.IsTrue(platform.activeSelf);

            freezable.Tick(3.9f);
            Assert.IsTrue(freezable.IsFrozen);
            freezable.TryInteract(AuraInteraction.Freeze, AuraId.Water);
            freezable.Tick(3.9f);
            Assert.IsTrue(freezable.IsFrozen, "re-freezing restarts the 4 s");
            freezable.Tick(0.2f);
            Assert.IsFalse(freezable.IsFrozen);
            Assert.IsTrue(lava.activeSelf);
            Assert.IsFalse(platform.activeSelf);
        }

        [Test]
        public void WaterShieldCastExtinguishesAndFreezesNearbyOnly()
        {
            var player = Make("Player", Vector2.zero);
            var health = player.AddComponent<Health>();
            var skillObject = Make("Shield", Vector2.zero);
            skillObject.transform.SetParent(player.transform, false);
            var skill = skillObject.AddComponent<WaterShieldSkill>();
            skill.Bind(null, health);

            var trap = NewGate<ExtinguishableGate>(out _, new Vector2(1.5f, 0f));
            var farTrap = NewGate<ExtinguishableGate>(out _, new Vector2(8f, 0f));
            var lavaRoot = MakeBox("Lava", new Vector2(-1.5f, 0f), new Vector2(2f, 2f), true);
            var freezable = lavaRoot.AddComponent<LavaFreezable>();

            skill.Cast();

            Assert.IsTrue(trap.IsOpen, "fire trap within 2.5 tiles is extinguished");
            Assert.IsFalse(farTrap.IsOpen);
            Assert.IsTrue(freezable.IsFrozen);
        }
    }
}
