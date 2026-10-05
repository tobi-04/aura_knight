# Nhật ký thay đổi

Mới nhất ở trên. Kế hoạch: [`development-roadmap.md`](development-roadmap.md).

## 2026-10-06

Phase 1, 3, 4, 5, 6 và hai vòng sửa theo review. Chưa commit. Số đã kiểm: EditMode 421/421, PlayMode 20/20, `compile` 0 lỗi / 0 cảnh báo, dev APK build được (56 MB). Chưa chạy trên thiết bị thật.

### Thêm
- **Phase 1:** project Unity 6000.6.0f1 (URP 2D, Input System, Cinemachine 6.6), `Aura → Setup Project`, `tools/unity-batch.sh` (compile/test/setup/exec, khoá chống chạy song song, từ chối khi Editor đang mở), build dev APK (`BuildDevelopmentApk`), `.gitattributes` (LFS, UnityYAMLMerge).
- **Phase 3:** di chuyển động học (`KinematicMotor2D`) và 13 state: chạy, nhảy (coyote, buffer, cắt nhảy), dash, slide, wall slide/jump, bơi; điều khiển cảm ứng (joystick động, vuốt, safe area); scene `Test_Movement`.
- **Phase 4:** `Health`, `Hitbox`/`Hurtbox` theo team, kiếm combo + chém xuống, knockback, i-frame, hit stop, năng lượng, hồi sinh tại bàn thờ.
- **Phase 5:** `AuraManager`, 3 Aura (Gió/Hỏa/Thủy) với passive và skill (Vòng Gió Lốc, Cầu Lửa, Khiên Nước), cơ quan môi trường (cổng đốt/dập, luồng gió, dung nham, nước + oxy), scene `Test_Aura`.
- **Phase 6:** `Room`/`RoomManager`/`RoomExit`, `RegionGraph` + `RegionLoader` (load additive), `SunAltar`/`CheckpointService`, `Shortcut`, `WorldEntry` (game mới / tiếp tục), `GameState` + `SaveSystem`, 5 scene vùng với phòng khởi đầu greybox, `RoomIdValidator`.
- 7 physics layer và collision matrix (`PhysicsLayers`, `PhysicsLayerSetup`).
- Tài liệu: `code-standards.md`, `system-architecture.md`, `development-roadmap.md`, `project-changelog.md`.

### Sửa theo review
- Hồi sinh xuyên vùng: tra bàn thờ qua `RegionNode.altarIds`, warp qua `RoomManager.Warp`, publish `PlayerRespawned` khi đã tới nơi; không giải được bàn thờ thì về Hub và thử lại.
- Luồng tiếp tục: `GameMode.Loading`, event `GameStateLoaded`, `ResetToFreshStart()` khi vào lại.
- Save: ghi tmp rồi replace, giữ `.bak`; `.bak` chỉ dùng khi file chính thiếu/rỗng/hỏng; sai version bị từ chối.
- `RegionLoader` theo dõi thao tác load/unload đang chạy (`RegionLoadPlan`); `Singleton.IsDuplicate` thống nhất cho các manager; `EventBus` copy-on-write không cấp phát.
- Rung (haptics) qua `VibrationEffect` 20 ms, quyền `VIBRATE` đã kiểm trong APK.
- Chặn input khi Pause/Dead (`PlayerActionRules`, `ControlsEnabled`); skill bị chặn khi Dead/Hurt.
- `BuildReleaseApk` từ chối khi chưa có keystore riêng.

### Thay đổi so với GDD (đã cập nhật GDD)
- Cắt nhảy nhả sớm: `vy *= 0.25` (GDD cũ ghi 0.5).
- `AuraGate` tách thành `OneTimeAuraGate` + `BurnableGate`/`ExtinguishableGate` và các cơ quan riêng.
- `shopPurchases` lưu dạng danh sách `{key, count}`; save có `.bak`, ghi nguyên tử, thêm `GameMode.Loading`.
- Kiếm dùng được khi bơi: chờ nhóm xác nhận.

### Giới hạn
- Chưa kiểm trên thiết bị: rung, `File.Replace` trên Android IL2CPP (có đường copy dự phòng), đường dẫn UnityYAMLMerge của Windows.
- `WorldEntry` chưa unload vùng cũ khi bắt đầu game mới lần hai.
