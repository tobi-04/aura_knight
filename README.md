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
tools/unity-batch.sh test EditMode    # chạy unit test (hiện 968 test)
tools/unity-batch.sh test PlayMode    # chạy PlayMode test (vào scene Core thật, chạy headless; hiện 169 test: 167 chạy, 2 test ảnh chụp [Explicit] cần GPU nên được bỏ qua)
tools/unity-batch.sh setup            # Aura/Setup Project (layer vật lý, collision matrix, scene, build settings)
tools/unity-batch.sh exec Ns.Class.Method   # generator prefab/scene, build APK (xem docs/code-standards.md §7)
```

Ảnh chụp runtime (cần GPU, không dùng `-nographics`): `UNITY_GRAPHICS=1 UNITY_TEST_FILTER=AuraKnight.Tests.PlayMode.UI.RuntimeRoomScreenshotTests tools/unity-batch.sh test PlayMode` ghi `Logs/screenshots/runtime_room_<id>.png` (mỗi vùng một phòng và hai phòng boss); `RuntimeScreenshotTests` ghi ảnh HUD.

Chạy không tham số sẽ in cách dùng. Các lệnh dùng chung một khoá (`.unity-batch.lock`, lưu PID chủ khoá); khoá của tiến trình đã chết được tự thu hồi.

## Cài APK

Phiên bản **1.0.0** (version code 1), Android 8.0 (API 26) trở lên, ARM64. APK nằm ở `Builds/Android/` (không commit APK vào repo, `.gitignore` đã chặn `*.apk`).

Build bản dev (Development Build bật, ký bằng khóa debug, khoảng 50 MB):

```bash
tools/unity-batch.sh exec AuraKnight.Editor.BuildScript.BuildDevelopmentApk   # -> Builds/Android/AuraKnight-dev.apk
```

Bản release (`BuildReleaseApk`) cần keystore riêng và **từ chối build** (thoát mã 1) nếu Player Settings chưa chọn keystore. Keystore và mật khẩu lưu trong password manager của nhóm, không commit, không ghi vào README. Chọn keystore ở Edit > Project Settings > Player > Android > Publishing Settings (Custom Keystore), rồi chạy `tools/unity-batch.sh exec AuraKnight.Editor.BuildScript.BuildReleaseApk` hoặc menu **Aura > Build > Release APK**.

Cài lên máy Android:

1. Bật **Tùy chọn nhà phát triển** (Cài đặt > Giới thiệu về điện thoại > chạm 7 lần vào "Số bản dựng") rồi bật **Gỡ lỗi USB**.
2. Cắm cáp USB, chấp nhận "Cho phép gỡ lỗi USB" trên máy. Kiểm tra máy được thấy: `adb devices`.
3. Cài (hoặc cài đè, giữ dữ liệu): `adb install -r Builds/Android/AuraKnight-dev.apk`. Nếu báo ký khác khóa cũ thì gỡ bản cũ trước: `adb uninstall com.aurastudio.auraknight`.
4. Cài không cần adb (sideload): chép APK vào máy (USB, Drive...), mở file bằng ứng dụng Tệp, cho phép "Cài ứng dụng không rõ nguồn" cho ứng dụng đang dùng, bấm Cài đặt.
5. Mở **Aura Knight** trong danh sách ứng dụng. Xem log: `adb logcat -s Unity`.

Checklist kiểm trên máy thật (fps, RAM, vào nền, tỉ lệ màn hình): [`docs/qa/device-checklist.md`](docs/qa/device-checklist.md).

## QA

Test case, bug log, mẫu playtest từng mốc: [`docs/qa/`](docs/qa/) (`test-cases.md`, `bug-log.md`, `playtest-m1.md`, `-m2.md`, `-m3.md`, `device-checklist.md`). Các mục thủ công và đo trên máy thật **chưa chạy**; xem file tương ứng.

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

## Giấy phép

Mã nguồn và tooling theo [MIT](LICENSE). Art, âm thanh, phông và `docs/reference/` không thuộc giấy phép này: xem `Assets/_Project/Art/LICENSES.md`, `Assets/_Project/Art/Fonts/LICENSES.md`, `Assets/_Project/Audio/LICENSES.md`.

## Máy test

| Máy | Android | RAM | Chip | Ghi chú |
|-----|---------|-----|------|---------|
| _(điền)_ | | | | máy tầm trung |
| _(điền)_ | | | | máy yếu |
