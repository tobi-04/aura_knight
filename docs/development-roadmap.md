# Lộ trình phát triển

Nguồn: [`plans/261005-2159-aura-knight-android-implementation/plan.md`](../plans/261005-2159-aura-knight-android-implementation/plan.md). Cập nhật: 2026-10-06. Nhật ký: [`project-changelog.md`](project-changelog.md).

| Phase | Nội dung | Trạng thái |
|-------|----------|------------|
| 1 | Setup project và repo | Xong, chờ kiểm trên máy thật (APK dev 56 MB đã build) |
| 2 | Asset và art pipeline | Chưa làm |
| 3 | Di chuyển + điều khiển cảm ứng | Xong, chờ kiểm cảm giác trên máy thật |
| 4 | Chiến đấu + máu | Xong, chờ kiểm trên máy thật |
| 5 | Hệ thống Aura | Xong, chờ kiểm trên máy thật |
| 6 | Khung thế giới + save | Xong, chờ kiểm trên máy thật |
| 7 | AI quái | Chưa làm |
| 8 | Boss | Chưa làm |
| 9 | Nội dung 4 vùng | Chưa làm |
| 10 | UI, HUD, menu | Chưa làm |
| 11 | Âm thanh | Chưa làm |
| 12 | Tiến trình, shop, bản đồ | Chưa làm |
| 13 | QA, tối ưu, phát hành | Chưa làm (chạy liên tục từ tuần 2) |

## Việc treo từ các phase đã xong

- Kiểm trên thiết bị thật: rung, `File.Replace`, cảm giác nhảy/dash cảm ứng, 60 fps.
- Menu thật gọi `WorldEntry`; pause menu phải dùng `GameMode.Paused` và `HitStop.GameplayScale`.
- Phase 7 gắn logic reset quái vào `OnEnable` (phòng được `Room.Restart()` khi hồi sinh).
- Boss gọi `AuraManager.Unlock` trước khi publish `BossDefeated`.
- Quyết định chờ xác nhận: cho phép chém kiếm khi bơi.
- Mốc theo GDD §13–14: mốc 1 (Rừng chơi hết) cuối tuần 4, trigger cắt phòng nếu trễ; feature freeze cuối tuần 7.
