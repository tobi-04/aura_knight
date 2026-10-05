# Phase 5 report: Aura System

Status: DONE_WITH_CONCERNS. Compile clean (no CS errors, no warnings from phase 5 files), EditMode 333/333 pass (256 existing + 77 new), generators re-run (Aura assets, Player prefab, Test_Movement, Test_Aura). Nothing committed.

## Files (under Assets/_Project)
Created
- Scripts/Aura (AuraKnight.Aura): AuraId (+AuraIds), AuraPassives, AuraDefinition, AuraState (pure rules), AuraManager, PlayerAuraBinder, AuraPassiveResolver, AuraVisuals, AuraFlash, AuraSkillBase, AuraInteraction (+IAuraInteractable, AuraInteractionRules), AuraInteractionProbe, AssemblyInfo (InternalsVisibleTo tests)
- Scripts/Aura/Skills: WindGustSkill, FireballSkill, FireballProjectile, WaterShieldSkill, ShieldState
- Scripts/World/Interactables: AuraGate, VentCycle, WindCurrent, LavaFreezable, WaterVolume, OxygenMeter, OxygenTimer
- Scripts/Player/States/SwimState.cs (also holds SwimMath)
- Scripts/Editor/Aura (namespace AuraKnight.Editor, in the existing Editor asmdef): AuraAssetGenerator, AuraDefinitionGenerator, AuraSkillPrefabBuilder, AuraInteractablePrefabBuilder, AuraPlayerPrefabBuilder, AuraTestSceneGenerator, AuraPrefabParts, AuraSerialized
- Data/Auras/{None,Wind,Fire,Water}.asset, Prefabs/Aura/{WindGustSkill,FireballSkill,FireballProjectile,WaterShieldSkill}.prefab, Prefabs/Interactables/{AuraGate_Burn,AuraGate_Extinguish,AuraGate_HeatVent,WindCurrent,LavaFreezable,WaterVolume}.prefab, Scenes/Test/Test_Aura.unity
- Tests/EditMode/Aura (own asmdef AuraKnight.Tests.EditMode.Aura): 9 files, 77 tests

Modified outside the owned folders (all small, additive, needed)
- Player/PlayerController.cs + PlayerController.Api.cs: new `JumpMultiplier` hook (default 1), applied in `StartJump`. Needed for "jump x0.6 in water"; no hook existed.
- Combat/Health.cs: new `Func<DamageInfo,bool> DamageFilter`, consulted after the invulnerability check; returning true gives `HitOutcome.Absorbed`. Needed so the water shield can eat exactly one real hit (i-frames never consume it). Contract of existing members unchanged.
- Scripts/AuraKnight.asmdef: added reference `Unity.RenderPipelines.Universal.2D.Runtime` (Light2D lives there, not in Universal.Runtime). Scripts/Editor/AuraKnight.Editor.asmdef: same reference.
- Editor/Player/PlayerAssetGenerator.cs: one line calling `AuraPlayerPrefabBuilder.Attach(...)`. The builder also sets the player SpriteRenderer vertex colour to white; the Aura tint now comes from a MaterialPropertyBlock `_Color`.

## Public API for later phases
- `AuraManager.Instance` (on the Player prefab; null when no player in scene).
  - `bool Unlock(AuraId id)`: boss reward (phase 8). First real Aura is auto-equipped. Writes GameState.unlockedAuras/currentAura immediately, publishes `AuraUnlocked` (+ `AuraChanged` when it auto-equipped). Returns false for None/duplicates.
  - `TrySwitch(AuraId)`, `TryCycle(+1/-1)` (HUD ring, phase 10; locked or inside 0.3 s cooldown returns false), `TryCastSkill()` (returns `CastResult`), `Current`, `IsUnlocked(id)`, `GetDefinition(id)`, `CurrentDefinition`, `Passives`, static `CurrentPassives`, `State` (AuraState: SwitchCooldownRemaining, SkillCooldownRemaining for HUD cooldown rings).
  - Input is read inside the manager (AuraWind/Fire/Water/Next/Prev, SkillPressed) from `controller.Input`; no UI wiring needed besides the virtual buttons that already exist.
  - Events on EventBus: `AuraChanged{AuraId}` (also published once in Start so late subscribers get the initial state), `AuraUnlocked{AuraId}`; ids are enum names ("Wind", "Fire", "Water", "None"). Parse with `AuraIds.TryParse`.
  - Save/load: automatic from `GameManager.Instance.State` in Start; with no GameManager (test scenes) it uses the serialized `debugUnlocked` list.
- `AuraDefinition` fields for UI: `Id, DisplayName, Color, LightRadius, Passives, SkillPrefab, EnergyCost, SkillCooldown, SwitchSfx` (AudioClip placeholder, empty; audio phase should play it on `AuraChanged`).
- `PlayerAuraBinder` (Player prefab): `Modifiers`, `HeatImmune`, `AcidImmune` (phase 9 acid pools / vents should check these), `InWater`, `ModifiersChanged`, `EnterWater()/ExitWater()` (ref-counted).
- `OxygenMeter.Timer.Fraction` and event `Changed(float 0..1)` for an oxygen bar.
- `WaterShieldSkill.Absorbed` event (feedback/SFX), `IsShielded`, `Remaining`.
- Level design setup (all prefabs in Prefabs/Interactables; every root has a trigger collider the skills probe):
  - Burn barricade (`AuraGate_Burn`): scale/replace the `Blocker` child; give the root a unique `PersistentId` string so the open state persists (stored via GameState.MarkShortcutOpened/openedShortcuts, namespaced by your id string, e.g. "gate_hub_barricade_01"). Fire trap `AuraGate_Extinguish` likewise.
  - `AuraGate` fields: requiredAura, interaction (Burn, Extinguish, WindLift, HeatVent), deactivateOnOpen[], activateOnOpen[], ventHazard (HeatVent only). `StateChanged(bool)` event. Freeze is deliberately only on `LavaFreezable` (4 s; re-freeze restarts it).
  - `AuraGate_HeatVent` pulses a hazard Hitbox 2 s on / 2 s off; harmless while the current Aura has heatImmune (Fire).
  - `WindCurrent`: trigger zone, lifts to 9 u/s only while Wind is the current Aura. `WaterVolume`: trigger zone sized to the pool; keep it tight.
  - Anything else that should react to skills implements `IAuraInteractable.TryInteract(AuraInteraction, AuraId)`; Fireball offers Burn, WaterShield offers Extinguish and Freeze within 2.5 tiles at cast.
- Skills deal damage through ordinary `Hitbox` (Team.Player, 1 dmg gust with 3-tile default knockback, 2 dmg fireball).

## Numbers implemented
None #FFEB9E glow r3; Wind #27D38C r4, 25 NL, double jump (v 20), glide; Fire #FF5C57 r5, 30 NL, speed x1.2, heat immune; Water #27B5F7 r4, 35 NL, swim, acid immune. Switch cooldown 0.3 s, flash 0.2 s. Gust r2.5 / 1 dmg; fireball 12 tiles at 20 u/s / 2 dmg / pool 3-6; shield 6 s, one hit. Wading without Water: speed x0.5, jump x0.6, 8 s oxygen then 1 heart per 2 s (first heart the moment air runs out) via `Health.TakeDamage` hazard damage; with Water: 8-way swim at full speed, no gravity, no oxygen limit, hop out of the surface.

## Tests (77 new, all passing)
Switch cooldown, locked rejection, cycle order/wrap/skip None, first-unlock auto-switch, energy refusal spends nothing, skill cooldown, save/load round trip and bad data fallback, passive mapping per Aura and wading penalties, oxygen timer, shield absorbs exactly one hit through real `Health` and not on i-frame hits, vent cycle, swim 8-way quantising, gate opens only for the matching Aura/interaction (all Aura x interaction combinations), lava freeze 4 s, water shield proximity extinguish/freeze, manager events, visuals colour/radius/flash/zero allocation over 100 switches, and real-physics simulations: double jump reaches 4.5 + 2.72 = 7.2 tiles (+-0.4), no double jump without Wind, fire run 9.6 u/s, wading 4 u/s and 1.6 tile jump, swim 8-way speed 5.66 and no sinking, surface hop, wind current lift, fireball 12.0 tiles in 30 steps / left / damage 2 / stops at walls / burns barricade but not a fire trap / pool reuse, gust radius and one hit per cast. Generated assets and Player prefab/Test_Aura scene wiring are verified as well.

## Concerns / follow-ups
1. Sword attack is not available while swimming (PlayerCombat only starts swings from Idle/Run/Jump/Fall). Skills still work. Decide with design whether swimming sword is wanted; one-line change in `PlayerCombat.TryStartAttack`.
2. Fireball projectile Hitbox reports the projectile root as `DamageInfo.Source`, not Leo. Enemies that need the attacker position should use `Direction`. Easy to fix once enemies exist (phase 7).
3. Light2D needs a 2D Renderer to show; the project renderer and the test scenes use unlit sprite materials, so glow is invisible until the art phase switches to lit materials/2D renderer. Values and API are verified in tests, not visually. PlayMode (real triggers, touch input, hand-off WaterVolume/WindCurrent callbacks, AuraGate presence/vent paths under a live AuraManager) was not run in batch; those trigger callbacks are covered structurally only.
4. AuraGate persistence reuses `GameState.openedShortcuts` (Core not modified). Use gate ids that cannot clash with shortcut ids; a dedicated `openedGates` list would need a Core change and a save-version decision.
5. Skills have no art/SFX/animation (placeholder squares). Gust has no visible ring; shield uses a translucent rectangle.
6. The Player prefab generator ordering: `PlayerAssetGenerator.Generate` now builds Aura assets first (via `AuraAssetGenerator.EnsureAssets`). It overwrites Aura definition numbers each run (GDD authoritative), unlike the movement config.
7. `MovementTestSceneGenerator`/`Test_Movement` have no `debugUnlocked` Auras (all locked), so the movement scene stays Aura-less by design; use Test_Aura.
8. Test assembly needs `UNITY_INCLUDE_TESTS`; FindObjectsByType overloads without sort mode used to avoid obsolete warnings.

Regenerate: `tools/unity-batch.sh exec AuraKnight.Editor.AuraTestSceneGenerator.Generate` (assets, Player prefab, Test_Aura); `...AuraAssetGenerator.Generate` for assets only. Menu: Aura/Aura System/*.

**Status:** DONE_WITH_CONCERNS
**Summary:** Aura system built: data-driven AuraDefinition x4, pure AuraState + AuraManager (switch/cycle/unlock/cast/save), passive binder, swim/oxygen/water, Wind/Fire/Water skills, AuraGate/WindCurrent/LavaFreezable/WaterVolume, visuals, generators and Test_Aura. Compile clean, EditMode 333/333.
**Concerns/Blockers:** Additive edits to Health (DamageFilter), PlayerController (JumpMultiplier) and two asmdefs (Light2D ref); no sword while swimming; Light2D glow and trigger callbacks not verified in PlayMode.
