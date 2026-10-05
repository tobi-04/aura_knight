# Aura Knight: Mảnh Vỡ Ánh Sáng

2D Action Platformer × Metroidvania cho Android. Đồ án học phần Thiết kế và Phát triển Game, nhóm Aura Studio.

- Thiết kế game (GDD): [`docs/game-design-document.md`](docs/game-design-document.md)
- Quy ước code: [`docs/code-standards.md`](docs/code-standards.md)
- Kiến trúc hệ thống: [`docs/system-architecture.md`](docs/system-architecture.md)
- Lộ trình và nhật ký thay đổi: [`docs/development-roadmap.md`](docs/development-roadmap.md), [`docs/project-changelog.md`](docs/project-changelog.md)
- Kế hoạch triển khai: [`plans/261005-2159-aura-knight-android-implementation/plan.md`](plans/261005-2159-aura-knight-android-implementation/plan.md)
- Tài liệu gốc: `docs/reference/`

## Yêu cầu

| Công cụ | Phiên bản |
|---------|-----------|
| Unity | **6000.6.0f1** (cả nhóm dùng đúng bản này) + module Android Build Support (SDK, NDK, OpenJDK) |
| Git LFS | `brew install git-lfs && git lfs install` |

## Bắt đầu

```bash
git clone https://github.com/tobi-04/aura_knight.git
cd aura_knight
git lfs pull
```

Mở thư mục gốc bằng Unity Hub (Add project from disk). Scene khởi đầu: `Assets/_Project/Scenes/Boot.unity`.

Menu **Aura → Setup Project** áp lại cấu hình chuẩn (Player Settings Android, scene, build settings). Chạy lại bao nhiêu lần cũng được.

## Dòng lệnh (batch mode)

Đóng Unity Editor trước khi chạy: script từ chối chạy (exit 75) nếu Editor đang mở project (`Temp/UnityLockfile` đang bị giữ).

```bash
tools/unity-batch.sh compile          # import + compile, in lỗi C#
tools/unity-batch.sh test EditMode    # chạy unit test (hiện 421 test)
tools/unity-batch.sh test PlayMode    # chạy PlayMode test (vào scene Core thật; hiện 20 test, chạy headless)
tools/unity-batch.sh setup            # Aura/Setup Project (layer vật lý, collision matrix, scene, build settings)
tools/unity-batch.sh exec Ns.Class.Method   # generator prefab/scene, build APK (xem docs/code-standards.md §7)
```

Chạy không tham số sẽ in cách dùng. Các lệnh dùng chung một khoá (`.unity-batch.lock`, lưu PID chủ khoá); khoá của tiến trình đã chết được tự thu hồi.

## Merge scene/prefab (UnityYAMLMerge)

`.gitattributes` đã gắn `merge=unityyamlmerge` cho `.unity`, `.prefab`, `.asset`, nhưng mỗi máy phải khai báo driver một lần (đường dẫn theo bản Unity 6000.6.0f1 của nhóm):

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge (UnityYAMLMerge)"
git config merge.unityyamlmerge.driver '"/Applications/Unity/Hub/Editor/6000.6.0f1/Unity.app/Contents/Helpers/UnityYAMLMerge" merge -p %O %B %A %A'
git config merge.unityyamlmerge.recursive binary
```

Windows: thay bằng (thường là) `C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Data\Tools\UnityYAMLMerge.exe`. Không khai báo thì git rơi về merge văn bản thường (vẫn chạy, chỉ dễ conflict hơn).

## Quy ước làm nhóm

- Nhánh: `main` (luôn chạy được) ← `dev` ← `feature/<tên>`; merge bằng Pull Request.
- Mỗi scene chỉ có **một** người sở hữu được sửa (GDD §13.1). Người khác làm việc trong prefab riêng.
- Code: mỗi file dưới 200 lines, mỗi class một trách nhiệm, namespace theo thư mục (`AuraKnight.Player`, `AuraKnight.Core`...).
- Không commit keystore, APK, thư mục `Library/`.

## Máy test

| Máy | Android | RAM | Chip | Ghi chú |
|-----|---------|-----|------|---------|
| _(điền)_ | | | | máy tầm trung |
| _(điền)_ | | | | máy yếu |
