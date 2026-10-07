# Reviewer final: f89a05d..e0623d5 (phases 2, 7-13 + integration)

Depth: targeted read of runtime scripts on the focus list (Core save, Bosses, World gates/seals/chests, Pickups, Progression, UI HUD/Shop/Map, Light budget, Haptics), BuildScript, tools/unity-batch.sh, LICENSES vs contents. Generators reviewed for risk only (no destructive calls besides one DeleteAsset of a half-written font). No Unity run by me; tests not re-run.

Score 8/10. Decision SEALED (0 critical). Verdict JSON: evidence/inspection-verdict.json.

## Critical
None.

## Warning
1. OneTimeAuraGate.cs:45, Shortcut.cs:48. Gate/shortcut open is marked in memory only; AuraSeal/SealGate/TreasureChest/Shop all call Save. Lost if the process dies without OnApplicationPause. Fix: `gm.Save()` after MarkGateOpened / MarkShortcutOpened.
2. BossVictorySteps.cs:20-28. AuraManager null -> reward skipped, boss still marked defeated: boss absent forever, Aura (and its gates) unobtainable. Fix: UnlockReward returns bool; BossVictorySequence skips MarkDefeated/Publish when the reward could not be granted (or retries).
3. GameManager.cs:57-60 + MainMenuScreen.cs:56-64. New Game sets stateSaved=false but OnApplicationPause saves any mode but Menu: backgrounding right after New Game (before first altar) overwrites the Continue slot with a fresh state; no overwrite confirmation. Fix: skip pause-save while the state is a never-saved new game; confirm New Game when a save exists.

## Suggestion
- LightBudgetController.cs:65-71: FindObjectsByType allocation each 0.5 s, O(n^2) Contains. Use a Light2D registry.
- SummonSpiderlingsAttack.cs:32-45: summoned spiderlings drop coins and are re-summoned: coin farm in the fight. Zero their drop table.
- BossBase.cs 206 lines, MapScreen.cs 201 (standard 200). BossBase.Spawns.cs:37 has two comments jammed on one line.
- BuildScript.cs:42-43: keystore passwords are session-only in Unity, so batch release always refuses; read env vars, bump versionCode.
- unity-batch.sh: no timeout for compile/test/exec; stale Logs/test-results.xml summary printed if run dies early. Exit code logic itself is correct (code=$? right after case; shot via subshell; 1 on CS errors; 64/75 usage/busy).
- PauseController: no auto-pause when app backgrounds.
- Licensing: contents match LICENSES.md (27 wav, 12 ogg generated, 5 OFL fonts, one CC0 Kenney pack, credits aggregate them). Not listed: Art/UI pngs, KeyArt jpgs, TMP LiberationSans.ttf; no repo LICENSE; confirm rights to publish `docs/reference/Tai_Lieu...v2 (1).pdf` and key art.
- Plan checklists in phase files still unticked and plan.md status still `pending` for phases 2, 7-13 (docs drift).

## Checked and fine
- Ordering: Aura unlock+save before MarkDefeated/BossDefeated; interruption between them is safe (re-fight returns Unlock false).
- Corrupt/empty/wrong-version/unreadable saves never throw; .bak recovery; Normalize repairs nulls; PlayerStatsSeed clamps hearts/energy/sword; Wallet clamps negatives.
- EventBus Subscribe/Unsubscribe symmetric in all runtime files; snapshot delivery tolerant.
- Menu return loads MainMenu in single mode, so New Game cannot see stale gates/seals from a prior session; BurnableGate/Seals restore on Start and GameStateLoaded.
- BossHazard pool: Configure resets all state, markers never pooled, Owner check in ClearSpawned, dead entries skipped. CoinPickup pool resets state.
- Coroutines (RoomManager, RegionLoader, CheckpointService) null-guarded; no coroutines in enemies/hazards.
- SmoothWall only affects wall grip (GripsWall), applied by RoomTerrain. Known pogo risk BUG-005 documented.
- Release: no secrets/keystore tracked, release refusal intact, Haptics JNI fallback ok.

## Unresolved
Device-only: fps/RAM, touch feel, background/resume, 3 aspect ratios, Android atomicity of File.Replace, human playtest balance (BUG-004 weak-point x2 open, Castle light 0.15 vs GDD 0.05).

**Status:** DONE_WITH_CONCERNS
