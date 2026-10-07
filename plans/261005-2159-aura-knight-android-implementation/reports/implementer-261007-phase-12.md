# Phase 12 report: Progression Shop and Map (2026-10-07)

Status: DONE_WITH_CONCERNS. Nothing committed. Screens were never rendered (batch Unity is `-nographics`), so layout is verified by structure and tests only.

## Verification
| Check | Result |
|---|---|
| `tools/unity-batch.sh compile` | clean |
| `test EditMode` | 766 passed / 0 failed (90 are mine, `AuraKnight.Tests.EditMode.Progression`) |
| `test PlayMode` | 96 passed / 0 failed (13 are mine, `AuraKnight.Tests.PlayMode.Progression`) |
| `exec AuraKnight.Editor.ProgressionAssetGenerator.GenerateAll` | run 3 times, idempotent |
| `exec AuraKnight.Editor.UiGenerator.GenerateAll` | rerun after the last UI change |

TDD note: the pure-logic tests were written right after the code and before the first Unity run; I did not get a per-test red run.

## Files (under Assets/_Project)
Runtime `Scripts/Progression` (namespace `AuraKnight.Progression`): `Wallet`, `ShopEffect`, `ShopItem` (SO), `ShopEffects` (caps 9/200/3), `ShopRules` (tier price, status, `TotalCost`), `ShopService` (`TryBuy(GameState,item)` pure; `Buy(item)` live: refresh player, save, Sfx), `ShopRequested` (event), `ChestLogic` (+`ChestReward`, `ChestOutcome`), `RoomMapData` (SO, `MapRoom`, `MapIcon`), `MapRules`, `MapLayout`, `MapViewport` (pure zoom/pan), `WorldPromptBuilder`.
`Scripts/World/Interactables`: `TreasureChest`, `NpcSol`, `DialogueByProgress` (SO).
`Scripts/UI/Screens`: `ShopScreen`, `ShopItemView`, `ShopMapLauncher`, `MapScreen`, `MapCellView`, `MapInput` (drag, Input System pinch, mouse wheel).
Editor `Scripts/Editor/Progression`: `ProgressionAssetGenerator`, `RoomMapDataBuilder`, `InteractablePrefabBuilder`. Also `Scripts/Editor/UI/ShopMapScreensBuilder.cs` (see deviations).
Generated: `Data/ShopItems/01_Heart..07_Map_castle.asset`, `Data/Dialogue/SolDialogue.asset`, `Data/Map/RoomMapData_{hub,forest,cave,city,castle}.asset`, `Prefabs/Interactables/{TreasureChest,NpcSol}.prefab`, `GameScreens.prefab` regenerated.
Modified (minimal): `Scripts/World/Pickups/CoinCollector.cs` (Collect goes through `Wallet.Live`; `AddSaturating` delegates to `Wallet`, same behaviour and signature), `Scripts/Player/PlayerStats.cs` (+`ApplyUpgrades()`, see below), `Scripts/Editor/UI/ScreensPrefabBuilder.cs` (builds Shop and Map screens and the launcher), `Data/UI/Resources/Strings_vi.json` (+36 keys, 126 total).

## Behaviour
- Wallet: only writer of `GameState.coins`; saturating add, spend never goes negative, every change raises `OnCoinsChanged` and `EventBus CoinsChanged`. HUD/audio keep working unchanged.
- Shop: items are data. Price tier = purchase count (last tier repeats), max count, `AtStatLimit` guard (no charge if a stat is already capped). `Buy` applies the upgrade to the live player, saves immediately, plays `Sfx.Coin` (refusal: `UiBack`).
- `PlayerStats.ApplyUpgrades()` (new, tiny public method): takes new maxima/sword level from the save but keeps current hearts/energy and adds the gain (a new heart arrives full). I did not use `RefreshFromGameState` because it fully heals on every purchase. Does nothing for hearts while dead. Chests with a free upgrade use it too.
- Chest: `PersistentId` + trigger; opens once, 100-150 coins (inclusive) or a per-chest free upgrade (falls back to coins if the stat is capped), saves immediately, `Sfx.Chest`, restores open state on `GameStateLoaded`/Start from `openedChests`.
- Map: one rectangle per room, region colour from `UITheme.RegionToken` (forest wind, cave/hub gold, city water, castle fire). Visited = solid; unvisited = faint and only when `map_<region>` was bought; icons (T altar, B boss, R chest, D shortcut) only on visited rooms, opened chests vanish; Leo marker from `RoomManager.Current` + player position inside the room bounds. Drag, two-finger pinch, mouse wheel and +/-/LEO buttons; pan clamped. Regions are laid out like the GDD diagram (hub centre, forest left, cave right, city below, castle above, 2-cell gaps).
- Opening: `ShopMapLauncher` (in GameScreens root) listens to `MapRequested` (second press closes) and `ShopRequested`; both screens pause the game and only open while `PauseController.CanPause`.
- NpcSol: zone trigger + hidden world prompt; pushing the stick up (edge, threshold 0.6, same as up-slash) talks; she picks the last `DialogueByProgress` entry whose Auras/boss are met and publishes `ShopRequested(lineKey)`; the shop shows the line above the cards. Lines (Vietnamese, keys `dialogue.sol.*`): start -> Rừng Xanh/Gốc Cây; Wind -> Hang Đá/Nhện Đá; Wind+Fire -> rào gỗ hub/Đô Thị/Cỗ Máy; all three -> Lâu Đài/Malakor; plus Malakor defeated -> ending line. The final boss id is `"malakor"` in `ProgressionAssetGenerator.GenerateDialogue`: change it if the bosses agent uses another id, then rerun.

## Economy (asserted in EditMode, both in-memory and against the generated assets)
Heart 100/200/300/400 (4), Energy 120/240/360/480 (4, +25), Sword 300/600 (2), Map 50 per region (1 each, 4 regions).
2 hearts 300 + 2 energy 360 + 1 sword 300 = 960; with 1500 coins 540 is left: not enough for a 2nd sword (600) or a 3rd heart plus a 3rd energy (660). Buying everything reaches exactly 9 hearts / 200 energy / sword 3 for 1000 + 1200 + 900 = 3100.

## How phase 9 places chests / NPC and rebuilds map data
1. Chest: drop `Prefabs/Interactables/TreasureChest.prefab` into the room prefab, set its `PersistentId.id` (convention `chest_<region>_<nn>`, unique across the game, 2 per region) and, in the `TreasureChest` component, `Reward` (default Coins 100-150; or kind Upgrade + effect Heart/Energy/Sword). Origin sits on the floor.
2. Sol: put `Prefabs/Interactables/NpcSol.prefab` in the hub room (origin on the floor). Zone is 6 x 3 around her; the prompt appears inside it.
3. Map data: after placing or moving rooms in the `Region_*` scenes run `Aura > Progression > Rebuild Map Data` (batch: `tools/unity-batch.sh exec AuraKnight.Editor.RoomMapDataBuilder.RebuildAll`, or `ProgressionAssetGenerator.GenerateAll`). It opens each region scene additively, turns every `Room.Bounds` polygon into grid cells (10 world units per cell, floor/ceil so a room is always covered), and adds icons for `SunAltar`, `TreasureChest`, `Shortcut` and every room whose id contains `boss`. Assets are updated in place, so `GameScreens.prefab` references stay valid; the map needs no UI regeneration. Rooms need a non-empty `roomId`; rooms only present as unplaced prefabs have no position and are skipped.

## Deviations and concerns
1. `ShopMapScreensBuilder.cs` lives in `Scripts/Editor/UI` (not `Editor/Progression`): `ScreenParts` is `internal` to the `AuraKnight.Editor.UI` assembly. `RegionGraph` path is duplicated as a constant (`ProgressionAssetGenerator.RegionGraphPath`) because `WorldAssetGenerator` is in another editor assembly.
2. Generation order matters: run `ProgressionAssetGenerator.GenerateAll` before `UiGenerator.GenerateAll` (the builder binds the ShopItem/RoomMapData assets by type; it logs a warning if there are none). `UiGenerator` itself was not modified.
3. No visual check: card and map layout numbers follow slide 12 on the 1920x1080 canvas; the map is a plain coloured grid (slide 11 background art left for P1 polish as the risk section allows). Check on device: card columns (852 x 130, 2 columns, 4 rows fit 590 px), +/-/LEO button column, legend text, prompt size in world space (TMP font size 5).
4. Interaction uses stick-up because the control scheme has no interact button; discoverable via the prompt text. If design wants a dedicated button, change `NpcSol.Update`.
5. Shop stock is the 7 assets found in `Data/ShopItems`; hub has no map item (always known once visited). Hub unvisited rooms stay hidden.
6. Coins awarded by chests/pickups are saved only by chest open / next altar / purchase (pickups do not save by themselves, same as before).
7. `GameUiDirector` and HUD were not touched: the existing HUD MAP button already publishes `MapRequested`, which the new launcher consumes.
8. Docs (`system-architecture.md`, `code-standards.md`) were not updated (outside ownership): need a Progression module line, the `AuraKnight.Tests.*.Progression` asmdefs and the two generator entry points above.
9. Dialogue text for Fire/Water hints is written from GDD §7.1-7.2; review wording.

**Status:** DONE_WITH_CONCERNS
**Summary:** Wallet, data-driven shop (service, screen, NPC Sol with progress-based dialogue), treasure chests with persistence, room-map data with an editor rebuild tool, and a zoom/pan map screen are in. Compile clean, EditMode 766/766, PlayMode 96/96, generators and UI generator rerun.
**Concerns/Blockers:** No visual verification of the new screens; final boss id "malakor" and chest ids are conventions to align with phase 9 and the bosses agent; screens depend on running the progression generator before the UI generator.
