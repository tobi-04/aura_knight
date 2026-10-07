# Phase 11 Audio - implementer report (2026-10-07)

**Status:** DONE_WITH_CONCERNS

## Sources and licenses
All audio is generated and project-owned (CC0-only rule satisfied; nothing downloaded). Recorded in `Assets/_Project/Audio/LICENSES.md`.
- SFX: `tools/audio/generate_sfx.py` (numpy, fixed seeds, 27 mono 44.1 kHz WAVs, 1.1 MB total).
- BGM: `tools/audio/generate_bgm.py` (numpy + ffmpeg native vorbis, 32 kHz stereo OGG, 6.5 MB total, largest file 726 KB).
- Why not OpenGameArt: each region needs two layers with identical tempo/length/chords for a synced crossfade; no CC0 packs ship matching stems. Not searched in depth; if you want human-composed music later, add a row per source to LICENSES.md.
- Regenerate: `python3 tools/audio/generate_sfx.py && python3 tools/audio/generate_bgm.py && tools/unity-batch.sh exec AuraKnight.Editor.AudioAssetGenerator.GenerateAll`.

## What was built (namespace AuraKnight.Audio, Scripts/Audio)
AudioManager (pool 12, voice stealing, 40 ms per-id throttle, pitch +-5%), Sfx (static API), SfxLibrary/SfxEntry (SO), AudioEventListener, PlayerSfxProbe, MusicLayerController (+MusicDeck, CrossfadeValue, MusicSync, CombatIntensityTracker), RegionMusic (SO), AudioVolumeSettings, pure helpers (AudioMath, ClipPicker, SfxThrottle, VoiceSelector, SfxEventMap, FootstepTracker, LandingDetector).
Editor (Scripts/Editor/Audio): AudioMixerWriter (writes AuraKnight.mixer text deterministically: Master -> Music/SFX/UI, exposed MusicVolume/SfxVolume/UiVolume), AudioImportPostprocessor (SFX mono ADPCM decompress-on-load; BGM mono Vorbis streaming; Android override; menu Aura/Audio/Reimport), AudioAssetGenerator, AudioSceneGenerator, PlayerSfxProbeInstaller.
Data: Data/Audio/SfxLibrary.asset, RegionMusic_{hub,forest,cave,city,castle,boss,ending}.asset. Core.unity got one new root object "Audio" (AudioManager, AudioEventListener, MusicLayerController); nothing else in Core touched.

## Public API for other systems
`Sfx.Play(SfxId id, Vector3? pos = null)` (no-op when no AudioManager). Position gives falloff/pan; null is 2D.
SfxIds (append-only, serialized): None, Footstep, Jump, Land, Dash, Slide, WallSlide, SwordSwing, SwordHit, PlayerHurt, PlayerDie, AuraWind, AuraFire, AuraWater, AuraUnlock, SkillWind, SkillFire, SkillWater, Coin, Chest, Altar, UiTap, UiBack, BossRoar, EnemyHit, EnemyDie, Respawn.
- Enemies/bosses should call: EnemyHit, EnemyDie, BossRoar (with position). UI: UiTap, UiBack (routed to the UI mixer group). Chest: call `Sfx.Play(SfxId.Chest)` from the chest pickup (no chest code exists yet).
- Music: `MusicLayerController.Instance` -> `PlayBoss()`, `PlayEnding()`, `ReleaseOverride()` (back to region track), `StopMusic()`, `SetCombatIntensity(0..1)` (only effective with `AutoCombat = false`; auto mode rescans every 0.5 s and wins). Boss fight code should call PlayBoss on start and ReleaseOverride on BossDefeated; Ending screen calls PlayEnding. Track ids: hub, forest, cave, city, castle (RegionId of rooms), boss, ending.
- Settings: `AudioVolumeSettings.Set(music, sfx)` saves PlayerPrefs "settings.musicVolume"/"settings.sfxVolume" (default 0.8) and applies; `AudioVolumeSettings.Apply()` re-reads prefs (also called in AudioManager.Start). UI sounds follow the SFX slider.

## Event wiring
EventBus -> AudioEventListener: PlayerDamaged, PlayerDied, PlayerRespawned, AuraChanged (first value and unlock-frame switches silent), AuraUnlocked, EnergyChanged drop = skill cast (per current Aura; only skills spend energy), CoinsChanged increase, CheckpointReached (altar), GameStateLoaded resets trackers. RoomEntered -> MusicLayerController.
PlayerSfxProbe (no Player/Combat/Aura edits): state changes (Jump, WallJump, Dash, Slide, WallSlide, Attack/AirAttack = SwordSwing), double jump via `DoubleJumpUsed`, land via `Grounded` + 0.12 s air time, footsteps by distance (1.4 u stride, Run state), SwordHit via child Hitbox.Hit events. "Sword miss" is the swing sound itself (played at swing start; no separate miss id).

## ACTION for the orchestrator: Player generator
The Art agent's Player prefab regeneration removed the probe once during this phase. After the Player generator runs, call `AuraKnight.Editor.PlayerSfxProbeInstaller.Apply()` (idempotent, additive; or `tools/unity-batch.sh exec AuraKnight.Editor.PlayerSfxProbeInstaller.Apply`). I re-applied it at the end; the prefab currently has the component. `AudioAssetGenerator.GenerateAll` runs assets + Core object + probe in one go.

## Verification
- compile: clean. EditMode: 590/590 passed (final full run; includes 84 audio tests: dB conversion, pitch bounds, clip picker, throttle, voice stealing, library lookup, crossfade math, loop sync, combat linger, event mapping, trackers, generated mixer/library/import settings/BGM sizes/layer lengths).
- PlayMode audio: 14/14 (pooled SFX, 12 voices all busy with none lost, throttle, EventBus->SFX, room->track switch with crossfade, combat mix 1 s, auto combat via real Enemy-layer collider within 8 u, boss override/release, volume dB in the mixer, probe state sounds, probe unsubscribe).
- Last full PlayMode run (before one probe-test fix) had 4 failures: 3 in `EnemyBehaviourPlayModeTests` (Enemies agent, in progress) and my probe unsubscribe test, which I fixed and re-ran green. One earlier full PlayMode run aborted with a Unity test-runner NRE (PlayModeRunTask) while other agents were compiling; a rerun was fine. I did not get a fully green full-suite PlayMode run because of the enemy tests; Audio + World tests pass.
- One EditMode failure seen mid-run (`GeneratedAssetsTests.PlayerPrefabHasWiredComponents`, Animator on Leo) came from the Art agent's concurrent prefab work and was green on the final run.

## Sizes / RAM
SFX 1.1 MB on disk, about 1.2 MB decompressed in RAM (test caps at 8 MB). BGM streamed (about 4 decks x small buffers). Far below the 40 MB target.

## Concerns / follow-ups
1. Music and SFX are synthesized chiptune; nobody has listened on device. Needs an ear pass (P13) and likely swaps for hand-made or CC0 tracks; replacing a file with the same name keeps all wiring. LUFS target approximated by RMS (-20 dBFS), not measured.
2. ffmpeg's native vorbis encoder ignores bitrate; Unity re-encodes at import anyway (quality 0.4).
3. `CheckpointReached` also fires when the world spawns Leo on an altar, so the altar chime plays at game start/respawn; suppress in the listener if it annoys.
4. The mixer file is hand-written YAML (no public Unity API); verified by loading it in Unity and driving exposed params in PlayMode. EditMode cannot SetFloat on a mixer, so that test checks exposed names only.
5. Music pauses are not tied to GameMode (music keeps playing while paused); Core scene must contain exactly one AudioListener (camera).
6. Files: AudioPlayModeTests.cs is 212 lines (test file); all runtime files <= 182 lines.

**Summary:** Phase 11 audio is in: generated SFX/BGM, mixer, pooled AudioManager with the `Sfx.Play` API, event listener, player probe, dynamic region music with 1 s combat crossfade, settings API, import rules, tests.
**Concerns/Blockers:** Generated (not composed) audio needs on-device listening; Player generator must call `PlayerSfxProbeInstaller.Apply()`; enemy PlayMode tests from another agent were red in my last full run.
