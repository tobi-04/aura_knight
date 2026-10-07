# Aura Knight: Nền tảng hoàn thành (Phases 1, 3-6)

**Ngày**: 2026-10-06 08:15  
**Severity**: high  
**Component**: Android / Core mechanics  
**Status**: resolved

## Điều đã được xây dựng

Phases 1, 3, 4, 5, 6 triển khai trên Unity 6000.6.0f1 (không phải 2022.3 LTS như kế hoạch—version 6000.6 với module Android đã được cài). 4 commit từ brainstorm GDD qua project setup, docs, core systems, đến game systems. Tạo từ 2D URP template tích hợp (tránh tạo tay Renderer2D assets). Setup CI/CD: `tools/unity-batch.sh` serialize runs với lock file, đảm bảo phase 3 và 6 không mở project hai lần.

**Output**: compile 0 lỗi/0 cảnh báo, EditMode 421/421, PlayMode 20/20, APK dev 56 MB IL2CPP ARM64, đẩy github.com/tobi-04/aura_knight (main, dev).

## Điều gãy và bất ngờ

**Round 1 (6.5/10)**: Reviewer phát hiện 2 critical. C1: respawn xuyên-region bị phục sinh tại chỗ (death loop). C2: không có flow continue-from-save. 12 warnings bao gồm File.Replace trên Android, input latch khi paused, no physics layers. Tất cả đã fix.

**Round 2 (7/10)**: Critical C-1 handle coroutine cũ. Viết test đầu (test-first)—đã PASS trên code cũ. Unity StartCoroutine không complete synchronously; handle đã clear trước gán. Vẫn hardened với `bool respawning` dù không tái hiện bug thực.

**GDD gating**: Cung 7-tile không crossable mà không double jump (run+jump+dash ≈ 10 tiles)—logic sai. Đổi thành 6-tile smooth wall (single jump 4.5, double ≈ 7.2).

**Teleport desync**: WorldTags.Teleport chỉ move transform; KinematicMotor2D giữ vị trí riêng. Giải quyết: ITeleportable interface trong World, PlayerController implement.

## Chi tiết kỹ thuật

- **EditMode**: 421 tests, 100% pass (Player 152 + Combat 104 + Aura 77)
- **PlayMode**: 20 tests xuyên phases (respawn region, continue, save atomic, pause gating)
- **BuildScript**: Bổ sung `FileInfo.Length` log (BuildSummary.totalSize báo 761.8 MB nhưng APK 56 MB)
- **Physics**: Thêm PhysicsLayers (Ground, Player, Enemy, Hazard, Attack*, Interactable), matrix collision
- **Save**: File.Replace + .bak fallback + copy path khi Replace lỗi, first-write atomic via Move
- **EventBus**: Copy-on-write array, 0 alloc per publish (test asserts)

## Những quyết định

Dừng respawn-in-place hoàn toàn (revive vào last altar với async load). Tách AuraGate thành OneTimeAuraGate + BurnableGate/ExtinguishableGate. Thêm GameMode.Loading. Fireballs vào skill scene (active scene fix). Sword-while-swimming: Swim → AirAttack → Swim (chưa confirm design).

## Bài học

1. **Test-first khi nghi ngờ bug**: Reproducer test lên code cũ, PASS? Không phải lỗi live. Hardened anyway để bảo vệ.
2. **Arithmetic checks GDD**: Tile dimensions, jump arc, chasm width—tính toán: 7-tile chasm sai.
3. **Atomic writes**: tmp → File.Replace → .bak, copy fallback, first-write via Move. Android không đảm bảo atomicity.
4. **Serialize batch ops**: Lock file tránh race conditions khi parallel agents.

## Tiếp theo

- On-device: touch feel, 60 fps, haptics, kill-app restore, transition smoothness
- Design: sword-while-swimming (awaiting user confirm)
- Phases 2, 7–13: chưa bắt đầu
- Deploy menu flow, pause menu (GameMode.Paused + resume to HitStop.GameplayScale)

**Status:** DONE  
**File path:** /Users/tgiap.dev/devs/aura_knight/docs/journals/2026-10-06-aura-knight-foundation-phases-1-3-4-5-6.md
