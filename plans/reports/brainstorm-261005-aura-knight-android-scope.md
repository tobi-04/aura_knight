# Brainstorm — Aura Knight trên Android (2026-10-05)

## Commission
Đồ án cuối kỳ: chuyển spec "Aura Knight" (PC/Switch) sang Android, Unity 6 (6000.6.0f1) + C#, UI/asset theo `NỀN TẢNG Mobile.pdf`. Nhóm 4 người, 8 tuần.

## Quyết định (user đã chốt)
- Q: Nhân lực / thời gian → A: Nhóm 4 người, ~8 tuần
- Q: Phạm vi → A: Đủ 4 vùng như spec
- Q: Cách tổ chức 4 vùng → A: Bản đồ mở liền mạch (không chọn phương án "4 vùng gọn")
- Q: Gating → A: Hạ boss nhận Aura (Rừng→Gió, Hang→Hỏa, Đô Thị→Thủy, Lâu Đài cuối)
- Q: Asset → A: Asset pack miễn phí + key art PDF cho menu
- Q: Ngoại hình Leo → A: Theo key art trang 1
- Q: Điều khiển → A: Nút ảo + vuốt, hỗ trợ gamepad

## Paths examined
| Path | Ưu | Nhược |
|------|----|-------|
| 4 vùng gọn, mỗi vùng 5–6 phòng (khuyến nghị) | An toàn tiến độ | Ít cảm giác thế giới mở |
| 1 vùng sâu + 3 vùng ngắn | Demo vùng đầu đẹp | Các vùng sau mỏng |
| **Map mở liền mạch (được chọn)** | Đúng tinh thần Metroidvania | Tốn level design/test gấp 2–3 lần, rủi ro trễ hạn cao |

## Agreed direction
Map mở liền mạch nhưng dựng bằng room-prefab, mỗi vùng là một scene additive, nên chuyển phòng không có loading. Dùng 4 kiểu quái gốc và đổi skin theo vùng. Mỗi boss có 3 đòn, 2 phase. Có danh sách cắt P0/P1/P2, trigger cắt ở tuần 4.

## What to watch
- Mốc tuần 4: Rừng phải chơi được hết, nếu không thì cắt còn 5 phòng/vùng.
- Điều khiển cảm ứng cho platformer chính xác: playtest trên máy thật từ tuần 2.
- Độ đồng nhất asset giữa 4 vùng: khóa asset ngay tuần 1.
- Key art trong PDF (trang 1 và trang 8) đang mâu thuẫn về ngoại hình Leo; đã chốt theo trang 1.

## Success
Xem `docs/game-design-document.md` §15.2 (Definition of Done).

## Next
Spec chi tiết: `docs/game-design-document.md`. Bước tiếp theo: `/tkm:create-plan`.

## Unresolved
1. Có phải nộp lên Google Play không (AAB/target API)?
2. Có bắt buộc tự vẽ một phần asset không?
3. Bản quyền key art trong PDF.
