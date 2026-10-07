# Phase 10 report: UI Screens and HUD (2026-10-07)

Status: DONE_WITH_CONCERNS. Nothing committed. Visuals were never rendered (batch Unity runs `-nographics`), so look and layout are verified by structure and tests only.

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | 0 errors, 0 warnings |
| `test EditMode` | 647 passed / 0 failed (57 are new UI tests) |
| `test PlayMode` | 82 passed / 0 failed (31 are new UI tests) |
| `exec AuraKnight.Editor.UiGenerator.GenerateAll` | run twice, idempotent |
| `exec AuraKnight.Editor.BuildScript.BuildDevelopmentApk` | Succeeded, 47.4 MB |

## What exists
Runtime `Scripts/UI` (namespace `AuraKnight.UI`):
- Core: `UITheme` (SO, all GDD 9.1 tokens, 6 px accent bar, 4 px card border, mono +15%, 200 ms / 16 px / 0.95), `ThemedText` / `ThemedImage` / `ThemedAccentBar`, `UITween`, `UIScreen`, `ScreenStack` (pure), `UIRouter`, `Localization` + `StringTable`, `GameSettings`, `SettingsKeys`, `UiEvents`, `PauseGate` (pure) + `PauseController`, `UIButton`, `PressScale`, `MinTouchTarget` + `TouchTargets`, `CoverFit` + `CoverMath`, `GameLauncher` + `CoreLauncher`.
- `Build/UiFactory`: runtime builders shared by the editor generators and `VirtualControlsBuilder`.
- `HUD/`: `HudController`, `HeartsView`, `EnergyBarView`, `CoinsView`, `BossHealthBarView`, `AuraRingView`, `AuraButtonView`.
- `Screens/`: Splash, MainMenu (+ `MainMenuFlow`), IntroCutscene (+ `TypewriterSequence`), Pause, Settings, AuraInfo, AuraUnlockPopup, BossIntroBanner, GameOver, Credits (+ `CreditsDragProbe`), `GameUiDirector`, `HudInput`.
- `VirtualControls/`: `VirtualControlsBuilder` now builds themed TMP labels, a pixel padlock, `AuraButtonView` x3 under an `AuraRingView`, a CanvasGroup and `VirtualControlsStyler`. Public signature `Build(Sprite, Font)` kept (font ignored); new `Build(Sprite)`. Canvas stays 960x540 on purpose (see concerns). Existing prefab tests still pass.

Editor `Scripts/Editor/UI` (new asmdef `AuraKnight.Editor.UI`): `UiGenerator` (menu `Aura/UI/Generate All`), font/sprite/theme generators, `ScreenParts` + one builder per screen group, `HudPrefabBuilder`, `ScreensPrefabBuilder`, `UiSceneGenerator`, `CreditsTextBuilder` (also an `IPreprocessBuildWithReport`, so every build refreshes the credits).

Assets: `Art/Fonts` (5 TTF, 5 Dynamic TMP SDF assets, `LICENSES.md` with OFL), `Art/KeyArt` (hero + 3 aura JPGs extracted with `pdfimages`, slide 11 map not extracted: phase 12), `Art/UI` (generated heart, sun, white, gradient PNGs), `Data/UI/Resources` (`UITheme.asset`, `Strings_vi.json` 91 keys, `CreditsText.txt`), `Prefabs/UI` (`Hud`, `GameScreens`, `MenuScreens`, `VirtualControls`), `Scenes/MainMenu.unity`, and `UI_Root` inside `Core.unity` (the only Core object touched).

## Screens and flow
- Splash (once per app session, tap skips) -> Main Menu (slide 1: navy panel, gold bar, `KỴ SỸ ÁNH SÁNG`, title, 2x2 buttons, hero art with a gradient edge; TIẾP TỤC disabled unless `SaveSystem.HasSave`).
- TRÒ CHƠI MỚI -> Intro (4 typed cards, tap advances, BỎ QUA) -> `GameLauncher.Begin(true)` loads `Core` (single mode); `CoreLauncher` in Core consumes the request and calls `WorldEntry.StartNewGame()` / `Continue()`; on failure it returns to the menu. Pause > VỀ MENU saves and loads `MainMenu` (unloads Core and regions).
- HUD shows only while `GameMode.Playing`. Pause (also Android Back with nothing open, or gamepad Start) sets `GameMode.Paused` and `timeScale 0`; resume restores `HitStop.GameplayScale` captured at pause (PlayMode test with a hit-stop in progress).
- Back = Escape key (Android Back in the Input System): the top screen handles it, else it pops; with an empty stack in gameplay it opens Pause; on the root menu it quits.
- Loading cover while `GameMode.Loading`; Game Over on `PlayerDied` until `PlayerRespawned`; Aura unlock popup (pauses, queues several); boss banner; ending + credits on `GameCompleted`.

## Events defined in `Scripts/UI/UiEvents.cs` (for other agents)
- Boss agent publishes: `BossEncounterStarted{BossId, DisplayName}` (banner `BOSS / NAME` + HP bar, coloured by the last `RoomEntered` region), `BossHealthChanged{BossId, Current, Max}` (ignored unless the id matches the active encounter), `BossEncounterEnded{BossId, DisplayName}` (hide bar; publish on defeat and on player death).
- Progression publishes `GameCompleted` after the final boss: opens ending + credits, whose button returns to the menu.
- UI publishes: `MapRequested` (MAP button or gamepad Select; phase 12 listens), `SettingsChanged{Key}`, `PauseStateChanged{Paused}`.
- UI consumes: `HeartsChanged`, `EnergyChanged`, `CoinsChanged`, `AuraChanged`, `AuraUnlocked`, `GameStateLoaded`, `PlayerDied`, `PlayerRespawned`, `RoomEntered`.

## SettingsKeys (PlayerPrefs)
`settings.musicVolume` and `settings.sfxVolume` (float 0..1, default 0.8; Audio reads these and can also listen to `SettingsChanged`), `settings.haptics` (int, same key `Haptics` reads, written through `Haptics.SetEnabled`), `settings.buttonScale` (0.8..1.3), `settings.buttonOpacity` (0.3..1, default 0.6), `settings.powerSaving` (int; 30 fps vs 60), `settings.language` (string, `vi`). All typed, clamped and NaN-safe through `GameSettings`. Button scale/opacity apply live to the on-screen buttons.

## How phase 12 adds Shop and Map
1. Add `ShopScreen : UIScreen` / `MapScreen : UIScreen` (override `HandleBack`/`OnShowing` if needed); build them in a new editor builder using `ScreenParts.NewScreen<T>` (shell, safe area, footer, header, `Button`, `Text`, `Bar`, `KeyArt`) and the card style (`ThemedAccentBar` card border, mono gold price).
2. Instantiate them in `ScreensPrefabBuilder.BuildGameScreens` under the same canvas (later sibling = on top), then `UIRouter.Push(screen)`.
3. Map: subscribe to `MapRequested` in `GameUiDirector` (or its own always-active listener) and push the screen. Remember `PauseController.Pause(false)` if the screen should freeze the game.
4. New strings go into `Strings_vi.json`; the prefab test fails if a `ThemedText` key is missing.

## Deviations and concerns
1. No visual check. Layout numbers follow the slides on a 1920x1080 canvas; open the Game and MainMenu scenes in the editor at 16:9, 19.5:9, 21:9 and a notch simulator and adjust `Scripts/Editor/UI/*Builder.cs` (then rerun the generator).
2. `Assets/TextMesh Pro` (outside my folders) was added: TMP needs its Essential Resources and `AssetDatabase.ImportPackage` does not work in batch mode, so I unpacked the package's own `TMP Essential Resources.unitypackage` by script. TMP Settings default font is now Be Vietnam Pro. LiberationSans and the shaders are part of it.
3. `UITheme.asset`, `Strings_vi.json`, `CreditsText.txt` live in `Data/UI/Resources/` (not `Data/UI/`) so runtime code and the legacy-signature `VirtualControlsBuilder` can find them without serialized references.
4. VirtualControls keep a 960x540 Canvas Scaler (not 1920x1080): button sizes are written in dp-like units and an existing test asserts 96 and >= 64. Everything else uses 1920x1080 match 0.5. Menu buttons are 150 units tall and `MinTouchTarget` grows any tappable rect to 64 dp on the real device.
5. `PlayerAssetGenerator` (not mine) re-bakes `VirtualControls.prefab` through the legacy `Build(circle, font)` overload; it now produces the themed TMP version too, so rerunning it is safe.
6. GameOver "HỒI SINH TẠI BÀN THỜ" is a status line, not a button: respawn is automatic (`CheckpointService`).
7. TRÒ CHƠI MỚI does not ask for confirmation when a save exists (the first altar overwrites it). Follow-up: add a confirm popup.
8. English is structure only: `Localization.SupportedLanguages = {"vi"}`; add `Strings_en.json` and the code there. The language row cycles through supported languages (one today).
9. Aura ability texts for Fire and Water in `Strings_vi.json` are written from GDD section 6 (slides only cover Wind); review wording.
10. Fonts atlas pre-filled with ASCII + Vietnamese at 64 pt sampling; Display titles at 150-210 px are SDF-scaled, check crispness on device.
11. `Haptics`/`Audio` consumers were not touched; the Audio agent only needs the two PlayerPrefs keys above.
12. Docs (`docs/system-architecture.md`, `code-standards.md`) were not updated (outside my ownership): the UI module section, `GameLauncher` menu flow and the new asmdef `AuraKnight.Editor.UI` need a line each.
13. The PlayMode test `NewGameFromTheMenuLoadsCoreAndEntersTheWorld` and the pause tests use the default save path; the menu tests back up and restore `save_0.json` and the settings prefs.

# Visual fixes (follow-up, 2026-10-07)

Runtime screenshots (real `WorldEntry.StartNewGame` path, Core + Region_Hub), all viewed:
- `Logs/screenshots/runtime_hud_1920x1080.png`, `Logs/screenshots/runtime_hud_2520x1080.png` (21:9: HUD panel top-left and controls bottom-right do not collide)
- `Logs/screenshots/runtime_hud_auras_1920x1080.png` (all Auras unlocked, Fire current, 3/5 hearts, 120 coins, half energy)
- `Logs/screenshots/runtime_popup_1920x1080.png` (Aura unlock popup)

## Fixes
1. Hearts: `HeartsView` now adopts slots baked into the prefab and the HUD builder bakes 5 full hearts (`Preview(5)`), so the prefab and the edit-mode shot are never empty; at runtime it still follows `HeartsChanged`.
2. Boss bar: the group is serialized with alpha 0 (hidden by default) and `BossHealthBarView` hides it on enable; shown only after `BossEncounterStarted`.
3. Energy bar: `UITheme.EnergyColor` = pale gold (gold lerped 35% to white) with no Aura, otherwise the Aura colour; the prefab fill is baked pale gold.
4. Virtual controls themed: navy `bg/panel` disc (alpha from `settings.buttonOpacity`) + accent ring (gold for JUMP/ATK/DASH/SKILL, muted for MAP/II), mono labels scaled to the button (JUMP largest). New generated sprites `ui-disc.png` / `ui-ring.png`; `VirtualControlsBuilder.Build(Sprite circle, Sprite ring)` (the old one-argument and `(Sprite, Font)` overloads still work, without ring). Aura buttons are filled circles in Wind/Fire/Water colour labelled GIÓ / HỎA / THỦY; locked = grey disc + padlock; the current Aura gets a white ring, others a dark outline.
5. Dynamic joystick: ring is serialized inactive and only appears under the finger.
6. 21:9 checked in `runtime_hud_2520x1080.png`.
Also: `AudioListenerGuard` on `UI_Root`. No scene has an AudioListener, so the game was silent and Unity logged "no audio listeners" every frame (with a GPU this produced multi-GB logs and made PlayMode runs crawl). It adds one to the main camera only when none exists (test `CoreHasExactlyOneAudioListener`). If the Audio agent adds a listener to the camera, the guard simply does nothing.

## Runtime capture test
`Tests/PlayMode/UI/RuntimeScreenshotTests.cs`, `[Explicit]` + `[Category("Screenshot")]`: skipped by normal runs (a namespace filter such as `AuraKnight.Tests.PlayMode.UI` selects it explicitly and runs it too, harmless). Batch mode never fires `WaitForEndOfFrame`, so the test renders the main camera by hand into a RenderTexture (overlay canvases switched to that camera, scale computed per size) and writes the PNGs.
Run: `UNITY_GRAPHICS=1 UNITY_TEST_FILTER=AuraKnight.Tests.PlayMode.UI.RuntimeScreenshotTests tools/unity-batch.sh test PlayMode`
`tools/unity-batch.sh test` now drops `-nographics` when `UNITY_GRAPHICS` is set (only change to the script).

## Verification (follow-up)
compile 0 errors / 0 warnings; EditMode 800/800; PlayMode non-Boss classes 98/98 (UI 32 incl. the Explicit capture test, Progression, Enemies, World, Audio); `UiGenerator.GenerateAll` rerun clean; dev APK Succeeded. The full PlayMode run does not finish right now: it hangs inside `Tests/PlayMode/Bosses` (the Bosses agent's work in progress, log goes silent after loading Region_Hub), unrelated to UI.
