# AURA KNIGHT: Mảnh Vỡ Ánh Sáng — Game Design Document (Android)

> Phiên bản: 1.0 · Ngày: 2026-10-05 · Nhóm: Aura Studio (4 thành viên) · Thời gian: 8 tuần
> Nguồn: `Tai_Lieu_Phan_Tich_Va_Thiet_Ke_Game_2D_v2 (1).pdf` (gameplay) + `NỀN TẢNG Mobile.pdf` (UI / art direction)

---

## 0. Quyết định đã chốt

| # | Hạng mục | Quyết định |
|---|----------|-----------|
| 1 | Nền tảng | **Android** (thay cho PC/Switch trong spec gốc), màn ngang (landscape) |
| 2 | Engine | Unity **6000.6.0f1 (Unity 6)**, C#, URP 2D Renderer |
| 3 | Phạm vi | **Đủ 4 vùng + 4 boss + 3 Aura**, bản đồ mở liền mạch |
| 4 | Cấu trúc map | Room-based seamless: phòng nối liền, chuyển phòng không loading; mỗi vùng = 1 scene additive |
| 5 | Gating | Hạ boss vùng → nhận Aura: Rừng → **Gió**, Hang → **Hỏa**, Đô Thị → **Thủy**, Lâu Đài = vùng cuối |
| 6 | Asset | Asset pack pixel art miễn phí (32×32) + key art trong PDF cho menu / splash / cutscene |
| 7 | Nhân vật | Leo theo key art trang 1: tóc nâu, giáp tối viền vàng, khăn xanh, khiên mặt trời |
| 8 | Điều khiển | Nút ảo + vuốt; hỗ trợ thêm gamepad Bluetooth |

---

## 1. Tổng quan

- **High concept:** 2D Action Platformer × Metroidvania. Leo đổi qua lại 3 Aura nguyên tố để di chuyển, giải đố và chiến đấu, thu lại 4 mảnh vỡ Mặt Trời Lõi.
- **Thời lượng chơi mục tiêu:** 60–90 phút (chơi hết lần đầu), có phòng bí mật để khuyến khích quay lại.
- **Đối tượng:** người chơi mobile thích platformer (Hollow Knight, Dead Cells, Celeste).
- **Ngôn ngữ:** Tiếng Việt (mặc định), tiếng Anh (P2).
- **Không có:** kiếm tiền, quảng cáo, online, tài khoản.

### 1.1 Thông số kỹ thuật mục tiêu

| Mục | Giá trị |
|-----|---------|
| Android tối thiểu | 8.0 (API 26) |
| Kiến trúc | IL2CPP, ARM64 (+ ARMv7 tùy chọn) |
| Tỉ lệ màn hình | 16:9 → 21:9, xử lý Safe Area (tai thỏ / đục lỗ) |
| FPS | 60 fps trên máy tầm trung (RAM 4GB, Snapdragon 6xx / Helio G8x) |
| Dung lượng APK | < 150 MB |
| Độ phân giải pixel | Pixels Per Unit = 32; Pixel Perfect Camera, reference 640×360 |

---

## 2. Cốt truyện & cách kể

- **Bối cảnh:** Hành tinh Solarus sống nhờ Mặt Trời Lõi (Core Sun). Shadow Syndicate do chúa tể Malakor cầm đầu đã phá lõi thành 4 mảnh nguyên tố, khiến hành tinh chìm vào đêm vĩnh cửu.
- **Nhân vật chính:** Leo, hiệp sĩ cuối cùng của Order of the Sun. Leo có thanh kiếm cổ và chiếc Khiên Hào Quang (Aura Shield) hấp thụ được sức mạnh từ mảnh vỡ.
- **Cách kể (giữ ở mức tối giản):**
  - Intro: 4 khung tĩnh (dùng key art PDF trang 1–3) + chữ chạy, có thể bỏ qua.
  - NPC **Tư Tế Sol** ở Đền Mặt Trời: gợi ý hướng đi + bán nâng cấp.
  - Sau mỗi boss: popup "Nhận Aura" + 1 dòng thoại.
  - Ending: Mặt Trời Lõi sáng lại, Đền chuyển sang bảng màu sáng, credits.

---

## 3. Điều khiển (Android)

### 3.1 Bố cục nút (màn ngang)

```
┌──────────────────────────────────────────────────────────────┐
│ ♥♥♥♥♥  ▓▓▓▓▓░░ Aura   ☀ 120                   [MAP] [II]     │
│                                                              │
│                                                              │
│                                              ( G )           │
│                                           ( T )  ( H )  ← vòng Aura│
│   ╭─────╮                          [SKILL]                   │
│   │  ◉  │  ← joystick động          [DASH]  [ATK]            │
│   ╰─────╯     (nửa trái màn hình)          [ JUMP ]          │
└──────────────────────────────────────────────────────────────┘
```

| Input | Hành động |
|-------|-----------|
| Joystick trái (động, xuất hiện tại chỗ chạm ở nửa trái màn hình) | Đi trái/phải; đẩy xuống khi đang đứng = ngồi; xuống + ATK trên không = chém xuống (pogo) |
| JUMP (giữ) | Nhảy căn lực; trên không với Gió = nhảy đúp; giữ khi rơi với Gió = lướt rơi chậm |
| ATK | Chém kiếm (combo 2 nhát) |
| DASH | Lướt ngang |
| Vuốt xuống trên nửa phải màn hình **hoặc** joystick xuống + DASH | Slide |
| SKILL | Kỹ năng của Aura hiện tại (tốn năng lượng) |
| Vòng Aura (3 nút tròn Gió/Hỏa/Thủy) | Chạm để đổi Aura ngay; Aura chưa mở thì xám + ổ khóa |
| MAP / II | Bản đồ / tạm dừng |

### 3.2 Quy tắc UX điều khiển

- Nút có vùng chạm tối thiểu **64 dp**; JUMP to nhất (96 dp) đặt ở góc phải dưới.
- Settings cho phép: chỉnh kích thước nút (80–130%), độ mờ nút (30–100%), kéo vị trí nút (P1).
- **Input buffer:** ghi nhớ lệnh JUMP/ATK/DASH trong 0.12 s.
- **Coyote time:** vẫn nhảy được 0.1 s sau khi rời mép đất.
- Rung (haptic) nhẹ khi trúng đòn / bị đánh, bật tắt được.
- Gamepad: New Input System, map sẵn layout Xbox (A=Jump, X=Atk, RB=Dash, Y=Skill, LB/RT=đổi Aura).

---

## 4. Hệ thống di chuyển (Grid Unit = 1 Unity unit = 32 px)

Leo cao 2 ô, rộng 1 ô. Collider: Capsule 0.8 × 1.9 (nhỏ hơn ô một chút để không kẹt mép).

| Thông số | Giá trị | Ghi chú / cách tính |
|----------|---------|---------------------|
| Tốc độ chạy | 8 u/s | Tăng tốc 0.06 s, giảm tốc 0.04 s |
| Tốc độ chạy (Aura Hỏa) | 9.6 u/s | +20% |
| Nhảy tối đa | 4.5 ô | Thời gian lên đỉnh 0.35 s |
| Trọng lực khi lên | 73.5 u/s² | g = 2h/t² = 2·4.5/0.35² |
| Vận tốc nhảy ban đầu | 25.7 u/s | v₀ = 2h/t |
| Nhảy ngắn (nhả sớm) | ≈ 2 ô | Khi nhả JUMP: vy *= 0.25 (`jumpCutMultiplier`); giữ tối thiểu 0.08 s |
| Trọng lực khi rơi | ×1.6 | Rơi nhanh hơn lên → cảm giác "nặng tay" |
| Tốc độ rơi tối đa | 20 u/s | |
| Dash | 5 ô / 0.2 s = 25 u/s | Trọng lực = 0 khi dash; i-frame 0.1 s đầu; CD 0.8 s; 1 lần dash trên không, hồi khi chạm đất/tường |
| Slide | 0.45 s, 11 u/s | Collider còn 0.8 × 0.9; đi qua khe cao 1 ô; trần thấp thì tự kéo dài slide đến khi ra khỏi khe |
| Wall slide | rơi tối đa 3 u/s | Kích hoạt khi áp tường + giữ hướng vào tường + đang rơi |
| Wall jump | lực (±11, 22) u/s | Khóa input ngang 0.15 s để bật ra khỏi tường |
| Nhảy đúp (Gió) | v = 20 u/s | 1 lần trên không |
| Lướt rơi (Gió) | rơi tối đa 3.5 u/s | Giữ JUMP khi đang rơi |
| Dưới nước (không có Thủy) | tốc độ ×0.5, nhảy ×0.6 | Thanh oxy 8 s, hết oxy mất 1 tim/2 s |
| Dưới nước (Aura Thủy) | tốc độ bình thường, bơi tự do 8 hướng | Thở vô hạn |

> **Kiếm khi bơi:** bản cài đặt hiện cho phép chém kiếm khi đang bơi (trạng thái `Swim` chuyển sang `AirAttack`, vẫn giữ điều khiển bơi rồi quay lại `Swim`). *Quyết định này chờ xác nhận của nhóm; spec gốc chưa nói rõ.*

**State machine của Leo:** `Idle, Run, Jump, Fall, WallSlide, WallJump, Dash, Slide, Attack, AirAttack, Hurt, Dead, Swim`, kèm 2 cờ phụ là `Grounded` và `CanDash`.

---

## 5. Chiến đấu

### 5.1 Chỉ số người chơi

| Chỉ số | Khởi đầu | Tối đa | Nâng cấp |
|--------|----------|--------|----------|
| Máu (tim) | 5 | 9 | +1 tim / lần mua ở Shop |
| Năng lượng Aura | 100 | 200 | +25 / lần mua |
| Sát thương kiếm | 1 | 3 | Rèn kiếm ở Shop (2 cấp) |

- **Kiếm:** combo 2 nhát (0.25 s/nhát), tầm 1.5 ô. Có chém lên (joystick lên) và chém xuống trên không (pogo nảy lên 3 ô khi trúng quái/gai).
- **Bị đánh:** mất 1 tim (boss 1–2), đẩy lùi 3 ô, i-frame 1.0 s (nhân vật nhấp nháy).
- **Hồi năng lượng:** mỗi nhát chém trúng +8; đứng gần Bàn Thờ Mặt Trời thì hồi đầy.
- **Hồi máu:** Bàn Thờ Mặt Trời (checkpoint) hồi đầy; quái rơi "Giọt Sáng" hồi 1 tim (tỉ lệ 10%).
- **Chết:** hồi sinh ở Bàn Thờ gần nhất, giữ nguyên xu và tiến trình (không phạt, KISS). Quái thường trong vùng hồi lại.

### 5.2 Khiên

Khiên Hào Quang là phần thể hiện hình ảnh của Aura: Leo giơ khiên khi dùng SKILL, khiên đổi màu theo Aura đang dùng. Chặn đòn chủ động (giữ nút Block) để ở mức **P2**, vì thêm nút sẽ làm màn hình chật.

---

## 6. Core Aura System

| | **Gió (Wind)** | **Hỏa (Fire)** | **Thủy (Water)** |
|-|----------------|----------------|------------------|
| Màu giáp / glow | Emerald Green `#27D38C` | Sunset Crimson `#FF5C57` | Deep Ocean `#27B5F7` |
| Nhận được ở | Hạ boss Rừng Xanh | Hạ boss Hang Đá | Hạ boss Đô Thị Hơi Nước |
| Bị động | Nhảy đúp, lướt rơi chậm | Chạy nhanh +20%, miễn nhiễm hơi nóng | Bơi tự do, thở vô hạn, miễn nhiễm axit nhẹ |
| SKILL (tốn năng lượng) | **Vòng Gió Lốc**: đẩy lùi quái trong bán kính 2.5 ô, 1 dmg, 25 NL | **Cầu Lửa**: đạn bay thẳng 12 ô, 2 dmg, 30 NL | **Khiên Nước**: hấp thụ 1 đòn bất kỳ trong 6 s, 35 NL |
| Tương tác môi trường | Đứng trên Luồng Gió để bay lên | Đốt Bụi Gai Độc / Rào Gỗ Cổ; kích Cơ Quan Nhiệt | Dập Bẫy Lửa / tạm đông Dung Nham thành bệ đứng 4 s |
| Ánh sáng | Light2D bán kính 4 ô | Bán kính 5 ô (sáng nhất) | Bán kính 4 ô |

- **Đổi Aura:** chạm vòng Aura → đổi ngay, CD 0.3 s. Có hiệu ứng bùng sáng 0.2 s + SFX riêng cho từng Aura.
- **Trước khi có Aura đầu tiên:** Leo ở trạng thái "Không Aura", giáp màu xám, glow vàng nhạt bán kính 3 ô.
- **Lâu Đài Bóng Tối:** ngoài bán kính glow là bóng tối hoàn toàn, nên Aura Hỏa (sáng nhất) cũng là lời giải cho phần "cần nguồn sáng".
- **Kiến trúc code:** mỗi Aura là một `AuraDefinition` (ScriptableObject) chứa màu, chỉ số và prefab skill. `AuraManager` giữ Aura hiện tại và phát event `OnAuraChanged`, để player, light, UI và cơ quan cùng lắng nghe.

---

## 7. Thế giới & màn chơi

### 7.1 Cấu trúc bản đồ

```
                    [LÂU ĐÀI BÓNG TỐI] ← cần Gió + Hỏa + Thủy
                            │
 [RỪNG XANH] ──── [ĐỀN MẶT TRỜI (hub)] ──── [HANG ĐÁ] ← cần Gió (vách cao 6 ô)
  (mở sẵn)                  │
                    [ĐÔ THỊ HƠI NƯỚC] ← cần Hỏa (rào gỗ cổ)
```

- Mỗi vùng là một **scene** (`Region_Forest`, ...) và được load additive khi Leo đến gần biên. Scene `Core` (Player, Camera, UI, Managers) luôn được giữ.
- Mỗi **phòng** là một prefab có `Tilemap` + `RoomBounds` (Cinemachine Confiner) + danh sách cửa. Đi qua mép phòng thì camera blend sang phòng mới trong 0.3 s, không có màn hình loading.
- **Bàn Thờ Mặt Trời** (checkpoint + save) đặt ở hub, đầu mỗi vùng và trước phòng boss.
- **Đường tắt (shortcut):** mỗi vùng có ít nhất 1 cửa một chiều mở từ bên trong, giúp đi về hub nhanh.

### 7.2 Chi tiết 4 vùng

| Vùng | Số phòng | Điều kiện vào | Bẫy / cơ quan | Quái | Boss → phần thưởng | Bí mật khi quay lại |
|------|----------|---------------|---------------|------|--------------------|---------------------|
| **Đền Mặt Trời** (hub) | 3 | — | — | — | — | Shop, NPC Tư Tế, bàn thờ lớn hiển thị số mảnh vỡ |
| **Rừng Xanh Aura** | 8 | Mở sẵn | Bụi gai nhọn, nền đất sụt lún (sập 0.6 s sau khi đứng lên, hồi lại sau 3 s) | Nấm Độc Nhảy, Bọ Gai | **Gốc Cây Mục Bóng Tối** → Aura Gió + Mảnh vỡ 1 | Luồng Gió lên tán cây (Gió); bụi gai độc chặn rương (Hỏa) |
| **Hang Đá Vô Tận** | 8 | Vách đá nhẵn cao 6 ô ở lối vào, không bám tường được (nhảy thường tối đa 4.5 ô; nhảy đúp ≈ 7.2 ô) | Thạch nhũ rơi (rung 0.5 s rồi rơi), hầm chông, hồ nước ngầm | Dơi Hút Máu, Nhện Đá Bò Tường | **Nhện Đá Khổng Lồ** → Aura Hỏa + Mảnh vỡ 2 | Hồ ngầm sâu (Thủy) |
| **Đô Thị Hơi Nước** | 8 | Rào Gỗ Cổ ở hub (cần Hỏa) | Ống xả hơi nóng (bật/tắt chu kỳ 2 s), piston dập, bể axit, khu ngập nước | Robot Tuần Tra, Rác Cơ Khí Phóng Điện | **Cỗ Máy Nổi Loạn** → Aura Thủy + Mảnh vỡ 3 | Khu ngập sâu dẫn tới rương nâng cấp (Thủy) |
| **Lâu Đài Bóng Tối** | 7 | Cổng 3 ấn: Luồng Gió + đuốc Hỏa + hào nước Thủy | Bóng tối (chỉ thấy trong glow), sàn gai chuyển động, bẫy lửa | Hiệp Sĩ Bóng Đêm, Bóng Ma | **Chúa Tể Malakor** (boss cuối) → Ending | — |

**Tổng cộng 34 phòng, đã tính 4 phòng boss** (cột "Số phòng" gồm cả phòng boss; trong code: Hub 3, Rừng 7 + boss, Hang 7 + boss, Đô Thị 7 + boss, Lâu Đài 6 + boss, xem [`level-map.md`](level-map.md)). Kích thước phòng chuẩn là 40 × 22 ô (vừa 2 màn hình); phòng dọc (trục leo) là 22 × 44 ô.

### 7.3 Quái thường (4 kiểu gốc, đổi skin theo vùng)

| Kiểu gốc | Hành vi | Biến thể theo vùng | HP | Dmg | Xu rơi |
|----------|---------|--------------------|----|-----|--------|
| **Walker** | Đi tuần giữa 2 điểm, quay đầu ở mép vực / tường; thấy Leo trong 6 ô thì lao tới | Bọ Gai (Rừng), Robot Tuần Tra (Đô Thị), Hiệp Sĩ Bóng Đêm (Lâu Đài: chặn đòn mặt trước, phải đánh sau lưng hoặc slide qua) | 2 / 4 / 6 | 1 | 3–5 |
| **Hopper** | Nhảy cung về phía Leo mỗi 1.5 s | Nấm Độc Nhảy (Rừng) | 2 | 1 | 3 |
| **Flyer** | Lơ lửng, lao chéo vào Leo rồi bay lên lại | Dơi Hút Máu (Hang: hút trúng thì hồi 1 HP), Bóng Ma (Lâu Đài: đi xuyên tường) | 2 / 4 | 1 | 4–6 |
| **Crawler / Static** | Bò dọc tường/trần hoặc đứng yên phóng điện theo chu kỳ | Nhện Đá (Hang), Rác Cơ Khí (Đô Thị: phóng điện vòng 2 ô mỗi 3 s) | 3 / 4 | 1 | 4 |

AI viết bằng state machine đơn giản `Patrol → Detect → Attack → Cooldown → Hurt → Dead`. Thông số đặt trong `EnemyStats` (ScriptableObject).

### 7.4 Boss (mỗi boss có 3 đòn, 2 phase; phase 2 bắt đầu ở 50% HP thì nhanh hơn 25% và thêm 1 biến thể đòn)

| Boss | HP | Đòn 1 | Đòn 2 | Đòn 3 | Điểm yếu / mẹo |
|------|----|-------|-------|-------|----------------|
| **Gốc Cây Mục Bóng Tối** (64×64) | 30 | Rễ đâm từ đất (có báo trước 0.6 s bằng bụi đất) | Ném 3 quả hạt độc theo cung | Quét cành ngang (phải nhảy) | Đánh vào lõi sáng khi nó há miệng sau đòn 3 |
| **Nhện Đá Khổng Lồ** | 40 | Bò trần rồi rơi đập xuống | Phun tơ làm chậm 50% | Gọi 2 nhện nhỏ | Dùng Gió đẩy nhện nhỏ; dash qua dưới bụng |
| **Cỗ Máy Nổi Loạn** | 50 | Piston dập 3 cột | Laser quét ngang (slide để tránh) | Xả hơi nóng toàn sàn (đứng lên bệ, hoặc dùng Hỏa để miễn nhiễm) | Cầu Lửa vào lò hơi phía sau gây ×2 dmg |
| **Chúa Tể Malakor** | 70 (3 phase) | Chém bóng tối tầm xa | Dịch chuyển + đâm | Phase 3: tắt hết ánh sáng phòng, Leo phải đổi Aura theo màu đòn đánh (khiên Thủy chặn, Hỏa chiếu sáng, Gió né trên không) | Dùng đủ 3 Aura |

Boss HP bar hiện ở cạnh dưới màn hình, theo style UI ở mục 9.

*Đối chiếu code (`Data/Bosses/*.asset`, `BossAttackSetup`):* HP 30 / 40 / 50 / 70 khớp bảng. Phase 2 ở 50% HP (nhanh x1.25, mọi đòn báo trước tối thiểu 0.5 s). Malakor vào phase 3 ở **25% HP** (bóng tối + `AuraColorStrikeAttack`). Điểm yếu nhân x2 sát thương hiện áp cho **mọi** loại sát thương, không riêng Cầu Lửa (BUG-004, mở). Số đòn là first-pass, chưa cân bằng bằng người chơi thật.

---

## 8. Tiến trình & kinh tế

- **Xu Mặt Trời (☀):** rơi từ quái, rương, bình vỡ. Hút về Leo khi ở gần trong 2 ô.
- **Shop (Tư Tế Sol ở hub):**

| Món | Giá | Số lần |
|-----|-----|--------|
| +1 Tim | 100 / 200 / 300 / 400 | 4 |
| +25 Năng lượng | 120 / 240 / 360 / 480 | 4 |
| Rèn kiếm (+1 dmg) | 300 / 600 | 2 |
| Bản đồ chi tiết vùng (hiện phòng chưa đi) | 50 mỗi vùng | 4 |

- **Rương bí mật:** 8 rương (2 rương/vùng), mỗi rương 100–150 xu hoặc 1 nâng cấp miễn phí.
- **Cân bằng:** đi hết đường chính (không tìm bí mật) kiếm được khoảng 1500 xu, đủ mua 2 tim + 2 năng lượng + 1 kiếm.

---

## 9. UI / UX — theo `NỀN TẢNG Mobile.pdf`

### 9.1 Design tokens

| Token | Màu | Dùng cho |
|-------|-----|----------|
| `bg/night` | `#070D1F` | Nền chính mọi màn hình menu |
| `bg/panel` | `#0E1A2C` | Panel, ô, popup |
| `bg/paper` | `#F1F0E5` | Màn hình sáng (ending, credits, thẻ vùng) |
| `bg/paper-alt` | `#D9D3C7` | Thẻ thứ cấp trên nền sáng |
| `accent/gold` | `#FFC857` | Viền nhấn dọc, label section, xu, tiêu đề phụ, nút chính |
| `aura/wind` | `#27D38C` | Gió, Rừng Xanh, subtitle |
| `aura/fire` | `#FF5C57` | Hỏa, Lâu Đài, cảnh báo, tim |
| `aura/water` | `#27B5F7` | Thủy, Đô Thị, năng lượng |
| `text/primary` | `#FFFFFF` | Chữ trên nền tối |
| `text/muted` | `#8A93A6` | Chữ phụ, số trang, nút disable |
| `text/ink` | `#0C1824` | Chữ trên nền sáng |

Màu vùng: Rừng = wind · Hang = gold · Đô Thị = water · Lâu Đài = fire (theo slide 12).

### 9.2 Typography (đều có trên Google Fonts và hỗ trợ tiếng Việt)

| Vai trò | Font | Ví dụ |
|---------|------|-------|
| Display / tiêu đề lớn | **Chakra Petch Bold**, IN HOA | `AURA KNIGHT`, `SOLARUS` |
| Label / số / mã section | **IBM Plex Mono Bold**, IN HOA, giãn chữ +15% | `01 / ĐỊNH VỊ`, `BOSS / NHỆN ĐÁ`, `☀ 120` |
| Nội dung / mô tả | **Be Vietnam Pro Regular** | Mô tả Aura, thoại NPC |
| Số HUD lớn | **Barlow Condensed Bold** | Giá shop, đồng hồ |

TextMeshPro: tạo Font Asset dạng **Dynamic** để render đủ dấu tiếng Việt. Pixel font cho damage number trong game là tùy chọn.

### 9.3 Ngôn ngữ thiết kế (rút từ slide)

- **Bố cục chia đôi:** panel navy bên trái chứa chữ, bên phải là key art hoặc hiện trường, ranh giới giữa hai bên được làm mờ (gradient).
- **Thanh nhấn dọc** 6 px màu `accent/gold` (hoặc màu Aura) chạy dọc bên trái khối tiêu đề.
- **Label section** dạng `NN / TÊN` bằng font mono, màu gold.
- **Footer:** `AURA KNIGHT / NN` mono màu muted ở góc trái dưới, kèm một gạch ngang ngắn 120 px màu nhấn ở góc phải dưới.
- **Thẻ (card):** nền phẳng, viền trái 4 px theo màu vùng, không bo góc (hoặc bo 2 px), không đổ bóng.
- **Danh sách đánh số** `01  Nhảy đúp trên không` (số mono + chữ Be Vietnam).
- Motion: fade + trượt 16 px trong 200 ms; nút khi nhấn scale 0.95.

### 9.4 Danh sách màn hình

| Màn hình | Nội dung | Tham chiếu slide |
|----------|----------|------------------|
| **Splash** | Logo "Aura Studio" rồi "AURA KNIGHT" fade | — |
| **Main Menu** | Trái: label `ĐỀ XUẤT...` → thay bằng `KỴ SỸ ÁNH SÁNG`, title lớn, thanh gold, các nút `TIẾP TỤC / TRÒ CHƠI MỚI / CÀI ĐẶT / GIỚI THIỆU`. Phải: key art Leo + mặt trăng (slide 1) | Slide 1 |
| **Intro cutscene** | 4 khung tĩnh có số `01–04` như slide 3, chữ chạy, nút `BỎ QUA` | Slide 3 |
| **HUD** | Tim (đỏ), thanh Aura (màu Aura hiện tại), xu (gold, mono), nút MAP/Pause, điều khiển ảo (mục 3) | — |
| **Pause** | Panel navy mờ 85%, thanh gold, `TIẾP TỤC / CÀI ĐẶT / VỀ MENU` | — |
| **Bản đồ** | Nền là bản đồ Solarus kiểu slide 11. Phòng đã đi được tô theo màu vùng, phòng chưa đi bị ẩn, có icon bàn thờ/boss/rương; pinch để zoom | Slide 11 |
| **Aura** (xem trong Pause) | 3 tab Gió/Hỏa/Thủy theo layout slide 8–10: trái là danh sách `01..04` kỹ năng, phải là ảnh minh họa | Slide 8–10 |
| **Shop** | Danh sách món dạng thẻ viền trái, giá mono gold, nút MUA | Slide 12 |
| **Nhận Aura** (popup) | Full màn, label màu Aura (vd `EMERALD GREEN`), tên `Aura Gió`, danh sách 4 kỹ năng, key art slide 8 | Slide 8–10 |
| **Boss intro** | Thanh chữ ngang: `BOSS / GỐC CÂY MỤC` mono, màu vùng | Slide 13 |
| **Game Over** | "ÁNH SÁNG LỤI TẮT", nút `HỒI SINH TẠI BÀN THỜ` | — |
| **Settings** | Âm lượng nhạc/SFX, rung, kích thước và độ mờ nút, ngôn ngữ | — |
| **Ending + Credits** | Nền `bg/paper` (sáng lại). Credits bắt buộc ghi tác giả các asset pack | Slide 12 style |

---

## 10. Mỹ thuật & asset

- **Phong cách:** pixel art 16-bit, tông tối, điểm nhấn là glow màu Aura (slide 15–16).
- **Kích thước:** nhân vật và tile 32×32 (Leo khoảng 32×64), boss 64×64 đến 128×128. PPU = 32, filter Point, không nén màu (Compression: None với sprite nhỏ).
- **Nguồn asset (miễn phí):** itch.io, OpenGameArt, Kenney. Tiêu chí chọn:
  1. License cho phép dùng (CC0 / CC-BY / free commercial), **lưu link + license vào `Assets/_Project/Art/LICENSES.md`**.
  2. Cùng PPU và cùng độ "dày" pixel. Không trộn pack 16 px với pack 32 px.
  3. Có đủ animation cho Leo: idle, run, jump, fall, attack ×2, dash, slide, wall-slide, hurt, death.
  - Gợi ý tìm: `metroidvania pixel tileset forest/cave/industrial/castle`, các pack của ansimuz (Gothicvania, Legacy Fantasy, Warped). **Cần kiểm tra license từng pack trước khi dùng.**
- **Leo:** chọn sprite hiệp sĩ gần nhất rồi recolor theo key art trang 1 (tóc nâu, giáp `#1B2233` viền `#C99A3B`, khăn xanh). Glow Aura làm bằng Light2D + đổi màu viền giáp qua shader/material (tint), **không vẽ lại 3 bộ sprite**.
- **Key art trong PDF** (Leo, 3 Aura, bản đồ Solarus, hang) dùng cho Main Menu, popup Aura, nền Map, Loading.
- **Parallax 4 lớp:** Foreground ×1.2 · Action ×1.0 · Midground ×0.5 · Background ×0.1.
- **Ánh sáng:** URP 2D Light; Global Light theo vùng (Rừng 0.6, Hang 0.25, Đô Thị 0.45, Lâu Đài **0.15**); Shadow Caster 2D trên tile đá (P1, có thể tắt trên máy yếu).
  - *Ghi chú (2026-10-08):* Lâu Đài ban đầu là 0.05, nhưng ảnh chụp runtime cho thấy bệ đứng gần như vô hình nên code (`RegionLightingTable.Castle`) đặt 0.15, vẫn là vùng tối nhất. **Cần xác nhận trên máy thật** (độ sáng màn hình khác nhau, xem `docs/qa/bug-log.md` BUG-001). Phase 3 của Malakor làm mờ mọi Global Light xuống 0.04 (`DarkPhaseController`).

---

## 11. Âm thanh

| Loại | Chi tiết |
|------|----------|
| BGM | 1 track/vùng + hub + boss + ending. Mỗi track vùng có 2 layer (Explore / Combat), crossfade 1 s khi có quái trong 8 ô. Nguồn: nhạc CC0/CC-BY (ghi credits) hoặc tự làm chiptune |
| SFX (bfxr + Audacity) | Bước chân ×2 mặt đất, nhảy, đáp đất, dash, slide, trượt tường, chém trúng / chém hụt, bị đánh, chết, đổi Aura ×3, skill ×3, nhặt xu, mở rương, bàn thờ, UI tap/back, boss gầm |
| Kỹ thuật | `AudioManager` singleton, pool 12 AudioSource, SFX random pitch ±5%. Định dạng: BGM Vorbis streaming, SFX WAV/ADPCM decompress-on-load |

---

## 12. Kiến trúc kỹ thuật

### 12.1 Package Unity

Theo `Packages/manifest.json` (Unity 6):

| Package | Ghi chú |
|---------|---------|
| `com.unity.render-pipelines.universal` 17.6.0 | URP, 2D Renderer; `Light2D` nằm trong assembly `Unity.RenderPipelines.Universal.2D.Runtime` |
| `com.unity.inputsystem` 1.20.0 | Input System |
| `com.unity.cinemachine` 6.6.0 | Cinemachine tích hợp sẵn trong Unity 6 (namespace `Unity.Cinemachine`: `CinemachineCamera`, `CinemachineConfiner2D`) |
| `com.unity.2d.tilemap.extras` 9.0.0 | Rule Tile |
| `com.unity.2d.pixel-perfect` 6.0.0 | Đã khai báo; chưa có camera Pixel Perfect trong cảnh |
| `com.unity.2d.animation` 16.0.0 | Dùng nếu boss cần rig |
| `com.unity.ugui` 2.6.0 | TextMeshPro nằm trong package này (assembly `Unity.TextMeshPro`), không còn package TMP riêng |

### 12.2 Cấu trúc thư mục

```
Assets/_Project/
  Art/        (Characters, Enemies, Bosses, Tilesets/<Region>, Backgrounds, UI, LICENSES.md)
  Audio/      (BGM, SFX)
  Data/       (Auras/*.asset, Enemies/*.asset, Bosses/*.asset, ShopItems/*.asset)
  Prefabs/    (Player, Enemies, Hazards, Interactables, Rooms/<Region>/Room_XX.prefab, UI)
  Scenes/     (Boot, MainMenu, Core, Region_Hub, Region_Forest, Region_Cave, Region_City, Region_Castle, Test/Test_Movement|Test_Aura|Test_Rooms)
  Scripts/    (một assembly runtime `AuraKnight`; namespace theo thư mục; mục đánh dấu * là kế hoạch, chưa có code)
    Core/        Bootstrapper, GameManager, GameMode, GameState, SaveSystem, ISaveStorage, FileSaveStorage, SceneLoader,
                 RegionLoader, EventBus, GameEvents, Singleton, PhysicsLayers, Haptics
    Player/      PlayerController (+Api), PlayerStateMachine, States/*, PlayerStats, PlayerInputReader, PlayerCombat (+Reactions),
                 KinematicMotor2D, PlayerMovementConfig, PlayerActionRules
    Aura/        AuraManager, AuraDefinition (SO), AuraState, AuraInteractionProbe, Skills/WindGustSkill|FireballSkill|WaterShieldSkill
    Combat/      Health, Hitbox, Hurtbox, DamageInfo, Knockback, HitStop, EnergyPool, ComboTracker
    World/       Room, RoomManager, RoomExit, RoomRegistry, RegionGraph, RegionLoadPlan, SunAltar, CheckpointService, Shortcut,
                 WorldEntry, Interactables/* (OneTimeAuraGate, BurnableGate, ExtinguishableGate, WindLiftZone, HeatVent, WaterVolume...), Pickups/*
    UI/          VirtualControls/* (đã có); HUD, MapScreen, ShopScreen, PauseMenu, AuraPopup, SettingsMenu*
    Enemies/*    EnemyBase, EnemyStateMachine, Walker, Hopper, Flyer, Crawler
    Bosses/*     BossBase, BossPhase, RootTree, StoneSpider, RogueMachine, Malakor
    Audio/*      AudioManager, MusicLayerController
    Editor/      assembly `AuraKnight.Editor` (+ `.Editor.World`); Aura/, Player/, World/: generator prefab/scene/asset, ProjectSetup, BuildScript
  Tests/
    EditMode/    Core, Player, Combat, Aura, World (asmdef riêng: `AuraKnight.Tests.EditMode[.Player|.Combat|.Aura]`)
    PlayMode/    asmdef `AuraKnight.Tests.PlayMode`: chạy vào scene Core thật
```

Mỗi file C# dưới 200 dòng, mỗi class một trách nhiệm.

### 12.3 Luồng chính

- `Boot` → load `MainMenu`. Menu gọi `WorldEntry.StartNewGame()` / `Continue()` (UI menu thuộc phase 10). `WorldEntry` đặt `GameMode.Loading`, đọc/tạo `GameState`, load `Region_X` của `lastAltarId` additive cạnh `Core`, tạo (hoặc dùng lại) Player, vào phòng của bàn thờ rồi đặt `GameMode.Playing`. Bàn thờ không tìm được thì quay về bàn thờ Hub.
- Scene `Core` chứa Camera (Cinemachine) và `Managers` (GameManager, CheckpointService, RoomManager, RegionLoader, WorldEntry). Player là prefab do `WorldEntry` tạo ra, không nằm sẵn trong scene.
- `GameMode`: `Menu, Playing, Paused, Cutscene, Loading`. Điều khiển Player chỉ hoạt động khi `Playing`.
- **EventBus (struct event):** `RoomEntered`, `PlayerDamaged`, `PlayerDied`, `PlayerRespawned`, `HeartsChanged`, `EnergyChanged`, `CoinsChanged`, `AuraChanged`, `AuraUnlocked`, `BossDefeated`, `CheckpointReached`, `GameSaved`, `GameStateLoaded` → UI và Audio lắng nghe, gameplay không gọi UI trực tiếp. `GameStateLoaded` báo `GameState` vừa bị thay (game mới / tiếp tục): thành phần nào đã cache giá trị từ save thì đọc lại.
- **Cổng Aura:** `OneTimeAuraGate` (lớp nền, lưu id đã mở vào `openedGates`) với `BurnableGate` (Hỏa đốt) và `ExtinguishableGate` (Thủy dập); `WindLiftZone`, `HeatVent`, `LavaFreezable`, `WindCurrent` là các cơ quan riêng. Mỗi thành phần nhận tương tác từ skill qua `AuraInteractionProbe`. (Thay cho một component `AuraGate` đa năng trong bản thiết kế đầu.)
- **Physics layer** (định nghĩa bởi `Aura → Setup Project`): `Ground, Player, Enemy, Hazard, PlayerAttack, EnemyAttack, Interactable`. Va chạm: Ground↔Player/Enemy; Player↔Interactable/Hazard/EnemyAttack; PlayerAttack↔Enemy/Hazard.

### 12.4 Save system

- File JSON ở `Application.persistentDataPath/save_0.json`, có 1 slot (3 slot là P2).
- Ghi nguyên tử: ghi file `.tmp` rồi thay vào file chính, bản cũ giữ thành `save_0.json.bak` (chỉ khi bản cũ còn đọc được). File chính thiếu / rỗng / hỏng thì đọc từ `.bak`; file sai `version` thì bị từ chối, không dùng `.bak`.
- Tự lưu khi chạm Bàn Thờ (bàn thờ đang đứng đã lưu rồi thì không ghi lại), khi hạ boss và khi app vào nền (`OnApplicationPause(true)`, quan trọng trên Android).
- `shopPurchases` lưu dạng danh sách `{key, count}` vì `JsonUtility` không serialize được dictionary.

```json
{
  "version": 1,
  "lastAltarId": "forest_altar_02",
  "maxHearts": 6, "maxEnergy": 125, "swordLevel": 1, "coins": 340,
  "unlockedAuras": ["Wind"], "currentAura": "Wind",
  "defeatedBosses": ["RootTree"],
  "visitedRooms": ["hub_01", "forest_01", "forest_02"],
  "openedChests": ["forest_chest_a"], "openedShortcuts": ["forest_sc_1"], "openedGates": ["hub_wood_barricade"],
  "shopPurchases": [{"key": "heart", "count": 1}, {"key": "energy", "count": 1}, {"key": "map_forest", "count": 1}],
  "playTimeSeconds": 1834
}
```

### 12.5 Tối ưu Android

- Sprite Atlas theo vùng; chỉ giữ atlas của vùng hiện tại và vùng kề.
- Object pool cho đạn, xu, VFX.
- `Application.targetFrameRate = 60` (30 khi "Chế độ tiết kiệm"; vsync 0 vì Android bỏ qua `targetFrameRate` khi bật vsync). Chưa có Shadow Caster trong game, nên chế độ tiết kiệm hiện chỉ giảm số Light2D (8 xuống 4, `LightBudget`). Chi tiết: `system-architecture.md` §12.
- Kiểm tra mỗi tuần trên **ít nhất 2 máy thật** (1 máy yếu); dùng Unity Profiler qua USB.

---

## 13. Phân công & lịch 8 tuần

### 13.1 Vai trò & quyền sở hữu file (tránh conflict Git)

| Thành viên | Vai trò | Sở hữu |
|-----------|---------|--------|
| A | Lead + Gameplay Programmer | `Scripts/Player`, `Scripts/Aura`, `Scripts/Combat`, `Scripts/Core`, scene `Core` |
| B | Level Designer + Enemy/Boss Programmer | `Prefabs/Rooms/*`, `Scenes/Region_*`, `Scripts/World`, `Scripts/Enemies`, `Scripts/Bosses` |
| C | Art + UI | `Art/*`, `Prefabs/UI`, `Scripts/UI`, scene `MainMenu` |
| D | Audio + QA + Build | `Audio/*`, `Scripts/Audio`, test case, build APK, `LICENSES.md`, credits |

- Unity: bật **Visible Meta Files** + **Force Text**; dùng `.gitignore` Unity chuẩn; dùng **Git LFS** cho `*.png, *.wav, *.ogg, *.psd, *.aseprite`.
- Chỉ người sở hữu scene mới sửa scene đó. Người khác sửa bằng cách tạo prefab riêng.
- Nhánh: `main` (bản chạy được) ← `dev` ← `feature/<tên>`; merge qua PR.

### 13.2 Lịch

| Tuần | Mốc | A (Gameplay) | B (Level/AI) | C (Art/UI) | D (Audio/QA) |
|------|-----|--------------|--------------|------------|--------------|
| 1 | Setup + chọn asset | Project, packages, Input, movement cơ bản | Grey-box Hub + 2 phòng Rừng, room transition | Chọn & khóa asset pack 4 vùng + Leo, recolor Leo | Repo/LFS, build APK đầu tiên lên máy thật |
| 2 | Movement hoàn chỉnh | Dash, slide, wall jump, coyote/buffer, điều khiển ảo | Walker/Hopper AI, Rừng 8 phòng grey-box | HUD + Main Menu theo token | SFX movement, test case movement |
| 3 | Combat + Aura Gió | Kiếm, Health/Hitbox, AuraManager + Gió | Boss Gốc Cây, hazard Rừng | Tileset + parallax Rừng, popup Aura | BGM Rừng 2 layer |
| 4 | **Mốc 1: Rừng chơi được hết** | Save/Checkpoint, Shop logic | Hang Đá 8 phòng + Flyer/Crawler | Tileset Hang, Map screen | Playtest vòng 1 + báo lỗi |
| 5 | Aura Hỏa + Đô Thị | Aura Hỏa + AuraGate | Boss Nhện, Đô Thị 8 phòng | Tileset Đô Thị, Shop UI | SFX combat/skill |
| 6 | **Mốc 2: 3 vùng** | Aura Thủy + bơi/nước | Boss Cỗ Máy, Lâu Đài 7 phòng | Tileset Lâu Đài, Pause/Settings | Playtest vòng 2 trên máy yếu |
| 7 | **Mốc 3: content complete** | Lighting Lâu Đài, tối ưu | Boss Malakor, bí mật/rương | Cutscene intro/ending, credits | BGM còn lại, dynamic music |
| 8 | Polish + nộp | Sửa bug, cân bằng | Cân bằng độ khó | Polish UI/VFX | Test toàn bộ, build release APK, video demo |

**Feature freeze cuối tuần 7.** Tuần 8 chỉ sửa bug, không thêm tính năng.

---

## 14. Ưu tiên & danh sách cắt giảm

| Ưu tiên | Nội dung | Nếu trễ |
|---------|----------|---------|
| **P0** (bắt buộc) | Movement đầy đủ, kiếm, 3 Aura + gating, 4 vùng (tối thiểu 5 phòng/vùng) + 4 boss, save, HUD, menu, điều khiển ảo, APK chạy ổn định | — |
| **P1** | Đủ 34 phòng, shop đầy đủ, map screen, dynamic music, Shadow Caster, rương bí mật, cutscene | Cắt phòng (giữ 5/vùng), map chỉ hiện phòng đã đi |
| **P2** | Kéo vị trí nút, tiếng Anh, 3 save slot, chặn đòn bằng khiên, gamepad tinh chỉnh, phase 3 Malakor | Bỏ |

**Trigger cắt:** cuối tuần 4 mà Rừng chưa chơi được hết thì lập tức giảm còn 5 phòng/vùng cho các vùng sau.

---

## 15. Kiểm thử & tiêu chí hoàn thành

### 15.1 Test case trọng tâm (D viết chi tiết)

- Nhảy nhấp nhẹ ≈ 2 ô, giữ = 4.5 ô (đo bằng gizmo lưới trong scene `Test_Movement`).
- Dash đi đúng 5 ô; bị đánh trong 0.1 s đầu dash thì không mất máu; CD 0.8 s.
- Slide qua khe cao 1 ô; slide dừng giữa khe thì không kẹt, không đứng dậy xuyên trần.
- Wall jump liên tục leo được trục cao 20 ô.
- Mỗi AuraGate chỉ mở bằng đúng Aura yêu cầu; không thể vào Hang khi chưa có Gió (kể cả dùng dash + wall jump để lách, phải thử).
- Chuyển phòng qua lại nhanh 50 lần không lỗi camera / không mất player.
- Thoát app giữa chừng (Home, khóa màn hình, cuộc gọi đến) rồi mở lại thì không mất tiến trình tính từ Bàn Thờ gần nhất.
- Đa tỉ lệ màn hình 16:9 / 19.5:9 / 21:9: UI không bị tai thỏ che.

### 15.2 Definition of Done

- Chơi từ đầu đến ending trên máy Android thật không crash, không có softlock.
- ≥ 55 fps trung bình trên máy tầm trung, không tụt dưới 30 fps khi đánh boss.
- Toàn bộ asset có license ghi trong `LICENSES.md` và màn credits.
- Có APK release + video demo 3–5 phút + báo cáo.

---

## 16. Rủi ro

| Rủi ro | Mức | Giảm thiểu |
|--------|-----|-----------|
| Bản đồ mở 4 vùng vượt khả năng 8 tuần | **Cao** | Room-prefab, 4 kiểu quái gốc, trigger cắt ở tuần 4 (mục 14) |
| Điều khiển cảm ứng khó chơi platformer chính xác | Cao | Coyote/buffer, nút to, playtest từ tuần 2, giảm độ khó vùng đầu |
| Asset pack không đồng nhất giữa 4 vùng | Trung bình | Khóa asset ngay tuần 1; chọn pack cùng tác giả nếu được; dùng Light2D + màu để thống nhất tông |
| Conflict scene Unity khi làm nhóm | Trung bình | Quyền sở hữu scene (13.1), Force Text, room prefab |
| Hiệu năng Light2D + Shadow trên máy yếu | Trung bình | Chế độ tiết kiệm, test máy yếu mỗi tuần |
| Key art PDF khác style sprite trong game | Thấp | Chỉ dùng key art ở menu / popup, không dùng trong gameplay |

---

## 17. Câu hỏi còn mở

1. Giảng viên có yêu cầu nộp lên Google Play không? Nếu có thì cần build AAB, target API mới nhất và privacy policy; nếu không thì APK là đủ.
2. Có yêu cầu bắt buộc tự vẽ một phần asset không? (Ảnh hưởng đến việc dùng 100% asset pack.)
3. ~~Key art trong PDF do ai tạo, có được phép dùng trong bản nộp không?~~ Đã chốt 2026-10-08: tài liệu của nhóm, được phép dùng (xem `Assets/_Project/Art/LICENSES.md`).
