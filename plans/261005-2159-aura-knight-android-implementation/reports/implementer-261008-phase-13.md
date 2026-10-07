# Phase 13 report: QA, optimization, release readiness (2026-10-08)

Status: DONE_WITH_CONCERNS. Nothing committed. Nothing device-side was run.

## Verification
| Check | Result |
|---|---|
| `compile` | 0 errors, 0 warnings |
| `exec RegenerateAll.Run` | clean ("Validators clean (34 room(s))"), run after generator changes |
| `test EditMode` | 963 passed / 0 failed (was 948) |
| `test PlayMode` | 167 total: 165 passed, 0 failed, 2 skipped (both `[Explicit]` GPU screenshot tests; was 154 + 1) |
| `exec BuildDevelopmentApk` | Succeeded, `Builds/Android/AuraKnight-dev.apk` = 48.5 MB (50,808,471 bytes), 0 errors |
| `exec BuildReleaseApk` (no keystore) | "Release build refused: no custom keystore selected...", exit=1, no APK written |

## What changed
- docs/qa: test-cases (GDD 15.1 mapped to test names), bug-log (BUG-001..008), playtest-m1/m2/m3, device-checklist. Manual rows all "Chua chay".
- Gap-fill: Health, Wallet, ShopService, SaveSystem (round-trip, corrupt, .bak), AuraManager already existed (mapped, none duplicated). Added the two GDD 15.1 cases with no test: 50 room crossings, app to background save.
- Runtime screenshots: `RuntimeRoomScreenshotTests` (hub_01, forest_03, cave_01, city_02, castle_03, forest_boss, castle_boss).
- Optimization: `BossHazardPool` (+ `BossHazard.Spawn/Release/Owner`), `LightBudget`/`LightBudgetController` (8 lit local lights, 4 in power saving), Boot honours stored power saving (`GameSettings.ApplyFrameRate`), vsync 0 in all quality levels. Atlases and Fireball/coin pools already existed; no UI atlas (6 sprites, two filter modes).
- AudioListener on the Core main camera via `WorldSceneGenerator`; guard stays.
- Release: 1.0.0 / code 1, Development Build off, `BuildScript.ReleaseSigningProblem` pure + tested, README "Cai APK" + QA sections, counts updated. Credits already built from the three LICENSES.md (Kenney CC0, OFL fonts, generated audio): reused, 181-line CreditsText.txt.
- Castle global light 0.05 -> 0.15 (see below).

## Screenshots viewed (1920x1080, camera follows Leo, 2.5 s settle)
- hub_01: bright, readable; parallax pillars share the ground's olive tone (BUG-006).
- forest_03: readable, teal night; trunks behind platforms, correct layering.
- cave_01: dark but readable, platforms and ledge visible by their rim colour.
- city_02: readable, blue-grey; patrol robot and falling-rain/steam particles visible.
- castle_03 / castle_boss at 0.05: near black, platforms invisible. Raised to 0.15: platforms, walls, chains, pillars and the chandelier readable, still the darkest region. Leo has his own glow. GDD 10 still says 0.05.
- forest_boss: arena readable. Placeholder props (orange fire-trap block, yellow altar) read as objects.
- No parallax layer covers gameplay; foreground teeth only frame top and bottom edges. No missing tiles seen.
- HUD shots: gold rings visible on JUMP/ATK/DASH/SKILL (BUG-008 not reproducible).

## Honest notes
- TDD: ReleaseConfigTests were RED first (2 of 6 failed before setup). The pool, light budget, listener and lifecycle tests were written after the code (they pass; not seen red).
- Not fixed: WeakPointHurtbox x2 on all damage (needs a source tag in DamageInfo), solver pogo, placeholder props/hazards, hub parallax tone.
- Edited one existing test (`PresentationPlayModeTests` castle intensity, renamed `EachRegionLightHasItsConfiguredIntensity`) and `RegionLightingTableTests` for the new value.
