---
phase: 6
title: "World Framework and Save"
status: completed
effort: "6d"
owner: "B (rooms/scenes), A (save/core)"
weeks: "1-2 (rooms), 4 (save)"
notes: "Pending on-device frame time validation (50 consecutive room transitions <50ms); PlayMode 20/20 tests pass in batch"
---

# Phase 6: World Framework and Save

## Context Links
- GDD §7.1 (cấu trúc map), §12.3 (luồng chính, EventBus), §12.4 (save JSON)

## Overview
- Priority: P0 · Status: pending
- Room prefab + chuyển phòng liền mạch + load vùng additive + Bàn Thờ (checkpoint) + shortcut + SaveSystem + EventBus + GameManager.

## Key Insights
- **Một phòng = một prefab** có ID cố định, nên B và người khác không cùng sửa một scene.
- Vùng nằm sát biên được load trước (preload) khi Leo vào phòng cửa ngõ, nên không có khựng khi qua vùng mới.
- Save chỉ lưu **ID** (phòng, rương, boss, bàn thờ), không lưu vị trí object, nên ít lỗi khi sửa level.
- Android có thể kill app bất cứ lúc nào, nên phải lưu ở `OnApplicationPause(true)`.

## Requirements
- Room: `RoomBounds` (PolygonCollider2D cho Cinemachine Confiner2D), danh sách `RoomExit` (hướng, phòng đích, spawn point), ID dạng `forest_03`.
- Chuyển phòng: camera blend 0.3 s, player giữ nguyên quán tính, quái phòng cũ bị disable.
- `RegionLoader`: load/unload scene vùng theo danh sách vùng kề (tối đa 2 vùng cùng lúc).
- `SunAltar`: tương tác thì hồi đầy máu và NL, set checkpoint, autosave.
- `Shortcut`: cửa một chiều, mở từ bên trong, trạng thái được lưu.
- Save JSON §12.4 có `version`, ghi an toàn (ghi file tạm rồi rename).

## Architecture
```
Core scene: GameManager · EventBus · SaveSystem · RegionLoader · CheckpointService · Player · CM Camera · HUD
Region_X scene: Room prefabs (Tilemap, RoomBounds, RoomExits, spawns, interactables)
RoomExit ─enter─▶ RoomManager.Enter(roomId) ─▶ Confiner swap, OnRoomEntered, RegionLoader.Preload(neighbors)
SaveSystem.Save(GameState) ⇄ save_0.json  ·  GameState (POCO, serializable)
```

## Related Code Files
- Create `Scripts/Core/`: `GameManager.cs`, `EventBus.cs`, `GameEvents.cs`, `SaveSystem.cs`, `GameState.cs`, `SceneLoader.cs`, `RegionLoader.cs`
- Create `Scripts/World/`: `Room.cs`, `RoomExit.cs`, `RoomManager.cs`, `SunAltar.cs`, `CheckpointService.cs`, `Shortcut.cs`, `PersistentId.cs`
- Create: `Prefabs/Rooms/_Template/Room_Template.prefab`, `Prefabs/Interactables/SunAltar.prefab`, `Shortcut.prefab`
- Create scenes: `Region_Forest`, `Region_Cave`, `Region_City`, `Region_Castle`
- Create: `Scripts/Editor/RoomIdValidator.cs` (báo ID trùng hoặc exit trỏ tới phòng không tồn tại)

## Implementation Steps
1. (B, tuần 1) Room template + RoomExit + RoomManager + Confiner swap, test với 3 phòng grey-box.
2. (B, tuần 1–2) RegionLoader additive + preload theo `RegionGraph` (SO liệt kê vùng kề).
3. (A, tuần 2) EventBus (C# events tĩnh theo kiểu struct) + GameManager (state: Menu/Playing/Paused/Cutscene).
4. (A, tuần 4) GameState + SaveSystem (JsonUtility, tmp + rename, có `version`), autosave tại bàn thờ, khi hạ boss và khi pause.
5. (B, tuần 4) SunAltar, CheckpointService.Respawn, Shortcut; `PersistentId` cho rương, cửa, boss.
6. Editor validator chạy được bằng menu `Aura/Validate Rooms`.

## Todo List
- [x] Room template + exits + confiner (Room.prefab template with RoomExit/RoomBounds/Confiner swap on entry)
- [x] RoomManager + events (room transitions, camera blend 0.3s, quái disable, velocity preservation, Warp for respawn)
- [x] RegionLoader + RegionGraph (async load/unload, preload neighbors, deferred plan for load-during-unload contention)
- [x] EventBus + GameManager (struct-based static events, GameMode state, DefaultExecutionOrder[-100])
- [x] GameState + SaveSystem (atomic, tmp+rename+.bak fallback, version tracking, corrupted-file recovery)
- [x] SunAltar/Checkpoint/Shortcut/PersistentId (altar healing+autosave, async cross-region respawn with fallback to hub, one-way doors)
- [x] RoomIdValidator + AltarValidator (0 errors, unique room IDs, exit cross-refs, altar region mapping)

## Success Criteria
- [ ] Đi qua lại 50 lần giữa 2 phòng ở 2 vùng khác nhau: không mất player (kinematics verified code-level) ✓, không giật quá 50 ms (cần máy thật)
- [ ] Kill app giữa chừng rồi mở lại: tiến trình được khôi phục từ bàn thờ gần nhất (cần máy thật; đã có test save round-trip + PlayMode Continue-from-save)
- [x] Validator báo 0 lỗi trên toàn bộ vùng ✓ (RoomIdValidator + AltarValidator both return 0 errors on generated rooms)

## Risk Assessment
- Load additive gây khựng trên máy yếu → dùng `LoadSceneAsync` + `allowSceneActivation` ở phòng cửa ngõ, giữ vùng nhỏ.
- Đổi cấu trúc save giữa chừng → có `version`, migrate hoặc reset có cảnh báo.

## Security Considerations
- Save không chứa dữ liệu cá nhân. Không cần mã hóa (game offline).

## Implementation Notes
- **Two-phase startup flow:** GameManager.StartNewGame/Continue calls WorldEntry.Begin (async) → RegionLoader loads region → player spawns and placed at altar → RoomManager.EnterRoom → GameMode.Playing + GameStateLoaded event published (all PlayerStats/AuraManager re-read on this event).
- **Cross-region respawn fixed:** RegionNode.altarIds maps altar ID to region; CheckpointService.Respawn is a coroutine that finds the altar's region, loads it, then calls AltarArrival.Place (which does the teleport and room entry). Falls back to hub if altar unresolvable, retries every 1s.
- **Save file safety:** FileSaveStorage uses atomic write (tmp → rename with .bak fallback). HasSave parses the file to verify loadability. SaveSystem falls back to backup if main file corrupted.
- **Version tracking:** GameState has `version` field; on load mismatch, the file is discarded with a log (migration TBD).
- **Physics layers:** PhysicsLayerSetup defined Ground/Player/Enemy/Hazard/PlayerAttack/EnemyAttack/Interactable; collision matrix set; all generators wire layers.
- **Pause/Dead gating:** PlayerActionRules; ControlsEnabled checks GameMode.Playing; input buffers drop when off; AuraManager blocks Switch/Cycle while Dead.
- **Room state persistence:** OneTimeAuraGate + BurnableGate/ExtinguishableGate use PersistentId + GameState.openedShortcuts (need ID scheme that doesn't clash).
- **Water volume wading:** WaterVolume.enter/exit ref-counts water; respawn clears count (fixed to not break mid-water respawn); OxygenMeter counts down 8s in wading, auto-heals when exiting.
- **Event bus allocation:** Copy-on-write arrays, zero allocation per publish; SubscriberCount added for monitoring.
- **EditMode 421/421 + PlayMode 20/20 tests pass:** Includes cross-region respawn, continue flow, save corruption recovery, room confiner blend, layer setup.

## Next Steps
- P9 dựng phòng trên template (use greybox start room from RegionSceneGenerator).
- P12 dùng GameState (shop modifications, gate state queries).
- P10 dùng EventBus (listen to PlayerDamaged, PlayerDied, AuraChanged, etc).
