# Test case (GDD §15.1) và bản đồ test tự động

Cập nhật: 2026-10-08 (phase 13). Nguồn: [`../game-design-document.md`](../game-design-document.md) §15.1, §15.2. Không đánh dấu "Đạt" cho mục nào chưa ai chạy: cột **Kết quả** chỉ ghi `Auto: xanh` khi test tự động đã chạy xanh trong lần chạy ghi ở cuối file, còn mục thủ công để `Chưa chạy` cho đến khi người chạy điền (tên, máy, ngày).

Chạy test tự động: `tools/unity-batch.sh test EditMode` và `tools/unity-batch.sh test PlayMode`. Tên test dưới đây là tên method (tìm bằng `grep -rn "<tên>" Assets/_Project/Tests`).

## 1. Test case trọng tâm GDD §15.1

| ID | Case (GDD §15.1) | Loại | Test tự động / bước thủ công | Kết quả |
|----|------------------|------|------------------------------|---------|
| TC-01a | Nhảy nhấp nhẹ khoảng 2 ô | Tự động + thủ công | `PlayerControllerSimulationTests.TappedJumpIsAboutTwoTiles`, `AirPhysicsSimulationTests.TappedJumpIsMuchLowerThanHeldJump`. Cảm giác trên cảm ứng: mở `Test_Movement`, bật gizmo lưới, nhấp nút JUMP 10 lần, đo bằng mắt ~2 ô | Auto: xanh. Thủ công: Chưa chạy |
| TC-01b | Giữ nhảy = 4.5 ô | Tự động + thủ công | `PlayerControllerSimulationTests.HeldJumpReachesFourAndAHalfTiles`, `AirPhysicsSimulationTests.HeldJumpReachesFourAndAHalfTiles(dt)` (nhiều dt). Thủ công: giữ JUMP trong `Test_Movement`, đỉnh nhảy chạm vạch 4.5 | Auto: xanh. Thủ công: Chưa chạy |
| TC-02a | Dash đi đúng 5 ô | Tự động | `PlayerControllerSimulationTests.DashCoversFiveTilesInTenSteps` | Auto: xanh |
| TC-02b | Bị đánh trong 0.1 s đầu dash không mất máu | Tự động | `DashTrackerTests.InvulnerableForFirstTenthOnly`, `PlayerControllerSimulationTests.DashIsInvulnerableOnlyForFirstTenth`, `PlayerCombatSimulationTests.DashIFramesAbsorbHits` | Auto: xanh |
| TC-02c | Hồi dash 0.8 s | Tự động | `DashTrackerTests.CooldownBlocksDashUntilEightTenthsElapse`, `PlayerControllerSimulationTests.DashRespectsCooldown`, `OnlyOneDashInTheAir` | Auto: xanh |
| TC-03a | Slide qua khe cao 1 ô | Tự động | `PlayerSlideAndWallSimulationTests.SlidesThroughOneTileGapTwentyTimesInARow` | Auto: xanh |
| TC-03b | Slide dừng giữa khe không kẹt, không đứng dậy xuyên trần | Tự động | `PlayerSlideAndWallSimulationTests.SlideStaysActiveWhileCeilingBlocksStanding`, `SlideShrinksColliderForFortyFiveHundredthsThenStands` | Auto: xanh |
| TC-04 | Wall jump liên tục leo trục cao 20 ô | Tự động | `PlayerSlideAndWallSimulationTests.WallJumpingClimbsTwentyTileShaft` (+ `WallJumpKicksAwayAndLocksInputForQuarterSecond`, `WallSlideCapsFallSpeedAtThree`) | Auto: xanh |
| TC-05a | Mỗi AuraGate chỉ mở bằng đúng Aura | Tự động | `WorldRulesTests.EachInteractionNeedsItsOwnAuraOnly`, `WrongInteractionKindIsRejectedEvenFromTheRightAura`; PlayMode `GatePlayModeTests.TheBarricadeBlocksUntilTheFireSkillBurnsIt`, `TheSealGateOpensOnlyWhenAllThreeSealsAreLit` | Auto: xanh |
| TC-05b | Không thể vào Hang khi chưa có Gió, kể cả dash + wall jump lách | Tự động | `GatePhysicsTests.NoRandomInputSequenceGetsOverTheWallWithoutWind` (70 phiên ngẫu nhiên), `NoRunUpJumpAndDashTimingGetsOverTheWallWithoutWind` (lưới ~2500 tổ hợp), `LeanOnTheWallAndMashJumpNeverClimbsIt`, `WindsDoubleJumpClearsTheWall`; `LevelProgressionTests.WithoutAnyAuraOnlyTheHubTheForestAndTheCaveDoorstepOpen`. **Bot vật lý không thay người chơi thử lách**: người thật phải thử 10 phút ở `cave_01` (xem MT-02) | Auto: xanh. Thủ công: Chưa chạy |
| TC-06 | Chuyển phòng qua lại nhanh 50 lần, không lỗi camera, không mất player | Tự động + thủ công | PlayMode `RoomTransitionStressPlayModeTests.FiftyDoorCrossingsBetweenTwoHubRoomsKeepTheCameraAndLeo` (50 lần qua cửa `hub_01` và `hub_02`, kiểm camera follow, confiner, Leo trong phòng); `TraversalPlayModeTests.*` đi qua mọi cửa của cả 5 vùng; `RoomSwitchPlayModeTests`. Thủ công: xem DC-06 trên máy thật | Auto: xanh. Thủ công: Chưa chạy |
| TC-07 | Thoát app giữa chừng (Home, khóa màn hình, cuộc gọi) rồi mở lại không mất tiến trình tính từ Bàn Thờ gần nhất | Tự động (logic) + thủ công (máy thật) | PlayMode `QaRuntimePlayModeTests.SendingTheAppToTheBackgroundSavesProgress`, `ProgressSurvivesTheAppBeingKilledAfterTheBackgroundSave`, `TheMenuDoesNotWriteASaveWhenTheAppGoesToTheBackground`; `SaveFlowPlayModeTests.*`. Thủ công: DC-04, DC-05 | Auto: xanh. Thủ công: Chưa chạy |
| TC-08 | Đa tỉ lệ 16:9 / 19.5:9 / 21:9, UI không bị tai thỏ che | Tự động (logic) + thủ công | `VirtualControlsMathTests.SafeAreaMapsToNormalisedAnchors`, `GeneratedUiAssetsTests.EveryCanvasScalesFrom1920x1080AtMatchHalfAndRespectsTheSafeArea`. Ảnh chụp runtime 1920x1080 và 2520x1080 (`UNITY_GRAPHICS=1`, `RuntimeScreenshotTests`). **Safe area thật chỉ có trên máy có tai thỏ**: DC-03 | Auto: xanh. Thủ công: Chưa chạy |

## 2. Test logic liệt kê trong plan phase 13 (Requirements)

| Hạng mục | Test (EditMode) | Phủ |
|----------|-----------------|-----|
| `HealthTests` | `Tests/EditMode/Combat/HealthTests.cs`: `StartsFull`, `DamageReducesCurrentAndReportsOutcome`, `LethalDamageKillsExactlyOnce`, `ZeroOrNegativeDamageIsIgnored`, `HitStartsInvulnerabilityWindow`, `ExternalGateAbsorbsDamage`, `HealClampsToMaxAndReportsApplied`, `DeadHealthCannotBeHealedButRefillRevives` | Sát thương, chết một lần, i-frame, hồi máu, dữ liệu xấu |
| `WalletTests` | `Tests/EditMode/Progression/WalletTests.cs`: cộng, `NonPositiveAddChangesNothingAndPublishesNothing`, `AddSaturatesAtIntMax`, `NegativeSavedCoinsCountAsZero`, `SpendTakesExactlyThePriceOrNothing`, `NegativePriceIsRejected`, `NullStateIsRejected` | Cộng/trừ xu, tràn số, dữ liệu save hỏng |
| `ShopServiceTests` | `Tests/EditMode/Progression/ShopServiceTests.cs`: mua tim bậc 1 và bậc 2, `NotEnoughCoinsBuysNothingAndKeepsEverything`, `CannotBuyMoreThanTheLimitAndCoinsNeverGoNegative`, `AStatAtItsCapIsNotChargedFor`, `InvalidItemsAreRefused`, `BuyWithoutAGameManagerReportsNoGame` | Giá theo bậc, không đủ xu, giới hạn mua, item sai |
| `SaveSystemTests` (round-trip, file hỏng) | `Tests/EditMode/Core/SaveSystemTests.cs`: `SaveThenLoad_RoundTripsAllFields`, `TryLoad_CorruptFile_ReturnsFalseAndWarns`, `TryLoad_UnknownVersion_ReturnsFalseAndWarns`, `TryLoad_MissingFieldsInJson_YieldsUsableState`, `Save_StorageFailure_ReturnsFalseAndLogsWarning`, `FileStorage_AtomicWriteReplacesAndLeavesNoTempFile` | Đủ trường, file hỏng, sai version, thiếu field, lỗi ghi |
| `.bak` fallback | `Tests/EditMode/Core/SaveRecoveryTests.cs`: `TryLoad_CorruptMainFallsBackToBackup`, `TryLoad_MissingMainFallsBackToBackup`, `TryLoad_BothDamaged_ReturnsFalse`, `FileStorage_SaveSystemRecoversWhenMainIsTruncated`, `FileStorage_DamagedMainDoesNotOverwriteAGoodBackup`, `TryLoad_WrongVersionMainIsNotReplacedByAnOlderBackup` | Hỏng/mất file chính thì dùng `.bak`, cả hai hỏng thì false |
| `AuraManagerTests` (unlock / switch / cost) | `Tests/EditMode/Aura/AuraStateTests.cs`: `FirstUnlockAutoSwitchesLaterOnesDoNot`, `LockedAuraIsRejected`, `SwitchHonoursThreeTenthsSecondCooldown`, `CycleWalksUnlockedAurasInOrderAndWraps`, `CastRefusedWhenEnergyIsInsufficientAndSpendsNothing`, `CastSpendsExactCostAndStartsSkillCooldown`, `SaveLoadRoundTripKeepsUnlockedSetAndCurrent`; `AuraManagerEventTests.*` (sự kiện unlock/switch); `AuraActionGatingTests.*` | Mở khóa, đổi (cooldown 0.3 s), tiêu năng lượng, lưu/nạp |

Không thiếu test nào trong danh sách này nên không thêm test trùng. Test bổ sung ở phase 13 là những ca GDD §15.1 chưa có test tự động: TC-06 (50 lần chuyển phòng), TC-07 (app vào nền), và các ca tối ưu/phát hành ở mục 3.

## 3. Test phase 13 (tối ưu, phát hành)

| Hạng mục | Test |
|----------|------|
| Pool hazard boss (đạn, sóng, gai) | PlayMode `BossHazardPoolPlayModeTests` (4 test: tái dùng đúng object và sạch trạng thái, vẫn gây sát thương sau khi tái dùng, `ClearSpawned` trả về pool, hazard không bị boss khác xóa nhầm) |
| Giới hạn Light2D | EditMode `LightBudgetTests` (6 test logic chọn N đèn gần nhất), PlayMode `QaRuntimePlayModeTests.OnlyTheNearestLocalLightsStayLitAndPowerSavingTightensTheBudget` |
| Chế độ tiết kiệm = 30 fps, mặc định 60 | EditMode `GameSettingsTests.PowerSavingDropsTheFrameRateToThirty`, `TheFrameRateDefaultsToSixtyAndFollowsTheStoredPowerSavingChoice`; PlayMode `QaRuntimePlayModeTests.PowerSavingCapsTheFrameRateAtThirty` |
| vsync tắt mọi quality level (Android bỏ qua `targetFrameRate` khi bật vsync) | EditMode `ReleaseConfigTests.EveryQualityLevelHasVsyncOffSoTheFrameCapWorksOnAndroid` |
| AudioListener trên camera Core | EditMode `CoreSceneAudioTests.TheCoreMainCameraHasTheOnlyAudioListener` (file scene), PlayMode `QaRuntimePlayModeTests.CoreHasOneAudioListenerOnTheMainCamera`, `PausePlayModeTests.CoreHasExactlyOneAudioListener` (guard vẫn pass) |
| Phiên bản, build, ký | EditMode `ReleaseConfigTests`: `1.0.0` mã 1, Development Build tắt, IL2CPP ARM64, từ chối release khi thiếu keystore, không có keystore/APK trong repo |
| Ánh sáng Lâu Đài | EditMode `RegionLightingTableTests` (0.15, xem `bug-log.md` BUG-001) |
| Ảnh chụp runtime từng vùng | `RuntimeRoomScreenshotTests` (Explicit, cần GPU): `Logs/screenshots/runtime_room_<id>.png` |

## 4. Test thủ công (con người, trên máy thật)

| ID | Mục | Các bước | Kết quả mong đợi | Kết quả |
|----|-----|----------|------------------|---------|
| MT-01 | Chơi từ đầu đến ending (GDD §15.2) | Cài APK, game mới, chơi tới Malakor và ending, không thoát game | Không crash, không softlock | Chưa chạy |
| MT-02 | Thử lách cổng Hang không có Gió | Từ `hub_03` vào `cave_01` khi chưa có Gió; thử nhảy, dash, wall jump, kết hợp nhiều kiểu 10 phút | Không qua được bức tường trơn 6 ô | Chưa chạy |
| MT-03 | Cổng Đô Thị và Lâu Đài | Chặn rào hub_03 bằng Gió/Thủy (phải không phá được); mở bằng Hỏa; 3 ấn Lâu Đài | Đúng như `docs/level-map.md` | Chưa chạy |
| MT-04 | Cảm giác điều khiển | 10 phút chạy/nhảy/dash/slide/wall jump bằng cảm ứng ở `Test_Movement` | Nhảy chính xác, coyote/buffer hợp lý, nút đủ to | Chưa chạy |
| MT-05 | Boss | Đánh 4 boss, ghi số lần chết | Điểm yếu và phase như GDD §7.4; khuyến nghị fps ≥ 30 | Chưa chạy |
| MT-06 | Shop, bản đồ, rương bí mật | Mua tim/xu; mở map; mở rương bí mật mỗi vùng | Số xu, bản đồ và save đúng | Chưa chạy |
| MT-07 | Credits | Menu chính, Credits | Hiển thị Kenney (CC0), phông OFL, audio tự sinh | Chưa chạy |

Checklist trên máy thật (fps, RAM, vào nền, tỉ lệ màn hình): [`device-checklist.md`](device-checklist.md). Playtest người mới: [`playtest-m1.md`](playtest-m1.md), [`playtest-m2.md`](playtest-m2.md), [`playtest-m3.md`](playtest-m3.md).

## 5. Lần chạy tự động gần nhất

Ghi ở cuối phase 13: xem báo cáo `plans/261005-2159-aura-knight-android-implementation/reports/implementer-261008-phase-13.md` (số test, ngày). Test `[Explicit]` (ảnh chụp runtime) không chạy trong lần chạy thường.
