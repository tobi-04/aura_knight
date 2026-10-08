# Nhật ký thay đổi

Mới nhất ở trên. Kế hoạch: [`development-roadmap.md`](development-roadmap.md).

## 2026-10-08 (sửa nhỏ: BUG-004, xác nhận Game mới, LICENSE)

Số đã kiểm: compile sạch, EditMode 968/968, PlayMode 167 xanh + 2 bỏ qua (ảnh chụp, cần GPU).

### Sửa
- **BUG-004:** thêm `Combat/DamageKind.cs` (General, Fire); `DamageInfo.Kind` tùy chọn, mặc định General; `Hitbox.Kind` đặt lúc chạy, `FireballProjectile.Launch` đặt Fire. `WeakPointHurtbox.fireOnly`: lò hơi Cỗ Máy chỉ nhân đôi Cầu Lửa (đòn khác x1), đúng GDD §7.4. Lõi Cây Mục vẫn nhân đôi mọi đòn, chờ thiết kế xác nhận.
- **Xác nhận Game mới:** `UI/Screens/ConfirmDialog.cs`; `MainMenuScreen` hỏi "Bắt đầu lại?" chỉ khi đã có save. Hủy và Back đóng hộp; BẮT ĐẦU chạy intro rồi vào game. Hộp do `OverlayScreensBuilder.ConfirmNewGame` dựng trong `MenuScreens.prefab`; chuỗi `confirm.*` trong `Strings_vi.json`.

### Thêm
- `LICENSE` (MIT) ở gốc repo, chỉ cho code và tooling. Art, âm thanh, phông, `docs/reference/` loại trừ, trỏ tới các `LICENSES.md`.
- Test: `AFireOnlyWeakPointDoublesOnlyFireDamage`, `BoilerTakesDoubleDamageFromAFireball`, kiểm `Kind` trong `FireballReviewTests` và `GeneratedBossAssetsTests`, `NewGameOverASaveAsksFirstAndCancelKeepsTheMenu`.

## 2026-10-08 (sửa theo review cuối, commit d02c5d7)

Reviewer cuối (`f89a05d..e0623d5`, quyết định SEALED, 0 lỗi critical) nêu 3 cảnh báo; cả ba đã sửa trong code.

### Sửa
- **Cổng và đường tắt lưu ngay:** `OneTimeAuraGate` và `Shortcut` gọi `GameManager.Save()` sau khi đánh dấu đã mở, giống ấn, rương, shop. Trước đó trạng thái chỉ nằm trong bộ nhớ, mất nếu tiến trình bị kill mà `OnApplicationPause` không chạy.
- **Chống mất boss khi thiếu phần thưởng:** `IBossVictorySteps.UnlockReward()` trả `bool`; `BossVictorySteps` trả `false` khi không có `AuraManager`, và `BossVictorySequence` khi đó không `MarkDefeated`/`BossDefeated` (boss không biến mất vĩnh viễn mà Aura chưa nhận).
- **Game mới không đè save cũ:** `GameManager.OnApplicationPause` bỏ qua lần lưu khi state là game mới chưa lưu mà đã có save (`!stateSaved && HasSave`), nên đưa app vào nền ngay sau "Game mới" không ghi đè slot Tiếp tục.

### Còn mở (từ review, chưa sửa)
- ~~Chưa có hộp xác nhận "Game mới" khi đã có save.~~ Đã làm, xem mục trên.
- Key art (`Art/KeyArt`) chưa rõ nguồn và quyền: xem `Assets/_Project/Art/LICENSES.md`, GDD §17.3.
- `BuildScript`: mật khẩu keystore chỉ có trong phiên Unity nên build release bằng batch luôn bị từ chối; `unity-batch.sh` chưa có timeout cho compile/test/exec.

## 2026-10-08 (phase 8)

Boss (`Scripts/Bosses`). Số đã kiểm khi hoàn thành: EditMode 800/800, PlayMode 127 xanh + 1 bỏ qua.

### Thêm
- Khung boss: `BossBase` (partial), `BossStats` (SO, `Data/Bosses/{RootTree,GiantStoneSpider,RogueMachine,Malakor}.asset`), `BossPhase` + `WeightedPicker` (chọn đòn có trọng số, tránh lặp), `BossAttackTimeline` (Telegraph/Execute/Recover, báo trước tối thiểu 0.5 s ở mọi tốc độ phase), `BossHazard`, `BossArena` (cửa đóng, thanh máu, nhạc, reset khi Leo chết), `WeakPointHurtbox` (x2 sát thương), `PlayerSlowStatus` (tơ nhện làm chậm).
- 4 boss, mỗi boss 3 đòn + 1 biến thể phase 2 (50% HP, nhanh x1.25); Malakor có phase 3 ở 25% HP (`DarkPhaseController` làm tối, `AuraColorStrikeAttack`). HP 30 / 40 / 50 / 70.
- Thứ tự chiến thắng (`BossVictorySequence`): `AuraManager.Unlock` (lưu) → `MarkBossDefeated` → `BossDefeated` → `BossEncounterEnded` → nhạc; boss cuối: `GameCompleted`. Boss đã thắng không xuất hiện lại sau Continue.
- Generator `BossAssetGenerator` (stats, prefab, phòng boss thô, `Test_Boss_*`), số đòn trong `BossAttackSetup`; hook debug `BossDebugControls` chỉ trong Editor.

### Giới hạn
- Chưa cân bằng bằng người chơi thật; hazard/prop là hình chữ nhật placeholder; thân boss không có collider đặc.

## 2026-10-07 (phase 2, 7, 10, 11, 12)

Chạy song song. Phase 2: pipeline art (`ArtPipeline`, art sinh bằng `tools/art/*.py`, một pack CC0 Kenney Tiny Dungeon). Phase 7: 4 archetype quái + 3 modifier, 8 biến thể, `CoinPickup`, `EnemyRoomLimitValidator`, `Test_Enemies`. Phase 10: UI (theme, router, HUD, menu, cài đặt, credits, `Strings_vi.json`, `UiGenerator`). Phase 11: âm thanh sinh bằng script (27 SFX, BGM theo vùng 2 lớp), `AudioManager`, `MusicLayerController`. Phase 12: `Wallet`, shop + NPC Sol, rương, dữ liệu và màn bản đồ. Chi tiết kiến trúc ở `system-architecture.md` §5-8. Công cụ chụp màn hình `tools/unity-batch.sh shot` thêm trong đợt tích hợp.

## 2026-10-08 (phase 13)

QA, tối ưu, sẵn sàng phát hành. Số đã kiểm: EditMode 963/963, PlayMode 165 xanh + 2 bỏ qua (ảnh chụp, cần GPU), `compile` 0 lỗi / 0 cảnh báo, `RegenerateAll` sạch, dev APK 48.5 MB. Chưa chạy trên thiết bị thật.

### Thêm
- `docs/qa/`: test case (ánh xạ GDD §15.1 sang test), bug log, mẫu playtest M1-M3, checklist thiết bị (mẫu trống).
- `BossHazardPool` (pool hazard boss), `LightBudget` + `LightBudgetController` (giới hạn Light2D, 4 khi tiết kiệm), `AudioListener` thật trên camera Core, `GameSettings.ApplyFrameRate` (Boot dùng lựa chọn tiết kiệm đã lưu).
- Test: 50 lần chuyển phòng, app vào nền, pool, light budget, cấu hình phát hành, `RuntimeRoomScreenshotTests` (ảnh runtime mỗi vùng).
- README: mục "Cài APK" và QA.

### Thay đổi
- Phiên bản `1.0.0` (mã 1), Development Build tắt, vsync 0 ở mọi quality level.
- `RegionLightingTable.Castle` 0.05 thành 0.15 (ảnh runtime cho thấy 0.05 không thấy bệ đứng). GDD §10 đã ghi số mới, kèm "cần xác nhận trên máy thật".
- `BuildScript.ReleaseSigningProblem` tách thành hàm thuần có test.

## 2026-10-08

Phase 9: nội dung 4 vùng. Số đã kiểm: EditMode 948/948, PlayMode 154 xanh + 1 bỏ qua (GPU).

### Thêm
- `Data/Levels/*.room.txt`: 30 phòng dạng lưới ký tự (nguồn sự thật), `docs/level-map.md` (bản đồ, ký hiệu, cổng, bí mật, đường tắt).
- `LevelGenerator` (+ `RoomFile` parser, bộ giải lưới `LevelReachability`, `LevelProgression`, `LevelValidator`, `BossRoomLinker`, `LevelSceneBuilder`) và các bước `bosses`, `progression`, `levels`, `map` trong `RegenerateAll`.
- Runtime: bẫy (`Spikes`, `CollapsingPlatform`, `FallingStalactite`, `Piston`, `MovingSpikeFloor`, `AcidPool`; hơi nóng/bẫy lửa dùng lại `HeatVent`/`ExtinguishableGate`), `ParallaxLayer`, `RegionLighting`, `RegionPreloadZone`, `SealGate`/`AuraSeal`, `SmoothWall`.
- Gating: vách nhẵn 6 ô (Hang, cần Gió), Rào Gỗ ở `hub_03` (Hỏa), cổng 3 ấn (Gió + Hỏa + Thủy). Có test chứng minh bằng bộ giải lưới và bằng mô phỏng `PlayerController` thật.

### Thay đổi
- `KinematicMotor2D.GripsWall` / `PlayerController.WallContactToward`: vách có `SmoothWall` không bám được (không thì chuỗi wall jump leo được vách 6 ô).
- Test cũ phải đổi vì thế giới thật nay có `hub_02`, `forest_boss`...: `RoomSwitchPlayModeTests` dùng id `hub_99`, `BossTestKit.SpawnRoom` đổi id bản sao, `WorldFlowPlayModeTests` không còn đòi hub bị unload (`forest_01` là phòng cửa ngõ giữ hub).

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
