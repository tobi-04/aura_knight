# Bug log

Mức độ ưu tiên sửa (plan phase 13): **Crash > Softlock > Gameplay > Visual**. Trạng thái: `Mở` / `Đang sửa` / `Đã sửa (cần kiểm trên máy)` / `Đã sửa` / `Không sửa (lý do)`.
Cách ghi: một dòng một lỗi, ID tăng dần (`BUG-NNN`), có bước tái hiện. Lỗi tìm bằng test tự động thì viết test tái hiện trước khi sửa (`docs/code-standards.md` §5).

## Template

| ID | Mức | Vùng / phòng | Mô tả | Bước tái hiện | Phát hiện bởi (người, máy, build) | Trạng thái | Ghi chú |
|----|-----|--------------|-------|---------------|-----------------------------------|------------|---------|

## Lỗi đã biết (từ phase 1-13, chưa có playtest người)

### Crash
Chưa có lỗi nào được ghi nhận. Chưa chạy trên máy thật: danh sách này sẽ đầy lên sau đợt kiểm thiết bị.

### Softlock
Chưa có lỗi nào được ghi nhận. Rủi ro còn lại: bộ giải lưới `LevelValidator` không mô hình hóa pogo (BUG-005); tất cả hố đều là vùng chết hoặc có thang/sàn (phase 9), nhưng chưa người nào chơi qua 34 phòng.

### Gameplay

| ID | Mức | Vùng | Mô tả | Bước tái hiện | Phát hiện bởi | Trạng thái | Ghi chú |
|----|-----|------|-------|---------------|---------------|------------|---------|
| BUG-004 | Gameplay | Boss Cây Mục, Cỗ Máy | `WeakPointHurtbox` nhân đôi **mọi** sát thương (kiếm, Cầu Lửa, gió), trong khi GDD §7.4 chỉ ghi Cầu Lửa vào lò hơi gây x2 (Cỗ Máy) và lõi sáng (Cây Mục: chỉ nói "đánh vào lõi") | Đánh kiếm vào boiler/lõi: sát thương x2 | Phase 8, ghi lại ở phase 13 | Đã sửa (2026-10-08), còn 1 điểm chờ thiết kế | `DamageInfo.Kind` (`DamageKind`: General, Fire; mặc định General); `Hitbox.Kind` đặt lúc chạy, `FireballProjectile.Launch` đặt Fire. `WeakPointHurtbox.fireOnly` bật cho lò hơi Cỗ Máy: chỉ Cầu Lửa x2, đòn khác x1. **Lõi Cây Mục vẫn nhân đôi mọi đòn** vì GDD chỉ nói "đánh vào lõi": cần nhóm thiết kế xác nhận. Test: `AFireOnlyWeakPointDoublesOnlyFireDamage`, `FireballReviewTests`, `GeneratedBossAssetsTests`, `BoilerTakesDoubleDamageFromAFireball` |
| BUG-005 | Gameplay (rủi ro) | Mọi vùng, nhất là vách Hang | Bộ giải lưới của `LevelValidator`/`GatePhysicsTests` không mô hình hóa pogo (kiếm đâm xuống nhảy lên 3 ô trên gai hoặc quái). Layout hiện tại không có gai hay quái gần vách Hang `cave_01` | Chỉnh phòng sau này: đặt gai/quái cạnh vách | Phase 9 | Mở (rủi ro thiết kế) | Quy tắc: không đặt gai/quái trong vòng 3 ô quanh cổng Aura. Hiện được xác nhận bằng mắt qua `docs/level-map.md`, chưa bằng bộ giải |

### Visual

| ID | Mức | Vùng | Mô tả | Bước tái hiện | Phát hiện bởi | Trạng thái | Ghi chú |
|----|-----|------|-------|---------------|---------------|------------|---------|
| BUG-001 | Visual (khó chơi) | Lâu Đài | Global Light 0.05 (GDD §10): ảnh runtime `castle_03`, `castle_boss` gần như đen, không thấy bệ đứng, tường | `RuntimeRoomScreenshotTests`, xem `runtime_room_castle_03.png` | Phase 13 (ảnh chụp) | Đã sửa (cần kiểm trên máy thật) | Nâng lên 0.15 (`RegionLightingTable.Castle`); vẫn tối nhất (Hang 0.25) nhưng bệ, tường, xích, trụ nhìn được. **GDD §10 còn ghi 0.05**: nhóm cần xác nhận số mới sau playtest, màn hình OLED sáng hơn LCD nên chỉnh theo máy thật |
| BUG-002 | Visual | Mọi vùng | Prop là hình vuông màu (bàn thờ, cổng, ấn, piston, rương, bẫy lửa, NPC Sol) | Mở bất kỳ phòng nào | Phase 9 | Mở (tài sản, chờ art) | Cần art thật. Không ảnh hưởng logic. Bàn thờ và bẫy lửa vẫn đọc được là "vật tương tác" nhờ màu |
| BUG-003 | Visual | 4 boss | Hazard boss (rễ, hạt độc, sóng, laser, hơi nóng, chém bóng) là hình chữ nhật màu | Vào bất kỳ đấu trường boss | Phase 8 | Mở (chờ art) | Telegraph vẫn rõ nhờ độ trong suốt 35% rồi đặc |
| BUG-006 | Visual | Hub | Lớp parallax "Back" (cột trụ) trùng màu với tile đất, có thể bị hiểu nhầm là tường đặc | `runtime_room_hub_01.png` | Phase 13 (ảnh chụp) | Mở | Viền sáng trên mặt đất vẫn phân biệt được; nên làm tối lớp Back của Hub trong `tools/art/gen_backgrounds.py`. Chờ phản hồi playtest |
| BUG-007 | Visual (công cụ) | Công cụ `shot` | Ảnh chụp edit-mode (`tools/unity-batch.sh shot`) vẽ cả 4 lớp parallax ở vị trí neo, răng cưa lớp foreground nằm giữa màn hình | `tools/unity-batch.sh shot` | Phase 9 | Không sửa (đúng thiết kế) | `ParallaxLayer` chỉ dịch chuyển khi chạy. Dùng ảnh runtime (`RuntimeRoomScreenshotTests`) để đánh giá hình ảnh |
| BUG-008 | Visual (đã đóng) | HUD | Báo cáo trước: vòng viền vàng của nút điều khiển ảo không thấy trong `runtime_hud_1920x1080.png` | Chạy lại `RuntimeScreenshotTests` ngày 2026-10-08 | Phase 10 | Đóng: không tái hiện | Vòng vàng thấy rõ ở JUMP, ATK, DASH, SKILL. Nút Aura khóa cố ý không có vòng (hiện khóa); nút Aura đã mở viền theo màu nguyên tố. Răng cưa đen đè lên tim và nút MAP trong ảnh là do test chuyển canvas sang Screen Space Camera (foreground parallax sort 20), không xảy ra ở build thật (Overlay) |

## Việc chưa kiểm được ngoài máy thật (không phải bug, ghi để không quên)
- Hiệu năng (fps, RAM) và nhiệt độ: chưa đo.
- Cảm giác điều khiển cảm ứng, rung, tai thỏ: chưa đo.
- Menu Boot/MainMenu không có AudioListener (menu im lặng và Unity cảnh báo mỗi frame trong menu): không ảnh hưởng gameplay (Core có listener thật). Ghi để làm khi thêm nhạc menu.
- `LightDropPickup` (rơi 10% khi quái chết) vẫn `Instantiate/Destroy`, chưa pool: tần suất thấp, không đáng.
