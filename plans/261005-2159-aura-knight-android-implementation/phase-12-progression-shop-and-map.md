---
phase: 12
title: "Progression Shop and Map"
status: pending
effort: "5d"
owner: "A (logic), C (UI)"
weeks: "4-5"
---

# Phase 12: Progression Shop and Map

## Context Links
- GDD §8 (xu, shop, rương, cân bằng), §9.4 (Shop, Bản đồ), §12.4 (GameState)

## Overview
- Priority: P0 (xu + shop tim/NL) / P1 (rèn kiếm, mua bản đồ, map screen đầy đủ, rương) · Status: pending
- Kinh tế xu, Shop của Tư Tế Sol, rương bí mật, màn hình bản đồ.

## Key Insights
- Món hàng là dữ liệu (`ShopItem` SO: giá theo cấp, số lần mua, hiệu ứng). Shop UI tự sinh từ danh sách này.
- Map screen vẽ bằng dữ liệu phòng (vị trí lưới trong `RoomMapData` SO) thay vì chụp ảnh, nên tô được phòng đã đi qua.

## Requirements
- Shop: +1 tim (100/200/300/400), +25 NL (120/240/360/480), rèn kiếm (300/600), bản đồ vùng (50).
- Rương: 8 rương (2/vùng), 100–150 xu hoặc 1 nâng cấp miễn phí, có `PersistentId`.
- Map: hiện phòng đã đi (`visitedRooms`); phòng chưa đi chỉ hiện khi đã mua bản đồ vùng; icon bàn thờ/boss/rương/vị trí Leo; pinch zoom + kéo.
- NPC Tư Tế Sol: thoại gợi ý theo tiến trình (vùng tiếp theo cần đi).

## Architecture
```
Wallet (coins, OnCoinsChanged) ─ GameState.coins
ShopItem SO[] ─▶ ShopService.TryBuy(item) ─▶ PlayerStats / GameState.shopPurchases ─▶ Save
RoomMapData SO (roomId, gridRect, region) ─▶ MapScreen (UI Toolkit or uGUI RawImage + cells)
NpcSol: DialogueByProgress SO
```

## Related Code Files
- Create `Scripts/Progression/`: `Wallet.cs`, `ShopItem.cs`, `ShopService.cs`, `TreasureChest.cs`, `RoomMapData.cs`
- Create `Scripts/UI/Screens/`: `ShopScreen.cs`, `ShopItemView.cs`, `MapScreen.cs`, `MapCellView.cs`
- Create `Scripts/World/Interactables/`: `NpcSol.cs`, `DialogueByProgress.cs`
- Create: `Data/ShopItems/*.asset`, `Data/Map/RoomMapData_*.asset`, `Prefabs/Interactables/TreasureChest.prefab`
- Modify: `GameState.cs`, `PlayerStats.cs` (max tim/NL/swordLevel từ save)

## Implementation Steps
1. Wallet + CoinPickup nối với GameState.
2. ShopItem SO + ShopService + ShopScreen (card theo slide 12).
3. TreasureChest + PersistentId.
4. RoomMapData (B nhập từ `docs/level-map.md`) + MapScreen.
5. NpcSol thoại theo tiến trình.
6. Cân bằng: đi đường chính thu khoảng 1500 xu (D đo trong playtest).

## Todo List
- [ ] Wallet
- [ ] Shop service + UI
- [ ] Rương
- [ ] Map data + screen
- [ ] NPC Sol
- [ ] Cân bằng xu

## Success Criteria
- Mua món → chỉ số tăng ngay, lưu lại sau khi khởi động lại; không mua quá số lần, không âm xu.
- Map hiển thị đúng phòng đã đi, đúng vị trí Leo.

## Risk Assessment
- Map screen tốn thời gian → P0 chỉ cần lưới ô vuông màu vùng; đẹp hơn (nền bản đồ slide 11) để P1.

## Security Considerations
- Game offline nên người chơi có thể sửa save. Chấp nhận, không cần chống.

## Next Steps
- P13 cân bằng kinh tế.
