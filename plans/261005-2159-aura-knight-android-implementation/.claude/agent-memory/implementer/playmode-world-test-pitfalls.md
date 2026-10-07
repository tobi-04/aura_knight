---
name: playmode-world-test-pitfalls
description: Pitfalls when writing PlayMode tests that walk Leo through the real region scenes (adjacent door triggers, Aura popup pause, hub preload)
metadata:
  type: feedback
---

- Rooms sit in adjacent 40-unit slots, so the right-door trigger of room A touches the left trigger of room B. Teleporting Leo into the middle of a door trips both and bounces him back; place him just inside the trigger from the room's interior (see `LevelsPlayModeBase.JustInside`).
- `AuraManager.Unlock` publishes `AuraUnlocked`; the HUD popup pauses the game (`GameMode.Paused`, `Time.timeScale` 0), after which `TrySwitch` fails because the cooldown never ticks. Call `PauseController.Instance.Resume()` after unlocking in tests.
- Leo takes knockback from an enemy at his arrival spawn and can be pushed into the exit he came through; level data keeps enemies 8 columns and hazards 3 cells away from every door spawn (validator `SpawnSafety`). Chain tests also set `Health.InvulnerabilityGate = () => true`.
- Any preloading zone that restores a default preload on `OnTriggerExit2D` races with `RoomExit` in the same physics step and can unload the region Leo is entering; restore in `LateUpdate` after checking the current room.
- Real region scenes now contain `hub_02`, `forest_boss`, etc. Tests that spawn extra rooms must use unique ids (spawn inactive, rename, activate), or `RoomRegistry` logs a duplicate-id error that fails the test.

**Why:** each of these cost a failed run during phase 9.
**How to apply:** reuse `Tests/PlayMode/Levels/LevelsPlayModeBase` helpers instead of re-deriving them.
