# Lộ trình phát triển

Nguồn: [`plans/261005-2159-aura-knight-android-implementation/plan.md`](../plans/261005-2159-aura-knight-android-implementation/plan.md). Cập nhật: 2026-10-08. Nhật ký: [`project-changelog.md`](project-changelog.md).

| Phase | Nội dung | Trạng thái |
|-------|----------|------------|
| 1 | Setup project và repo | Code xong; chờ kiểm trên máy thật (APK dev build được) |
| 2 | Asset và art pipeline | Code xong: art sinh bằng `tools/art`, một pack CC0 (Kenney); chờ đánh giá hình trên máy thật |
| 3 | Di chuyển + điều khiển cảm ứng | Code xong; chờ kiểm cảm giác trên máy thật |
| 4 | Chiến đấu + máu | Code xong; chờ kiểm trên máy thật |
| 5 | Hệ thống Aura | Code xong; chờ kiểm trên máy thật |
| 6 | Khung thế giới + save | Code xong; chờ kiểm trên máy thật |
| 7 | AI quái | Code xong (4 archetype, 8 biến thể); chờ cân bằng |
| 8 | Boss | Code xong (4 boss, Malakor 3 phase); chờ playtest cân bằng (BUG-004 đã sửa; lõi Cây Mục x2 mọi đòn chờ thiết kế xác nhận) |
| 9 | Nội dung 4 vùng | Code xong: 34 phòng (Hub 3, Rừng/Hang/Đô Thị 7 + boss, Lâu Đài 6 + boss) từ `Data/Levels/*.room.txt`; chờ playtest nhịp độ và kiểm hình |
| 10 | UI, HUD, menu | Code xong; chờ kiểm đa tỉ lệ và tai thỏ trên máy thật |
| 11 | Âm thanh | Code xong (âm thanh sinh bằng script); chờ nghe thử trên máy thật |
| 12 | Tiến trình, shop, bản đồ | Code xong; chờ playtest kinh tế (GDD §8) |
| 13 | QA, tối ưu, phát hành | Code xong: `docs/qa/`, pool, light budget, cấu hình 1.0.0. Còn: chạy trên máy thật, playtest M1-M3, keystore release, video demo |

Review cuối (`f89a05d..e0623d5`): SEALED, 3 cảnh báo đã sửa ở `d02c5d7` (xem changelog).

## Còn lại để phát hành (không làm được bằng code)

- Chạy trên ít nhất 2 máy thật (1 máy yếu): fps, RAM, cảm giác cảm ứng, nền/tiếp tục, 3 tỉ lệ màn hình, `File.Replace` trên Android: [`qa/device-checklist.md`](qa/device-checklist.md).
- Playtest M1, M2, M3: [`qa/playtest-m1.md`](qa/playtest-m1.md) ... Số liệu cân bằng (boss, kinh tế, Castle light 0.15) cần xác nhận sau đó.
- Tạo keystore release riêng (build release từ chối khi thiếu) và build APK ký.
- Quay video demo.
- Xác nhận nguồn gốc và quyền dùng key art và PDF tham chiếu (GDD §17.3; `Assets/_Project/Art/LICENSES.md`). Đã có `LICENSE` (MIT) ở gốc repo, chỉ cho code và tooling.

## Việc treo từ các phase đã xong

- Kiểm trên thiết bị thật: rung, `File.Replace`, cảm giác nhảy/dash cảm ứng, 60 fps.
- App vào nền không tự pause (`PauseController`).
- Quyết định chờ xác nhận: lõi Cây Mục có nên chỉ nhận x2 từ Cầu Lửa như lò hơi Cỗ Máy không (hiện x2 mọi đòn).
- Quái bị giết hồi sinh khi bật lại bất kỳ phòng nào, không chỉ sau bàn thờ (ledger kill chưa làm).
- Quyết định chờ xác nhận: cho phép chém kiếm khi bơi.
- Mốc theo GDD §13–14: mốc 1 (Rừng chơi hết) cuối tuần 4, trigger cắt phòng nếu trễ; feature freeze cuối tuần 7.
