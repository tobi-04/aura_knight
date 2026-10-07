# Bản đồ màn chơi

Nguồn sự thật là các file `Assets/_Project/Data/Levels/<Vùng>/<id>.room.txt`: `LevelGenerator` dựng prefab phòng, scene `Region_*`, danh sách bàn thờ của `RegionGraph` từ chúng, và màn bản đồ (`RoomMapDataBuilder`) đọc đúng vị trí các phòng đã đặt. Tài liệu này mô tả nội dung các file đó; sửa file rồi chạy lại `tools/unity-batch.sh exec AuraKnight.Editor.LevelGenerator.GenerateAll` (hoặc `Aura/Regenerate All`), không sửa tay prefab phòng hay scene vùng.

Số phòng (khớp GDD 7.2, mỗi vùng tính cả phòng boss): Hub 3, Rừng 7 + boss, Hang 7 + boss, Đô Thị 7 + boss, Lâu Đài 6 + boss = **34**. Phòng chuẩn 40 × 22 ô, phòng dọc 22 × 44 (`forest_04`, `city_04`, `castle_04`).

## 1. Sơ đồ thế giới

```
                          cổng 3 ấn (hub_03 cửa 3)
                                   │
 [boss]◄[07]◄[06]◄[05]◄[04▲]◄[03]◄[02]◄[01] ─ hub_01 ─ hub_02 ─ hub_03 ─► [01]─[02]─[03]─[04▲]─[05]─[06]─[07]─[boss]
   Rừng (đi sang trái, mở sẵn)                                    │          Hang (đi sang phải, vách nhẵn 6 ô ngay phòng 01)
                                                                  │ Rào Gỗ (hub_03 cửa 4)
                                                                  ▼
                                              Đô Thị [01]─[02]─[03]─[04▲]─[05]─[06]─[07]─[boss]
 Lâu Đài (sau cổng 3 ấn): [01]─[02]─[03]─[04▲]─[05]─[06]─[boss]
```

`▲` là phòng dọc. Mỗi vùng chạy thẳng một hàng (ô lưới `slot: cột,hàng`, cột cách nhau 40 đơn vị, hàng cách nhau 30 để mỗi phòng chiếm ô bản đồ riêng); phòng dọc chiếm hai hàng. Rừng đi về phía tây nên cột âm. Các vùng cách nhau 2000 đơn vị trong thế giới (hub 0,0; Rừng -2000,0; Hang 2000,0; Đô Thị 0,-2000; Lâu Đài 0,2000) để vùng bound không bao giờ chồng nhau; màn bản đồ tự xếp vùng quanh hub (`MapLayout`).

Đường chính: hub_01 (bàn thờ đầu game) → Rừng → boss Gốc Cây Mục (Gió) → hub_03 → Hang qua vách 6 ô (nhảy đúp) → boss Nhện Đá (Hỏa) → hub_03, đốt Rào Gỗ → Đô Thị → boss Cỗ Máy (Thủy) → hub_03, thắp 3 ấn → Lâu Đài → Malakor.

## 2. File phòng

```
id: forest_01            region: forest          slot: 0,0       size: 40x22         title: ...
doors: 1=hub_01 2=forest_02 7=forest_07          (chữ số trong lưới = dải cửa ở cột trái/phải, tối thiểu 3 ô cao)
altar: forest_altar_01   ids: C=chest_forest_02 T=thorn_forest_05 k=shortcut_forest   (id theo thứ tự đọc của từng nhóm ký hiệu)
rewards: chest_forest_02=Heart                  (rương cho nâng cấp miễn phí; mặc định 100-150 xu)
requires: chest_forest_02=fire chest_forest_01=wind 4=fire     (đích chỉ tới được khi có Aura này; test kiểm)
preload: hub             zones: city@24,0,9,7   (vùng cần preload thêm khi Leo đứng trong hình chữ nhật x,y,w,h ô)
---
<lưới: dòng đầu là hàng trên cùng; ô (0,0) là góc dưới trái>
```

Cửa của A dẫn tới B đặt Leo ở spawn `from_A` của B, cách cửa 3 ô; phòng boss chỉ có spawn `default`. Spawn không bao giờ nằm trong cửa nó vừa đi qua, nên không có vòng lặp qua lại.

| Ký hiệu | Ý nghĩa | Ký hiệu | Ý nghĩa |
|---|---|---|---|
| `.` `#` | trống, đất (collider hộp gộp, layer Ground) | `W` | vách nhẵn: đặc nhưng không bám, không wall jump (`SmoothWall`) |
| `=` | bệ một chiều (`PlatformEffector2D`) | `^` | gai sàn (1 tim, Hazard layer, pogo được) |
| `K` | hố chết (sát thương 99: về bàn thờ) | `c` | đất sụt (rung 0.6 s, hồi sau 3 s) |
| `s` | thạch nhũ treo trần (rung 0.5 s rồi rơi) | `h` | hơi nóng 2 s bật/2 s tắt (`HeatVent`, miễn nhiễm với Hỏa) |
| `P` | piston treo trần (báo trước 0.6 s) | `a` | axit (miễn nhiễm với Thủy) |
| `M` | sàn gai chạy ngang (±3 ô, 2.5 ô/s) | `f` | bẫy lửa (Thủy dập, `ExtinguishableGate`) |
| `~` | nước (bơi tự do cần Thủy) | `Y` | luồng gió lên (chỉ đẩy khi mang Gió) |
| `A` | Bàn Thờ Mặt Trời | `C` | rương bí mật |
| `T` | bụi gai (Hỏa đốt) | `Z` | Rào Gỗ Cổ (Hỏa đốt) |
| `G` | cổng 3 ấn | `Q` | ấn Gió / Hỏa / Thủy (id `seal_wind`...) |
| `k` `j` | cửa đường tắt cao 4 ô, mở từ bên trong (`k`: bên trái, `j`: bên phải) | `N` | Tư Tế Sol + shop |
| `b` `m` | Bọ Gai, Nấm Độc Nhảy (Rừng) | `B` `S` | Dơi, Nhện Đá (Hang; `S` là một dải ≥3 ô = đường bò) |
| `p` `z` | Robot Tuần Tra, Rác Cơ Khí (Đô Thị) | `n` `g` | Hiệp Sĩ Bóng Đêm, Bóng Ma (Lâu Đài) |
| `1`-`9` | dải cửa | | |

## 3. Bảng phòng

### Hub (sáng nhất, Global Light 1.0)

| Phòng | Ô lưới | Cỡ | Tên | Cửa | Nội dung | Cổng/bí mật |
|---|---|---|---|---|---|---|
| `hub_01` | (0,0) | 40x22 | Sun temple, start | `1→forest_01` `2→hub_02` | bàn thờ `hub_altar_01` |  |
| `hub_02` | (1,0) | 40x22 | Shop and Sol | `1→hub_01` `2→hub_03` | Tư Tế Sol + shop |  |
| `hub_03` | (2,0) | 40x22 | Gates: Cave, Castle seals, City barricade | `1→hub_02` `2→cave_01` `3→castle_01` `4→city_01` |  | 4=fire, 3=seals, seal_wind=wind |

`hub_01`: Leo xuất hiện ở bàn thờ, cửa trái sang Rừng. `hub_02`: Tư Tế Sol (đẩy cần lên để nói chuyện, mở shop). `hub_03`: ba cổng. Nhìn từ cửa trái vào:
- **Cửa 2 → Hang** (cao 8 ô, trên nóc hành lang): đi lên bằng ba bệ, mở sẵn.
- **Cửa 4 → Đô Thị** (hành lang dưới): Rào Gỗ `gate_hub_barricade` rộng 2, cao 5 từ sàn tới tấm trần của hành lang, nên không nhảy qua, không bám qua; chỉ cháy khi dùng Cầu Lửa (Hỏa).
- **Cửa 3 → Lâu Đài** (trên cao bên trái): `gate_hub_castle` kín tới trần. Ba ấn: Hỏa trên bệ giữa, Thủy trong hồ nông (`~`), Gió trên bệ cao 6 ô so với nóc hành lang (chỉ nhảy đúp tới được). Mỗi ấn sáng khi Leo đứng trên nó **đang mang đúng Aura**; chưa mở khóa Aura thì không mang được, nên cần cả ba Aura.
- Vùng preload: `hub_03` mặc định nạp Hang; đứng gần Rào Gỗ nạp thêm Đô Thị, đứng gần cổng ấn nạp thêm Lâu Đài (`RegionPreloadZone`).

### Rừng Xanh Aura (Global Light 0.6, đi về bên trái)

| Phòng | Ô lưới | Cỡ | Tên | Cửa | Nội dung | Cổng/bí mật |
|---|---|---|---|---|---|---|
| `forest_01` | (0,0) | 40x22 | Entry glade | `1→hub_01` `2→forest_02` `7→forest_07` | bàn thờ `forest_altar_01`; cửa tắt `shortcut_forest`; 1 Bọ Gai |  |
| `forest_02` | (-1,0) | 40x22 | Thorn spikes | `1→forest_01` `2→forest_03` | 1 Bọ Gai, 2 Nấm Độc; bẫy: gai |  |
| `forest_03` | (-2,0) | 40x22 | Rotten bridge | `1→forest_02` `2→forest_04` | bẫy: hố chết, đất sụt |  |
| `forest_04` | (-3,0) | 22x44 | Hollow tree climb | `1→forest_03` `2→forest_05` | bẫy: gai, đất sụt |  |
| `forest_05` | (-4,0) | 40x22 | Thorn hill | `1→forest_04` `2→forest_06` | rương `chest_forest_02`; 1 Bọ Gai, 1 Nấm Độc; bẫy: gai | chest_forest_02=fire |
| `forest_06` | (-5,0) | 40x22 | Updraft block | `1→forest_05` `2→forest_07` | rương `chest_forest_01`; 2 Bọ Gai, 1 Nấm Độc; bẫy: gai | chest_forest_01=wind |
| `forest_07` | (-6,0) | 40x22 | Rest before the Rotten Stump | `1→forest_06` `7→forest_01` `8→forest_boss` | bàn thờ `forest_altar_02`; 1 Bọ Gai |  |

### Hang Đá Vô Tận (Global Light 0.25)

| Phòng | Ô lưới | Cỡ | Tên | Cửa | Nội dung | Cổng/bí mật |
|---|---|---|---|---|---|---|
| `cave_01` | (0,0) | 40x22 | Smooth wall | `1→hub_03` `2→cave_02` `7→cave_07` | bàn thờ `cave_altar_01`; cửa tắt `shortcut_cave` | 2=wind |
| `cave_02` | (1,0) | 40x22 | Stalactite hall | `1→cave_01` `2→cave_03` | 2 Dơi; bẫy: gai, thạch nhũ |  |
| `cave_03` | (2,0) | 40x22 | Sinkhole | `1→cave_02` `2→cave_04` | rương `chest_cave_01`; 1 Nhện Đá | chest_cave_01=water |
| `cave_04` | (3,0) | 40x22 | Spider ceiling | `1→cave_03` `2→cave_05` | 2 Nhện Đá; bẫy: gai, thạch nhũ |  |
| `cave_05` | (4,0) | 40x22 | Thorn hill | `1→cave_04` `2→cave_06` | rương `chest_cave_02`; 1 Dơi; bẫy: gai, thạch nhũ | chest_cave_02=fire |
| `cave_06` | (5,0) | 40x22 | Gauntlet | `1→cave_05` `2→cave_07` | 3 Dơi; bẫy: gai, thạch nhũ |  |
| `cave_07` | (6,0) | 40x22 | Rest before the Stone Spider | `1→cave_06` `7→cave_01` `8→cave_boss` | bàn thờ `cave_altar_02`; 1 Dơi |  |

### Đô Thị Hơi Nước (Global Light 0.45)

| Phòng | Ô lưới | Cỡ | Tên | Cửa | Nội dung | Cổng/bí mật |
|---|---|---|---|---|---|---|
| `city_01` | (0,0) | 40x22 | Boiler gate | `1→hub_03` `2→city_02` `7→city_07` | bàn thờ `city_altar_01`; cửa tắt `shortcut_city`; 1 Robot Tuần Tra; bẫy: hơi nóng |  |
| `city_02` | (1,0) | 40x22 | Piston hall | `1→city_01` `2→city_03` | 1 Robot Tuần Tra, 1 Rác Cơ Khí; bẫy: piston |  |
| `city_03` | (2,0) | 40x22 | Acid tanks and the flooded shaft | `1→city_02` `2→city_04` | rương `chest_city_01`; 1 Robot Tuần Tra; bẫy: axit | chest_city_01=water |
| `city_04` | (3,0) | 22x44 | Steam shaft | `1→city_03` `2→city_05` | 1 Rác Cơ Khí; bẫy: hơi nóng |  |
| `city_05` | (4,0) | 40x22 | Updraft vats | `1→city_04` `2→city_06` | rương `chest_city_02`; 1 Robot Tuần Tra, 1 Rác Cơ Khí; bẫy: axit | chest_city_02=wind |
| `city_06` | (5,0) | 40x22 | Gauntlet | `1→city_05` `2→city_07` | 1 Robot Tuần Tra; bẫy: hơi nóng, piston |  |
| `city_07` | (6,0) | 40x22 | Rest before the Rogue Machine | `1→city_06` `7→city_01` `8→city_boss` | bàn thờ `city_altar_02`; 1 Robot Tuần Tra |  |

### Lâu Đài Bóng Tối (Global Light 0.05: ngoài vòng sáng Aura là tối)

| Phòng | Ô lưới | Cỡ | Tên | Cửa | Nội dung | Cổng/bí mật |
|---|---|---|---|---|---|---|
| `castle_01` | (0,0) | 40x22 | Gatehouse | `1→hub_03` `2→castle_02` `7→castle_06` | bàn thờ `castle_altar_01`; cửa tắt `shortcut_castle`; 1 Hiệp Sĩ Bóng Đêm, 1 Bóng Ma |  |
| `castle_02` | (1,0) | 40x22 | Spiked floors | `1→castle_01` `2→castle_03` | 1 Hiệp Sĩ Bóng Đêm; bẫy: sàn gai |  |
| `castle_03` | (2,0) | 40x22 | Fire trap closet | `1→castle_02` `2→castle_04` | rương `chest_castle_01`; 2 Bóng Ma; bẫy: sàn gai, bẫy lửa | chest_castle_01=water |
| `castle_04` | (3,0) | 22x44 | Watch tower | `1→castle_03` `2→castle_05` | 2 Bóng Ma; bẫy: gai |  |
| `castle_05` | (4,0) | 40x22 | Updraft crypt | `1→castle_04` `2→castle_06` | rương `chest_castle_02`; 2 Hiệp Sĩ Bóng Đêm; bẫy: sàn gai | chest_castle_02=wind |
| `castle_06` | (5,0) | 40x22 | Rest before Malakor | `1→castle_05` `7→castle_01` `8→castle_boss` | bàn thờ `castle_altar_02` |  |

Phòng boss (`<vùng>_boss`) do `BossAssetGenerator` dựng (xem phase 8) rồi `BossRoomLinker` bỏ tường trái, đặt `RoomExit` ở sát mép trái dẫn về phòng bàn thờ trước đó, thay hộp xám bằng tile của vùng và thêm 4 lớp parallax. Cửa vào là cửa số 8 của phòng 07 (Lâu Đài: 06), Leo xuất hiện ở `default` (3.5, 2). Cửa của trận đấu đóng khi bắt đầu, nên lối ra phía sau chỉ mở khi boss thua hoặc Leo chết (hồi sinh ở bàn thờ trước phòng).

## 4. Cổng (gating) vượt hẳn giới hạn di chuyển

| Cổng | Hình dạng | Giới hạn di chuyển (GDD 4) | Cần |
|---|---|---|---|
| Hang: vách nhẵn `cave_01` | 3 ô dày, **6 ô cao**, dựng từ sàn, không bám được, phía trên để trống | nhảy thường 4.5 ô cao, nhảy + dash ≈ 10 ô xa; nhảy đúp ≈ 7.2 ô cao | Gió (nhảy đúp) |
| Đô Thị: Rào Gỗ `hub_03` | 2 × 5 ô từ sàn tới trần hành lang | không đi qua vật đặc | Hỏa (Cầu Lửa) |
| Lâu Đài: cổng ấn `hub_03` | 2 × 3 ô trong ngõ cụt có trần, 3 ấn | | Gió + Hỏa + Thủy |
| Rương Gió | ống 3 ô rộng, 8 ô cao, vách nhẵn hai bên, luồng gió ở trong, hốc rương ở đỉnh | | Gió |
| Rương Thủy | ống nước cùng hình dạng | nhảy trong nước ×0.6 | Thủy |
| Rương Hỏa | bụi gai (hoặc bẫy lửa của Lâu Đài, cần Thủy) bít lối vào gian kín dưới đồi | | Hỏa (Thủy) |

Cách chứng minh (đều là test, chạy trong EditMode / PlayMode):
- **Bộ giải lưới** (`LevelReachability`): hai mô hình di chuyển. `Safe` (3 ô lên, 4 ô ngang, nhảy đúp 6 ô lên) chỉ để chứng minh mỗi cửa, bàn thờ, rương tới được từ mọi điểm vào khi có đủ Aura. `Max` (4 ô lên, 10 ô ngang kể cả dash, nhảy đúp 7 ô lên + lướt) dùng để chứng minh đích gated **không tới được** khi thiếu đúng Aura đó (mỗi dòng `requires`), và khi không có Aura nào. Phát hiện được lỗi thật khi viết: một bệ cạnh vách Hang làm vách thành bậc thang.
- **Mô phỏng vật lý bằng `PlayerController` thật** (`GatePhysicsTests`): đặt collider của `cave_01` thật, chạy 70 phiên ngẫu nhiên × 14 s và lưới hơn 1000 tổ hợp (điểm chạy đà × lúc nhảy × lúc dash) không có Gió: không lần nào qua vách; đứng sát vách bấm nhảy liên tục 20 s: không leo. Có Gió: một thời điểm nhảy đúp thắng. Đối chứng: cùng vách nhưng không có `SmoothWall` thì wall jump leo qua được (đó là lý do `SmoothWall` tồn tại).
- **Đồ thị** (`LevelProgression`): đi từ bàn thờ hub, mỗi boss cho Aura của nó. Không có Aura: chỉ Hub, Rừng (kể cả boss) và `cave_01`. Có Gió: cả Hang, chưa có Đô Thị. Gió + Thủy không mở Rào Gỗ. Hỏa mở Đô Thị. Cần cả ba cho Lâu Đài. Cuối cùng cả 34 phòng và Malakor đều tới được.
- **PlayMode** (`GatePlayModeTests`): Rào Gỗ chỉ cháy bằng `Burn` + Hỏa (Gió/Thủy/không Aura/`Extinguish` đều bị từ chối), mất collider khi cháy và vẫn mở sau Continue; cổng ấn chỉ mở khi cả ba ấn sáng, ấn không sáng khi mang Aura khác, Aura chưa có không mang được; trạng thái lưu sau Continue.

## 5. Bí mật (rương, 2 mỗi vùng)

| Rương | Phòng | Cần | Phần thưởng |
|---|---|---|---|
| `chest_forest_01` | `forest_06` (ống Gió) | Gió | 100-150 xu |
| `chest_forest_02` | `forest_05` (bụi gai trong đồi) | Hỏa | +1 tim tối đa |
| `chest_cave_01` | `cave_03` (hố ngầm) | Thủy | +25 năng lượng tối đa |
| `chest_cave_02` | `cave_05` (bụi gai trong đồi) | Hỏa | +25 năng lượng tối đa |
| `chest_city_01` | `city_03` (khu ngập sâu) | Thủy | +1 tim tối đa |
| `chest_city_02` | `city_05` (ống Gió) | Gió | 100-150 xu |
| `chest_castle_01` | `castle_03` (bẫy lửa bít gian kín) | Thủy | +25 năng lượng tối đa |
| `chest_castle_02` | `castle_05` (ống Gió) | Gió | 100-150 xu |

Rương Rừng/Hang cần Aura nhận được ở cuối vùng hoặc ở vùng sau; Đô Thị cần Thủy (boss của chính vùng) và Gió; Lâu Đài không còn Aura "sau" nên dùng Thủy và Gió, đều đã có khi tới đó.

## 6. Đường tắt

Mỗi vùng có một đường tắt từ phòng bàn thờ trước boss (`<vùng>_07`, Lâu Đài `castle_06`, cửa 7) về góc trên-trái của phòng đầu vùng (`<vùng>_01`, cửa 7), bên trong một ngăn kín có cửa `k`/`j` mở **từ bên trong**: đi từ ngoài vào thì cửa đặc, nên không thể đi tắt trước khi tới phòng 07; tới ngăn từ phòng 07 thì Leo đi lại gần cửa là cửa mở vĩnh viễn (`shortcut_forest`, `shortcut_cave`, `shortcut_city`, `shortcut_castle`, lưu trong `openedShortcuts`) và ngăn thông cả hai chiều. Test: ngăn không vào được từ ngoài khi cửa đóng (mô hình `Max`), cả bốn đường tắt mở sau khi đi hết đường chính, cửa đặc ở bên ngoài trong scene thật.

## 7. Bẫy theo vùng (GDD 7.2)

| Vùng | Bẫy trong phòng |
|---|---|
| Rừng | gai, đất sụt 0.6 s / hồi 3 s (cầu mục `forest_03` trên hố chết, tháp `forest_04`) |
| Hang | thạch nhũ (rung 0.5 s rồi rơi, 1 tim), gai, hố ngầm (nước) |
| Đô Thị | hơi nóng 2 s bật/tắt (dùng lại `HeatVent`), piston (báo trước 0.6 s), axit, khu ngập |
| Lâu Đài | bóng tối (Global Light 0.05), sàn gai chạy ngang, bẫy lửa (dùng lại `ExtinguishableGate`) |

Không phòng nào giam Leo: hố hoặc là có đường lên, hoặc là `K` (sát thương 99) đưa Leo về bàn thờ cuối cùng đã chạm. Không bẫy nào nằm trong 2 ô và không quái nào nằm trong 7 cột quanh điểm Leo xuất hiện khi qua cửa (validator).

## 8. Kiểm tra tự động

- `LevelValidator` (chạy khi sinh và trong bước `validators`): id và kích thước, cửa hai chiều, ký hiệu đứng trên mặt đất, tối đa 6 quái mỗi phòng, an toàn quanh điểm đến, mọi cửa/bàn thờ/rương tới được, mọi cổng không bị lách, ngăn đường tắt kín, đi thử toàn game.
- `LevelPrefabValidator`: mọi `RoomExit` trỏ tới phòng thật và spawn có thật, id lưu (`PersistentId`) không trùng, bàn thờ khớp file, 4 phòng boss có đường về.
- Có sẵn từ phase trước và vẫn chạy: `RoomIdValidator`, `EnemyRoomLimitValidator`.
- PlayMode đi qua **từng cửa của cả 5 vùng** trong scene thật (vào, về, spawn đúng, không bật ngược), kể cả preload vùng cho `hub_03`.

## 9. Giới hạn đã biết

- Layout chỉ được kiểm bằng bộ giải lưới, mô phỏng vật lý và test tự động; **chưa có người chơi thử trên thiết bị**, nên khoảng cách nhảy, độ khó và thời lượng 60-90 phút của đường chính chưa được đo.
- Gai và quái là mục tiêu pogo của kiếm (quy tắc có sẵn từ phase 4): quái và gai không đặt gần vách Hang, nhưng đây là một cách "nhún" cao hơn nhảy thường nếu đặt sai chỗ; bộ giải không mô hình hóa pogo.
- Cổng gated chỉ được chứng minh trong phạm vi mô hình đã nêu (và mô phỏng vật lý cho vách Hang); các cổng còn lại là vật đặc kín trần nên không phụ thuộc vào số đo nhảy.
- Art là tile/parallax đã có từ phase 2 (script tự sinh, CC0/project-owned), chưa có decor thêm; sprite của rào, bụi gai, ấn, piston, bàn thờ... là khối màu (greybox).
