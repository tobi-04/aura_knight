---
phase: 10
title: "UI Screens and HUD"
status: completed
effort: "10d"
owner: "C"
weeks: "2-7"
---

# Phase 10: UI Screens and HUD

## Context Links
- GDD §9.1 (tokens), §9.2 (fonts), §9.3 (ngôn ngữ thiết kế), §9.4 (danh sách màn hình), §3.1 (bố cục nút)
- Tham chiếu hình ảnh: `docs/reference/NỀN TẢNG Mobile.pdf` slide 1, 3, 8–10, 11, 12, 13

## Overview
- Priority: P0 (HUD, menu, pause, settings, game over, popup Aura) / P1 (map, cutscene, credits) · Status: completed (code; mục cần máy thật/người chơi còn mở, xem plan.md)
- Toàn bộ UI uGUI + TextMeshPro theo style slide: nền navy, thanh nhấn gold dọc, label mono `NN / TÊN`, card viền trái theo màu vùng.

## Key Insights
- Token màu và font gom vào **một `UITheme` SO** để mọi màn hình dùng chung (DRY), đổi một chỗ là áp dụng cho tất cả.
- UI **chỉ lắng nghe EventBus**, không tham chiếu trực tiếp player hay manager.
- TMP **Dynamic Font Asset** (Chakra Petch, IBM Plex Mono, Be Vietnam Pro, Barlow Condensed) để hiển thị đúng dấu tiếng Việt. Font tĩnh sẽ thiếu glyph.
- Canvas Scaler: Scale With Screen Size, reference 1920×1080, match 0.5; `SafeAreaFitter` (đã có từ P3) bọc HUD.

## Requirements
- HUD: tim, thanh NL (màu Aura hiện tại), xu (mono gold), nút MAP/Pause, vòng Aura 3 nút (khóa xám), boss HP bar.
- Màn hình: Splash, Main Menu (layout slide 1), Intro (4 khung slide 3, có Bỏ qua), Pause, Settings (âm lượng, rung, size/độ mờ nút, ngôn ngữ, chế độ tiết kiệm), Aura info (slide 8–10), Popup nhận Aura, Boss intro, Game Over, Ending + Credits (nền paper).
- Motion: fade + slide 16 px trong 200 ms; nút khi nhấn scale 0.95; mọi chữ chạy qua bảng `Localization` (vi mặc định, en là P2).
- **2026-10-08:** New Game shows confirmation dialog when save exists.

## Architecture
```
UITheme (SO): colors{...}, fonts{display, mono, body, number}, accentBarWidth=6
UIScreen (base: Show/Hide + tween) ─▶ MainMenuScreen, PauseScreen, SettingsScreen, AuraInfoScreen, GameOverScreen, CreditsScreen
UIRouter: stack màn hình, nút Back Android = pop
HUD: HeartsView, EnergyBarView, CoinsView, AuraRingView, BossHealthBarView (subscribe EventBus)
Settings ⇄ PlayerPrefs (cài đặt cá nhân, tách khỏi save game)
```

## Related Code Files
- Create `Scripts/UI/`: `UITheme.cs`, `UIScreen.cs`, `UIRouter.cs`, `UITween.cs`, `ThemedText.cs`, `ThemedAccentBar.cs`, `Localization.cs`
- Create `Scripts/UI/HUD/`: `HudController.cs`, `HeartsView.cs`, `EnergyBarView.cs`, `CoinsView.cs`, `AuraRingView.cs`, `BossHealthBarView.cs`
- Create `Scripts/UI/Screens/`: `SplashScreen.cs`, `MainMenuScreen.cs`, `IntroCutscene.cs`, `PauseScreen.cs`, `SettingsScreen.cs`, `AuraInfoScreen.cs`, `AuraUnlockPopup.cs`, `BossIntroBanner.cs`, `GameOverScreen.cs`, `CreditsScreen.cs`
- Create: `Data/UI/UITheme.asset`, `Data/UI/Strings_vi.json`, `Art/Fonts/*.ttf` + TMP Dynamic assets, `Prefabs/UI/*.prefab`
- Modify: `Scenes/MainMenu.unity` (C sở hữu), HUD prefab đặt trong `Core` (A thêm reference)

## Implementation Steps
1. (Tuần 2) UITheme + fonts TMP dynamic + ThemedText/AccentBar + HUD cơ bản (tim, NL, xu).
2. (Tuần 2) Main Menu theo slide 1 (key art bên phải, gradient mờ ranh giới).
3. (Tuần 3) AuraRingView + AuraUnlockPopup (slide 8–10).
4. (Tuần 4) Pause, Settings (kết nối size/độ mờ nút ảo), Game Over.
5. (Tuần 5–6) Boss intro + HP bar, Aura info screen.
6. (Tuần 7) Intro cutscene, Ending + Credits (đọc `LICENSES.md` do D tổng hợp).
7. Test 16:9, 19.5:9, 21:9 + máy có tai thỏ.

## Todo List
- [ ] UITheme + fonts tiếng Việt
- [ ] HUD (tim, NL, xu, ring, boss bar)
- [ ] Main Menu
- [ ] Popup Aura + Aura info
- [ ] Pause / Settings / Game Over
- [ ] Boss intro
- [ ] Intro / Ending / Credits
- [ ] Kiểm tra tỉ lệ màn hình + nút Back

## Success Criteria
- Đặt cạnh slide PDF, mỗi màn hình được người review đánh giá "cùng style".
- Không chữ tiếng Việt nào bị ô vuông hoặc vỡ dấu; UI không bị tai thỏ che trên 3 tỉ lệ màn hình.
- Nút Back Android hoạt động đúng ở mọi màn hình.

## Risk Assessment
- Overdraw do nhiều lớp UI trong suốt trên máy yếu → tách Canvas tĩnh và Canvas động, tắt Raycast Target cho ảnh không bấm được.

## Security Considerations
- Không áp dụng.

## Next Steps
- P12 Shop + Map screen dùng chung UITheme/UIScreen.
