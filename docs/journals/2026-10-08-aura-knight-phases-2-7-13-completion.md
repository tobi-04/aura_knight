# Aura Knight: hoàn thành phase 2, 7–13

**Ngày**: 2026-10-08
**Component**: toàn bộ game (Unity 6000.6.0f1, Android)
**Status**: code xong; còn phần cần máy thật và người chơi

## Đã làm

Chia 4 đợt, mỗi đợt commit local, cuối cùng push `main` + `dev` (e4806d6):

- **A** (9755709): art CC0/tự sinh (P2), AI quái (P7), màn hình UI (P10), audio (P11).
- **B** (58c8445): 4 boss (P8), shop/ví/rương/map (P12), tích hợp, sửa HUD.
- **C** (5836a99): 34 phòng sinh từ `Data/Levels/*.room.txt` (P9), vách trơn lối vào Hang, Rào Gỗ, cổng 3 ấn.
- **D** (e0623d5): QA docs, light budget, pool bẫy boss, 60/30 fps, version 1.0.0 (P13).
- **Sửa review** (d02c5d7): mở cổng/lối tắt thì lưu ngay; `IBossVictorySteps.UnlockReward` trả về bool, không trao được Aura thì boss chưa bị tính là đã hạ; app vào nền khi đang chơi New Game chưa chạm bàn thờ thì không ghi đè save cũ.

Kết quả: EditMode 964/964, PlayMode 166 pass + 2 skip (test chụp ảnh cần GPU), APK dev 54.9 MB, review 8/10, evidence gate SEALED.

## Chỗ gãy

- **Ảnh chụp bị ghi đè.** Agent UI báo "đã xem, đúng", nhưng file trên đĩa là ảnh xám trơn: một lần chạy PlayMode headless sau đó (`-nographics`) đã ghi đè. Sửa: test chụp ảnh `Assert.Ignore` khi `SystemInfo.graphicsDeviceType == Null`, chụp lại với `UNITY_GRAPHICS=1` và tự xem.
- **`unity-batch.sh` luôn in `exit=0`.** `if [ "$1" != shot ]; then code=$?; fi` lấy mã của phép thử `[`, không phải của Unity. Sửa: lấy `$?` ngay sau `esac`, thử với method không tồn tại thì ra `exit=1`. Lỗi này từng làm agent phase 9 chạy test trên prefab cũ vì generator dừng giữa chừng mà không ai biết.
- **Lâu Đài quá tối.** Global Light 0.05 theo GDD làm sàn gần như không thấy trong ảnh runtime. Nâng lên 0.15, cần xác nhận trên máy thật.
- **Ảnh edit-mode lệch.** Tự tính tọa độ focus cho lệnh `shot` bị sai, ảnh ra phòng trống. Chụp ở runtime (dịch chuyển Leo vào phòng, camera đi theo) mới tin được.
- **Plan không được cập nhật.** Agent project-manager viết báo cáo nhưng để mọi phase ở `pending`, số liệu cũ. Orchestrator tự sửa frontmatter và `plan.md`.
- **Evidence gate chặn 2 lần**: verdict không nhắc lại đúng tiêu chí mới trong study-context; nguồn gốc 4 ảnh key-art chưa rõ. Người dùng xác nhận key-art là tài liệu của nhóm, được phép dùng; đã ghi vào `Art/LICENSES.md`.

## Bài học

1. Báo cáo của agent không thay cho artifact: mở file, xem mtime, tự nhìn ảnh.
2. Lấy `$?` ngay sau lệnh cần đo, không qua một `if`.
3. Sau project-manager, kiểm lại frontmatter phase và số liệu trước khi chốt.
4. Thông số ánh sáng trong GDD phải thử ở runtime, ảnh edit-mode không đủ.
5. Test hồi quy cho 3 lỗi review chưa được thấy đỏ trước khi sửa: lần sau viết và chạy test trước.

## Còn mở

- Đo fps/RAM trên 2 máy, 3 tỉ lệ màn hình, checklist `docs/qa/device-checklist.md`.
- Playtest M1–M3 với người mới (độ khó, 60–90 phút).
- Keystore nhóm, APK release, video demo.
- BUG-004: điểm yếu boss nhân đôi mọi sát thương. Chưa có hộp xác nhận New Game. Chưa có `LICENSE` gốc; quyền đăng PDF spec của giảng viên chưa xác nhận.
