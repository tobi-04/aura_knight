# Phase 06 - World Framework and Save (implementer)

## Files (all under Assets/_Project)
- Scripts/Core: EventBus, GameEvents, GameState, ISaveStorage, FileSaveStorage, SaveSystem, GameMode, GameManager, SceneLoader, RegionLoader
- Scripts/World: Room, RoomExit, RoomRegistry, RoomManager, RegionGraph (+RegionNode), RegionLoadPlan, RoomValidator (+RoomInfo), SunAltar, CheckpointService, Shortcut, PersistentId, WorldTags
- Scripts/Editor/World: AuraKnight.Editor.World.asmdef (adds Unity.Cinemachine ref), RoomIdValidator, WorldAssetGenerator, WorldSceneGenerator
- Data/World/RegionGraph.asset; Prefabs/Rooms/_Template/Room_Template.prefab; Prefabs/Interactables/{SunAltar,Shortcut}.prefab; Scenes/Test/Test_Rooms.unity; Scenes/Core.unity populated
- Tests/EditMode/Core: EventBusTests, GameStateTests, SaveSystemTests; Tests/EditMode/World: RegionGraphTests, RoomValidatorTests

## Key APIs
EventBus.Subscribe/Unsubscribe/Publish<T: struct>, Clear(); GameState.NewGame(), GetPurchaseCount/AddPurchase, Mark* helpers;
SaveSystem(ISaveStorage).Save/TryLoad/Delete (never throws); GameManager.Instance (State, Mode, StartNewGame, Continue, Save);
RoomManager.EnterRoom(id, spawnName, mover) / EnterRoomAt(point, mover); CheckpointService.RegisterPlayer/SetCheckpoint/Respawn;
RegionLoader.SetCurrentRegion/Preload (auto-reacts to RoomEntered); menus Aura/Validate Rooms, Aura/Generate World Assets, Aura/Generate World Scenes.

## Results
Compile clean. EditMode 115 total, 114 pass. Only failure: AuraKnight.Tests.Player.PlayerSlideAndWallSimulationTests.SlidesThroughOneTileGapTwentyTimesInARow (other agent's, not touched). All 40+ Core/World tests pass. Generators run twice (idempotent: Core scene has exactly one of each component). Validator batch: 0 rooms scanned, 0 errors (no room prefabs yet besides _Template, which is skipped).

## Deviations / notes
- Confiner "blend": CinemachineConfiner2D has no blend; RoomManager raises Damping for 0.3s on swap (tunable). Needs on-device feel check.
- Exit zones sit just across the room border (inside the destination) to avoid ping-pong between rooms.
- GameState.version defaults to 0 until NewGame()/Save stamps it, so a JSON without version is rejected.
- Editor asmdef in Scripts/Editor/World added because the shared Editor asmdef lacks a Cinemachine reference and is outside my ownership.

## Follow-ups (human / later phases)
- Seamless transition feel, 50x cross-region walk test, kill-app restore on a phone.
- Respawn only finds altars in loaded scenes; flow must load the altar's region first (SetCurrentRegion) before Respawn after Continue.
- Hearts/energy restore on CheckpointReached (combat phase). Shortcut opens on trigger touch; input-based open optional.
- allowSceneActivation staging for gateway rooms not implemented; Test_Rooms has an unscripted TestPlayer (needs real Player).
- Validator does not yet scan PersistentIds placed directly in Region scenes.
