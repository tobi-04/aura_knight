# Review — phases 1, 3, 4, 5, 6 (2026-10-05)

Score 6.5/10 · read-only review, no Unity run. 2 critical · 12 warnings · 8 suggestion groups.

## Critical
- **C1** Respawn fails across regions → revive in place (death loop). `World/SunAltar.cs:14,32-40`, `World/CheckpointService.cs:63-73`, `Player/PlayerCombat.cs:137-141`. Fix: altarId→region map, RegionLoader.SetCurrentRegion + await load, never revive in place.
- **C2** No resume-from-save flow (`GameManager.Continue()` only flips state). Fix: `WorldEntry.Resume(GameState)` loads region of lastAltarId, spawns at altar, enters room.

## Warnings
- W1 `FileSaveStorage.cs:25` File.Replace unverified on Android, fallback File.Move cannot overwrite, no .bak; `HasSave` true for corrupt file.
- W2 Init order undefined (`PlayerStats.cs:23` reads GameManager in Awake); Continue/NewGame do not refresh PlayerStats/AuraManager → add DefaultExecutionOrder + GameStateLoaded event.
- W3 `RegionLoader.cs:51-77` no unloading set → double unload / load during unload.
- W4 `Haptics.cs:33` Handheld.Vibrate too long; use VibrationEffect one-shot; cache setting.
- W5 Pause/Dead not respected: AuraManager casts/switches while dead/paused; input latches while paused.
- W6 `HitStopTimer.cs:37` pause during hitstop gets lifted; editor stale Runner with domain reload off.
- W7 No physics layers; motor/probe masks = everything.
- W8 Respawn warp bypasses camera OnTargetObjectWarped.
- W9 `AuraManager.Unlock` not persisted to disk itself; ordering vs BossDefeated.
- W10 GameManager duplicate still subscribes in OnEnable; inconsistent singleton conventions.
- W11 `PlayerAuraBinder.cs:110-114` water count reset on respawn breaks swim if altar inside water.
- W12 SunAltar re-entry writes save every enter.

## Validated items
- Kinematic vs static trigger callbacks: refuted (works).
- Fireball DamageInfo.Source = projectile: confirmed (low impact) → Hitbox Source setter.
- Sword disabled while swimming: confirmed (design decision).
- Confiner "blend" via damping: partially confirmed, visible pop.
- Room transition keep-velocity / respawn zero: verified correct.
- AuraId strings, openedGates wiring: consistent; add validator rule Burn/Extinguish gates need PersistentId.

## Suggestions (summary)
EventBus GetInvocationList alloc per publish; SwipeGesture alloc per touch; GetComponent<PlatformEffector2D> per hit (use usedByEffector); SyncTransforms ×3 per fireball; LightDropPickup GetComponentInParent in Stay; fireball dies on one-way platforms; Room.Deactivate during blend; AuraGate split per interaction; PlayerCombat split; unity-batch.sh `set -u` no-arg + stale lock; LFS patterns (tga, psb, flac, m4a, aif, webp, exr, fbx), `* text=auto`, document unityyamlmerge; Release build should fail without custom keystore.

## Unresolved
File.Replace on Android IL2CPP; trigger re-fire on collider resize; active scene for instantiated fireballs; attack while swimming (design).
