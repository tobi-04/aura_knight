# Review: BUG-004 fire-only weak point, New Game confirm, LICENSE, credits

Score 8/10. No critical findings.

## Critical
None.

## Warning
1. Nothing tests that FireballProjectile.Launch sets Fire. Assets/_Project/Scripts/Aura/Skills/FireballProjectile.cs:41.
   BossTestKit.Strike (Tests/PlayMode/Bosses/BossTestKit.cs:102) sets hitbox.Kind itself, and no Aura test mentions DamageKind. Deleting that one line leaves every test green, while the boiler silently takes x1 from the real Fireball.
   Fix: in Tests/EditMode/Aura/FireballReviewTests.cs, or a PlayMode test, launch a real pooled fireball at the boiler and assert 50-4. Or assert via hitbox.Hit / Health.Damaged that info.Kind == Fire after Launch.
2. docs/qa/bug-log.md:23 still says BUG-004 is "Mở, không sửa" and says Kind does not exist. Update the status to fixed, note the scope (Rogue Machine boiler is fire-only, Root Tree core is still x2 for any hit, per GDD ambiguity), and flag that the team should confirm the Root Tree rule.
3. Assets/_Project/Prefabs/UI/MenuScreens.prefab: 15k-line diff (+8486/-6810). Probably regenerated fileIDs, which is expected for a generated prefab, but it cannot be reviewed.
   Fix: commit it separately from the code change and confirm in the PR that it is only a regeneration.

## Suggestion
1. Assets/_Project/Scripts/UI/Screens/ConfirmDialog.cs:46-50 (HandleBack). It returns true even when `pending == null`, so Answer exits early without removing the dialog. If a dialog is ever shown by a path other than Ask, Back is swallowed forever.
   Fix: `if (pending == null) return false;` (the stack then pops it), or call router.Remove(this) before the null check.
2. Back during the cancel/confirm hide tween. After router.Remove the stack depth is 1, so MainMenuScreen.HandleBack (MainMenuScreen.cs:93) calls Application.Quit() for about 200 ms. A double Back, or a Back right after Cancel, quits the app. Same hazard as any popup, but it is easy to hit here.
   Fix: have MainMenuScreen.HandleBack return true without quitting while `confirm != null && confirm.IsTransitioning`.
3. Double-subscription. MainMenuScreen.cs:73-75 does `intro.Completed += OnIntroCompleted` before router.Push. Two taps in one frame (no save, so no dialog) subscribe twice, and launch(true) runs twice. This already existed. Fix: `-=` before `+=`, or check the Push result.
4. Weak spot Kind semantics. `WeakPointMath.Scale(amount, mult, kind, fireOnly)` is fine and pure. Add a PlayMode assertion that WeakPointHurtbox forwards Kind through the stand-in Health (WeakPointHurtbox.cs:63), since a regression there would only show up as a lost x2 on the boiler.
5. The Strings_vi.json "no newline at EOF" change is harmless noise.

## Edge cases checked

BUG-004
- **Pooling/reuse:** Kind is a non-serialized auto-property, set on every Launch, and the fireball is always Fire. No stale-Kind risk.
- **Other sources:** other hitboxes (sword, enemies, hazards, Malakor fire strike) stay General. The Malakor fire strike hits the player, so it does not matter here.
- **DamageInfo construction sites:** only three non-test sites exist. Hitbox.BuildInfo passes Kind, WeakPointHurtbox forwards info.Kind, and OxygenMeter (General) is fine. No site loses Kind.
- **Prefabs:** RogueMachine fireOnly:1 and RootTree fireOnly:0 match the intent. BossAttackSetup sets the flag, and GeneratedBossAssetsTests pins both values.
- **Scale semantics:** the stand-in takes `applied` damage, scaled by 1x for non-Fire hits on a fire-only point. Correct.

New Game confirm
- **Double tap:** the dialog's Group.interactable is false during the hide tween, and the `pending` null-guard blocks a second answer. A second NewGame tap while the dialog is up cannot happen, because the dialog blocks raycasts. If it did happen, Push returns false and the callback is simply replaced.
- **Pop before push:** Answer calls router.Remove(this) before callback(), so the stack is [Menu] when the intro is pushed with hideBelow. The stack is correct, and the test asserts `!router.Contains(confirm)` and Depth==1.
- **Awake listeners:** screens start inactive, so ConfirmDialog.Awake and MainMenu.Awake run on first Show. Ask sets pending before Push, which is safe. The serialized router is bound by the builder.
- **Save probe:** it is evaluated at tap time, which is correct.

LICENSE
- MIT text is standard. All cited paths exist: Art/LICENSES.md, Audio/LICENSES.md, Art/Fonts/LICENSES.md, docs/reference/ (two PDFs), tools/, Scripts, Tests.
- Fonts note: it also correctly excludes Unity's TMP Liberation font, which falls under the Fonts clause.
- Remaining gap: files under `Assets/TextMesh Pro/` (Unity-licensed) are not mentioned. Optional.

CreditsText.txt
- Diff matches Art/LICENSES.md: scope line, UI row, key-art section dated 2026-10-08, TMP Liberation font section, and the OFL text. The cited file `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` exists. Consistent.

## Do the tests catch regressions?
- **BossLogicTests.AFireOnlyWeakPointDoublesOnlyFireDamage:** the 4 cases cover the whole truth table of the pure function. Catches an inverted fireOnly or Kind check.
- **GeneratedBossAssetsTests:** pins the prefab flags, so a regenerated prefab that drops fireOnly will fail. Good.
- **BossArenaPlayModeTests.BoilerTakesDoubleDamageFromAFireball:** exercises Hitbox.Kind through Hurtbox to WeakPoint to Health end to end (Fire x2, General x1). Weak only because of Warning 1 (the Fireball itself is untested). It also dropped the old body-hit assertion, which is acceptable.
- **MainMenuPlayModeTests.NewGameOverASaveAsksFirstAndCancelKeepsTheMenu:** covers cancel, Back, confirm, stack depth, dialog removal, intro, and launch. Missing: double Confirm tap, Back during the tween (Suggestion 2), and the no-save path (covered by the existing test, whose hook was correctly changed to `() => false`).
- **Rating:** good, with one real hole (the Fireball Kind assignment).

## Done well
- Minimal, backward-compatible API: optional ctor param, runtime-only Kind, a pure testable function.
- ConfirmDialog is small and self-guarding, with a single-callback null guard.
- Credits and licence data stays in sync, and the tests were updated to match behaviour rather than weakened.
- File sizes are all under 200 lines except the existing test files (BossArenaPlayModeTests 269, MainMenuPlayModeTests 206), which are slightly over the standard.

## Actions in order
1. Add a Fireball-launch-sets-Fire test (Warning 1).
2. Update bug-log BUG-004 (Warning 2).
3. Split the prefab regeneration from the code commit (Warning 3).
4. Apply Suggestions 1-3.
5. Consider splitting the two test files that exceed 200 lines.

## Numbers
- Not run (Unity is in use by the tester). Lint and coverage not measured.

## Unresolved
- Does the Root Tree core need to be fire-only too? The GDD is silent. Needs a design decision.

```json
{ "score": 8, "criticalCount": 0, "decision": "REWORK",
  "acceptanceCovered": ["BUG-004 boiler fire-only", "Kind survives pooling and forwarding", "New Game confirm flow", "LICENSE paths exist", "CreditsText matches LICENSES.md"],
  "regressionChecked": ["DamageInfo construction sites", "router stack order", "Back during hide tween", "prefab flags"],
  "contractStatus": "OK", "refuted": [], "unproven": ["FireballProjectile.Launch sets Kind=Fire (no test)"], "reachableRegressions": [],
  "findings": [
    { "severity": "Warning", "category": "Tests", "location": "Assets/_Project/Scripts/Aura/Skills/FireballProjectile.cs:41", "summary": "no test covers Launch setting DamageKind.Fire", "disposition": "Accept" },
    { "severity": "Warning", "category": "Docs", "location": "docs/qa/bug-log.md:23", "summary": "BUG-004 still marked open", "disposition": "Accept" },
    { "severity": "Suggestion", "category": "Logic", "location": "Assets/_Project/Scripts/UI/Screens/ConfirmDialog.cs:46", "summary": "HandleBack swallows Back when pending is null", "disposition": "Accept" },
    { "severity": "Suggestion", "category": "Logic", "location": "Assets/_Project/Scripts/UI/Screens/MainMenuScreen.cs:93", "summary": "Back during dialog hide tween quits the app", "disposition": "Accept" }
  ] }
```
