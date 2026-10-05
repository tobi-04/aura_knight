# Quy ước code (Aura Knight)

Chỉ ghi những gì code hiện đang làm. Thiết kế game: [`game-design-document.md`](game-design-document.md). Kiến trúc: [`system-architecture.md`](system-architecture.md).

## 1. Tổ chức code

| Quy ước | Chi tiết |
|---------|----------|
| Assembly | Runtime: một asmdef `AuraKnight` (`Scripts/`). Editor: `AuraKnight.Editor` (+ `AuraKnight.Editor.World`). Test: xem §5 |
| Namespace | Theo thư mục: `AuraKnight.Core`, `.Player` (`.Player.States`), `.Aura` (`.Aura.Skills`), `.Combat`, `.World` (`.World.Pickups`), `.UI`. Mọi thư mục con của `Editor/` dùng chung `AuraKnight.Editor` |
| Tên file | Một file một type, tên file = tên type (PascalCase). Class lớn tách bằng `partial` (`PlayerController.Api.cs`, `PlayerCombat.Reactions.cs`) |
| Cỡ file | Dưới 200 dòng mỗi file C# runtime (hiện không file nào vượt). Vài file test mô phỏng đã vượt, không lan sang code runtime |
| Class | `sealed` mặc định; chỉ bỏ `sealed` ở lớp nền trừu tượng (`PlayerStateBase`, `AuraSkillBase`, `OneTimeAuraGate`) |
| Chú thích | XML `<summary>` cho type và member public; chú thích giải thích *vì sao*, nhất là ràng buộc ngầm (thứ tự khởi tạo, hợp đồng event) |
| Tên ID | Chuỗi ổn định, lưu vào save (`hub_altar_01`, `AuraId`). Không lưu vị trí object, chỉ lưu ID |

## 2. Logic thuần + MonoBehaviour mỏng

Quy tắc tính toán đặt trong class C# thuần (không phụ thuộc Unity scene), MonoBehaviour chỉ nối dây. Nhờ vậy test được bằng EditMode, không cần Play.

Ví dụ trong code: `JumpCut`, `DashTracker`, `HorizontalMotion`, `AirPhysics` (Player); `ComboTracker`, `DamageRules`, `HitStopTimer` (Combat); `AuraState`, `AuraInteractionRules` (Aura); `RegionLoadPlan`, `RoomValidator`, `AltarValidator` (World); `JoystickMath`, `SafeAreaMath` (UI).

## 3. EventBus

- Event là `readonly struct` khai báo trong `Core/GameEvents.cs`, nhỏ, bất biến theo quy ước.
- Đăng ký trong `OnEnable` (hoặc `Bind`), hủy trong `OnDisable` (hoặc `Unbind`) tương ứng. `EventBus.SubscriberCount` dùng để test rò đăng ký.
- Danh sách subscriber là mảng copy-on-write: `Publish` không cấp phát bộ nhớ.
- **Lưu ý snapshot:** handler vừa hủy đăng ký trong lúc đang `Publish` vẫn bị gọi thêm một lần cho event đó; handler vừa đăng ký chỉ nghe event kế tiếp. Handler phải tự kiểm tra trạng thái của mình.
- Gameplay không gọi UI/Audio trực tiếp, chỉ publish.
- Thành phần cache giá trị từ `GameState` lúc khởi động phải đọc lại khi nhận `GameStateLoaded`.

## 4. Singleton

Dùng `Singleton.IsDuplicate(Instance, this)` ở đầu `Awake`; true thì `return`. Bản trùng tự tắt (nên `OnEnable` không chạy, không đăng ký gì) và tự xóa component. Đang dùng ở GameManager, RoomManager, CheckpointService, WorldEntry, AuraManager. GameManager có `[DefaultExecutionOrder(-100)]` để state sẵn sàng trước các component khác.

## 5. Test

| Loại | Vị trí | Ghi chú |
|------|--------|---------|
| EditMode | `Tests/EditMode/{Core,Player,Combat,Aura,World}` | asmdef riêng: `AuraKnight.Tests.EditMode`, `.Player`, `.Combat`, `.Aura`. Test logic thuần và bản mô phỏng. Aura mở `internal` cho test qua `InternalsVisibleTo` |
| PlayMode | `Tests/PlayMode` (asmdef `AuraKnight.Tests.PlayMode`, nền `WorldPlayTestBase`) | Vào scene `Core` thật: new game, continue, respawn xuyên vùng, chuyển phòng, save. Chạy headless được |

- Lỗi tìm được thì viết test tái hiện trước khi sửa. Không bỏ qua test đỏ.
- Lệnh chạy: `tools/unity-batch.sh test EditMode` / `test PlayMode`; `UNITY_TEST_FILTER=<tên>` thu hẹp phạm vi.

## 6. Physics layer

Định nghĩa trong `Core/PhysicsLayers.cs`; `Aura → Setup Project` (`PhysicsLayerSetup`) ghi layer vào TagManager và đặt collision matrix từ `CollidingPairs`. Không hard-code số layer; dùng `PhysicsLayers.Id/Mask/Apply`. Layer chưa tồn tại thì mask rỗng (không lỗi).

| Layer | Dùng cho | Thành phần dùng mask |
|-------|----------|----------------------|
| `Ground` | Đất, tường, vật cản đặc | `KinematicMotor2D` (mặc định `GroundMask`, prefab Player đặt rõ), `AuraInteractionProbe` (kiểm tra vật cản) |
| `Player` | Hurtbox của Leo | `Hitbox` của team khác nhắm vào |
| `Enemy` | Hurtbox của quái | `Hitbox` của Player nhắm vào (`HitMasks.TargetMask`) |
| `Hazard` | Gai, bẫy (team trung lập) | Player↔Hazard; kiếm pogo trúng được |
| `PlayerAttack` / `EnemyAttack` | Hitbox theo team | Setter `Team` của `Hitbox`/`Hurtbox` tự đặt layer (`HitMasks`) |
| `Interactable` | Trigger: bàn thờ, lối ra, nước, nhặt đồ, cổng | `AuraInteractionProbe` (`ProbeMask` = Interactable + Ground) |

Một object không được vừa có Hitbox vừa có Hurtbox khác team (validator phòng báo lỗi, chỉ quét prefab trong `Prefabs/Rooms`).

## 7. Generator là nguồn sự thật của prefab và scene test

Prefab, ScriptableObject, scene `Test_*`, scene `Core` và phòng khởi đầu của vùng được sinh bởi code trong `Scripts/Editor/**`. Sửa generator rồi chạy lại, không sửa tay file sinh ra (sẽ bị ghi đè). Menu `Aura/...` hoặc batch:

```bash
tools/unity-batch.sh exec AuraKnight.Editor.PlayerAssetGenerator.Generate
tools/unity-batch.sh exec AuraKnight.Editor.AuraAssetGenerator.Generate
tools/unity-batch.sh exec AuraKnight.Editor.WorldAssetGenerator.GenerateAll
tools/unity-batch.sh exec AuraKnight.Editor.WorldSceneGenerator.GenerateAll
tools/unity-batch.sh exec AuraKnight.Editor.RegionSceneGenerator.GenerateAll
tools/unity-batch.sh exec AuraKnight.Editor.RoomIdValidator.RunBatch
```

Scene vùng (`Region_*`) đã có phòng khởi đầu và `SunAltar` sinh tự động; người sở hữu scene phải giữ đúng bàn thờ ghi trong `RegionGraph` (`RegionNode.altarIds`). `RoomIdValidator` kiểm tra ID phòng, đồ thị vùng và bàn thờ.

## 8. Tương thích save

- `GameState.CurrentVersion` (hiện 1) được ghi vào file. File sai version bị từ chối (không rơi về `.bak`), không crash.
- Đổi cấu trúc `GameState`: thêm field có giá trị mặc định an toàn thì giữ nguyên version; đổi nghĩa hoặc xóa field thì tăng version và viết bước chuyển đổi trước khi bỏ code đọc cũ.
- `JsonUtility` không serialize dictionary: dùng danh sách struct (`PurchaseEntry`).
- Ghi file qua `ISaveStorage` (`FileSaveStorage`: tmp → replace, giữ `.bak`). Test dùng storage bộ nhớ qua `GameManager.UseSaveSystem`.
