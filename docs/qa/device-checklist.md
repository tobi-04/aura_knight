# Checklist trên máy Android thật

**Chưa chạy.** File này là mẫu cho người cầm máy; không ô nào được tích trừ khi người đó đã làm và ghi tên, máy, ngày. Tiêu chí lấy từ GDD §12.5, §15.1, §15.2 và plan phase 13: thành công khi checklist pass trên **2 máy** (1 máy tầm trung, 1 máy yếu).

## Thông tin máy

| | Máy 1 (tầm trung) | Máy 2 (yếu) |
|---|---|---|
| Model / Android | | |
| RAM / chip | | |
| Tỉ lệ màn hình / tai thỏ | | |
| Build (APK, commit) | | |
| Người kiểm, ngày | | |

## 0. Cài đặt
- [ ] Cài APK theo README "Cài APK" (adb hoặc sideload) và game mở được tới menu (máy 1 / máy 2).
- [ ] Màn hình luôn bật khi chơi; xoay ngang tự động; không xoay dọc.

## 1. Hiệu năng (Unity Profiler qua USB, build dev, dùng `adb forward` rồi Profiler > Android)
Cách đo: chơi liên tục 5 phút trong từng vùng và 3 phút ở mỗi boss; ghi fps trung bình và thấp nhất bằng Profiler (Frame Time) hoặc `adb shell dumpsys gfxinfo com.aurastudio.auraknight`.

| Mục | Ngưỡng | Máy 1 | Máy 2 |
|-----|--------|-------|-------|
| fps trung bình khi chơi thường (Hub, Rừng, Hang, Đô Thị, Lâu Đài) | **≥ 55** | | |
| fps thấp nhất khi đánh boss (4 boss) | **≥ 30** | | |
| RAM (PSS tổng, `adb shell dumpsys meminfo com.aurastudio.auraknight`) | **< 600 MB** | | |
| Chế độ tiết kiệm (Settings) khoá 30 fps | đúng 30 | | |
| Nóng máy sau 15 phút | không ngắt khung | | |
| Kích thước APK | < 150 MB (dev APK đo ở máy build: xem báo cáo phase 13) | | |

## 2. Vòng đời ứng dụng
- [ ] DC-04 Home ở giữa phòng, đợi 30 giây, mở lại: game tiếp tục (pause menu hiện hoặc game tạm dừng), không crash. (máy 1 / máy 2)
- [ ] DC-04b Khoá màn hình 1 phút rồi mở: như trên.
- [ ] DC-05 Có cuộc gọi đến (hoặc báo thức) khi đang chơi: sau khi cúp, không mất tiến trình tính từ Bàn Thờ gần nhất.
- [ ] DC-05b Vuốt tắt app (kill) khi đang giữa vùng, mở lại bằng Tiếp tục: về đúng Bàn Thờ gần nhất, xu và Aura còn nguyên.
- [ ] Cạn pin hoặc kill khi đang ghi (khó): nếu file save hỏng thì `.bak` được dùng (tự động đã test, đây là kiểm trên hệ file thật).

## 3. Ba tỉ lệ màn hình (GDD §15.1)
Thử trên máy thật hoặc đổi kích thước (`adb shell wm size 1080x2340`, `wm size reset` sau khi xong).

| Tỉ lệ | Thử bằng | UI không bị tai thỏ che (HUD, nút ảo, pause) | Nút ảo chạm được hết | Ghi chú |
|-------|----------|-----------------------------------------------|----------------------|---------|
| 16:9 | 1920x1080 | | | |
| 19.5:9 | 2340x1080 | | | |
| 21:9 | 2520x1080 | | | |

## 4. Chuyển phòng 50 lần (DC-06)
- [ ] Đi qua cửa `hub_01 ↔ hub_02` 50 lần liên tục (đi qua 25 lần mỗi hướng), không lỗi camera, không mất Leo, không nảy lại. (tự động đã test bằng teleport, đây là kiểm bằng điều khiển cảm ứng)
- [ ] Qua ranh giới vùng (hub_03 → cave_01, hub_03 → city, hub_03 → castle) 10 lần mỗi chỗ: không giật hình khi tải vùng mới.

## 5. Đầu vào cảm ứng
- [ ] Joystick động (nhấn trái màn hình), nút JUMP/ATK/DASH/SKILL đủ to, nhiều ngón cùng lúc, kéo xuống để slide, vuốt xuống để rơi qua bệ một chiều.
- [ ] Rung (Settings bật/tắt) hoạt động khi trúng đòn.
- [ ] Nút Aura khoá hiển thị khoá; nút Aura mở khoá đổi Aura đúng.

## 6. Âm thanh
- [ ] Nhạc theo vùng, crossfade khi vào combat; SFX nhảy, đánh, nhặt xu; âm lượng trong Settings.
- [ ] Không có tiếng rè hoặc lag âm khi nhiều SFX cùng lúc (boss).

## 7. Toàn bộ game (GDD §15.2)
- [ ] Chơi từ đầu tới ending trên máy thật không crash, không softlock (máy 1).
- [ ] Credits mở được từ menu và liệt kê asset.
- [ ] Release APK (khi có keystore) cài được và `Development Build` tắt.
