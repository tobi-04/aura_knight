---
phase: 11
title: "Audio"
status: pending
effort: "6d"
owner: "D"
weeks: "2-7"
---

# Phase 11: Audio

## Context Links
- GDD §11 (BGM, SFX, kỹ thuật audio)

## Overview
- Priority: P0 (SFX cốt lõi + 1 BGM/vùng) / P1 (dynamic music 2 layer) · Status: pending
- AudioManager + pool SFX + MusicLayerController + bộ SFX tự làm bằng bfxr + BGM CC0/CC-BY.

## Key Insights
- Gameplay chỉ phát event; `AudioEventListener` map event sang clip. Programmer không phải gọi trực tiếp clip nào.
- Dynamic music: 2 AudioSource chạy đồng bộ (cùng `timeSamples`), crossfade theo số quái active trong 8 ô.

## Requirements
- SFX (GDD §11): bước chân ×2, nhảy, đáp, dash, slide, trượt tường, chém trúng/hụt, bị đánh, chết, đổi Aura ×3, skill ×3, xu, rương, bàn thờ, UI tap/back, boss gầm.
- BGM: Hub, Rừng, Hang, Đô Thị, Lâu Đài (mỗi vùng 2 layer), Boss, Ending.
- Import: BGM Vorbis + Streaming; SFX ADPCM + Decompress On Load; pitch ±5%.
- Âm lượng Music/SFX lấy từ Settings (AudioMixer exposed params).

## Architecture
```
AudioMixer: Master → Music, SFX, UI
AudioManager (Core): pool 12 AudioSource · Play(SfxId, pos)
SfxLibrary (SO): SfxId → clips[], volume, pitchVar
AudioEventListener: EventBus → SfxId
MusicLayerController: regionTrack{explore, combat}, CombatIntensity → crossfade 1 s
```

## Related Code Files
- Create `Scripts/Audio/`: `AudioManager.cs`, `SfxId.cs`, `SfxLibrary.cs`, `AudioEventListener.cs`, `MusicLayerController.cs`, `RegionMusic.cs`
- Create: `Audio/AuraKnight.mixer`, `Data/Audio/SfxLibrary.asset`, `Data/Audio/RegionMusic_*.asset`
- Create: `Audio/SFX/*.wav`, `Audio/BGM/*.ogg`; dòng license trong `Art/LICENSES.md` (dùng chung file credits)

## Implementation Steps
1. (Tuần 2) Mixer + AudioManager + SfxLibrary + SFX movement.
2. (Tuần 3–5) SFX combat, skill, Aura, UI.
3. (Tuần 3) BGM Rừng + MusicLayerController; (5–7) các BGM còn lại.
4. (Tuần 7) Mix tổng: chuẩn hóa loudness khoảng −16 LUFS cho BGM, nghe thử bằng loa điện thoại + tai nghe.

## Todo List
- [ ] Mixer + manager + pool
- [ ] SfxLibrary + listener
- [ ] SFX đủ danh sách
- [ ] BGM 7 track (+ layer)
- [ ] Dynamic music
- [ ] Mix + kiểm tra trên loa điện thoại

## Success Criteria
- Mọi hành động trong danh sách SFX đều có âm thanh; không bị cắt tiếng khi nhiều SFX phát cùng lúc.
- Chuyển explore ↔ combat không bị giật nhịp; tổng RAM audio < 40 MB.

## Risk Assessment
- Nhạc CC-BY yêu cầu ghi tên tác giả → ghi vào LICENSES.md ngay lúc tải về.

## Security Considerations
- Không áp dụng.

## Next Steps
- P13 kiểm tra âm thanh trên máy thật.
