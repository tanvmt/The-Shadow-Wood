# Interaction System

Tài liệu hệ thống interaction góc nhìn thứ nhất của **The Shadow Wood**: raycast từ tâm màn hình, focus và highlight object, nhận `Interact`, hiện prompt chữ world space và crosshair.

## Luồng hoạt động

```text
GameplayLifetimeScope
  ├─ Camera ◄── CameraRig.OutputCamera
  ├─ PlayerInteractorView (Player.prefab) ── InteractionConfig
  └─ PlayerInteractor : IPlayerInteractor, ITickable
        │
        │ Tick — chỉ chạy khi IInputService.Context == Gameplay
        ▼
  Camera.ViewportPointToRay(0.5, 0.5) ── RaycastNonAlloc + occlusion
        │
        ├─ focus đổi ──► InteractableBehaviour.SetFocused ──► highlight
        │             └► FocusChanged ──► InteractionPromptView (chữ world space)
        │                             └─► InteractionCrosshairUI
        │
        └─ IInputService.InteractPressed
                 │
                 ▼
           InteractableBehaviour.Interact
             ├─ DestroyOnInteract
             ├─ TVInteractable ──► ICameraFocusService (push context Focus)
             └─ PickupItem ──► IInventoryService.TryAdd
```

Context rời khỏi `Gameplay` (vào `Focus`, `UI`, `Puzzle`) thì interactor bỏ focus ngay, prompt và crosshair ẩn. `Puzzle` vẫn bật action `Interact` nhưng puzzle controller tự đọc `InteractPressed`, interactor không raycast song song.

Assembly: `Interaction` chỉ phụ thuộc `Core`. `UI` và `Inventory` tham chiếu một chiều tới `Interaction`. Contract inventory (`IInventoryService`, `ItemDefinition`) nằm ở `Core/Items`, nên feature khác dùng inventory không cần tham chiếu `Inventory`. `Player` không cần biết `Interaction`: camera được đưa vào qua registration trong scope.

## File chính

| File | Vai trò |
|---|---|
| `IPlayerInteractor.cs` | Contract cho UI: `CurrentFocus`, `FocusChanged`, `InteractionCompleted` |
| `PlayerInteractor.cs` | C# thuần, `ITickable`. Raycast, occlusion, focus state, đọc `InteractPressed` |
| `PlayerInteractorView.cs` | MonoBehaviour trên `Player.prefab`: giữ `InteractionConfig`, root có collider bị bỏ qua |
| `InteractionConfig.cs` | ScriptableObject: khoảng cách, layer, trigger, buffer, debug ray |
| `InteractableBehaviour.cs` | Base class: availability, focus feedback, prompt |
| `InteractionPromptText.cs` | Hàm thuần ghép chuỗi prompt |
| `IInteractable.cs`, `InteractionContext.cs`, `InteractionResult.cs`, `InteractionFocusState.cs` | Contract và dữ liệu của một lần tương tác |
| `InteractionHighlight.cs`, `BehaviourInteractionHighlight.cs`, `RendererMaterialInteractionHighlight.cs` | Highlight khi focus |
| `DestroyOnInteract.cs` | Interactable one-shot cho object test |
| `TVInteractable.cs` | TV phát video, camera blend vào màn hình. Xem [TVInteractable.md](TVInteractable.md) |
| `UI/InteractionPromptView.cs` | Một label TMP world space dùng chung, bám theo object đang focus |
| `UI/InteractionCrosshairUI.cs` | Chấm crosshair phản ứng theo focus |
| `Inventory/PickupItem.cs` | Nhặt `ItemDefinition` vào `IInventoryService`. Xem [FlagsInventory.md](FlagsInventory.md) |

## Đăng ký trong scope

```csharp
// GameplayLifetimeScope.Configure
builder.RegisterComponentInHierarchy<PlayerInteractorView>();
builder.RegisterComponentInHierarchy<CameraRig>();
builder.Register<Camera>(r => r.Resolve<CameraRig>().OutputCamera, Lifetime.Singleton);
builder.RegisterEntryPoint<PlayerInteractor>();

builder.RegisterComponentInHierarchy<InteractionPromptView>();
builder.RegisterComponentInHierarchy<InteractionCrosshairUI>();
```

- Camera lấy từ `CameraRig.OutputCamera`, không dùng `Camera.main`. `OutputCamera` đọc thẳng `Camera` trên GameObject của `CinemachineBrain`, vì brain chỉ cache camera trong `Awake` của nó, có thể chạy sau khi scope build.
- `RegisterEntryPoint<PlayerInteractor>()` đăng ký luôn mọi interface, nên UI resolve được `IPlayerInteractor`.
- Prompt và crosshair là view có đúng một instance và bắt buộc phải có trong `Gameplay`, nên đăng ký bằng code. Thiếu object trong scene thì scope báo lỗi ngay.

## Cấu hình Player

`Player.prefab` có `PlayerInteractorView` trên root, trỏ tới `Assets/_Project/Data/Player/InteractionConfig.asset`:

| Setting | Giá trị |
|---|---:|
| Distance | `3 m` |
| Visibility Layers | `Default`, `TransparentFX`, `Water`, `UI`, `Player`, `Interactable` |
| Trigger Interaction | `Ignore` |
| Hit Buffer Size | `16` |
| Draw Debug Ray | Tắt. Bật để xem tia trong Scene view: đỏ = không trúng, vàng = trúng nhưng không tương tác được, xanh = tương tác được |

`Visibility Layers` phải chứa cả interactable lẫn geometry chặn tầm nhìn. Nếu mask chỉ có `Interactable`, ray nhìn xuyên tường thuộc layer `Default`.

Raycast bỏ qua mọi collider nằm dưới root của `PlayerInteractorView`. Hit gần nhất được chọn thủ công vì `RaycastNonAlloc` không bảo đảm thứ tự.

## Input

Action `Interact` trong `InputSystem_Actions`:

| Device | Binding |
|---|---|
| Keyboard | `E` |
| Gamepad | `buttonNorth` |

Action không gắn interaction (`Hold`/`Tap`). Interactor đọc `IInputService.InteractPressed` (`WasPressedThisFrame`). Nhãn phím trên prompt lấy từ `IInputService.InteractBindingLabel`, theo thiết bị dùng gần nhất và không kèm tên interaction.

Khi cần thao tác giữ phím (mở khoá, đốt nhang), thời gian giữ do từng interactable khai báo và interactor tự đếm. Không gắn `Hold` vào action `Interact`. Xem kế hoạch ở [DevelopmentPlan.md](DevelopmentPlan.md).

## Đặt interactable trong scene

Interactable cần service (TV, sau này là cửa, pickup, puzzle) nhận dependency qua `[Inject] public void Construct(...)`. Để được inject, object phải nằm dưới root được khai báo trong **Auto Inject Game Objects** của `GameplayLifetimeScope`:

```text
Gameplay
 ├─ GameplayLifetimeScope    Auto Inject: [Interactables, …]
 ├─ Player
 ├─ CameraRig
 ├─ HUD (Canvas)
 │   └─ InteractionCrosshair
 ├─ InteractionPrompt
 └─ Interactables            ← mọi interactable đặt dưới đây
     ├─ InteractableItem_Base
     ├─ Pickup_Candle, Pickup_Lighter, Pickup_Flashlight
     └─ TV
```

VContainer inject toàn bộ cây con của root khi scope build. Quy ước:

- Interactable đặt **dưới root được auto-inject**. Đặt ngoài root thì `Construct` không được gọi và lỗi không hiện ra: ví dụ TV có `CanInteract == false`.
- Không dùng service được inject trong `Awake`/`OnEnable`, vì hai callback này có thể chạy trước `Construct`. Dùng từ `Start` hoặc lúc `Interact`.
- Object sinh lúc runtime tạo bằng `IObjectResolver.Instantiate(prefab)`.
- Interactable không cần service (như `DestroyOnInteract`) chạy được ở bất kỳ đâu, nhưng vẫn đặt dưới root cho thống nhất.
- Khi dựng map thật, có thể đổi root `Interactables` thành root của cả map (ví dụ `Level`, chia con theo phòng). Quy ước giữ nguyên: mọi thứ của map nằm dưới root được auto-inject.

## Prompt world space

Một `InteractionPromptView` duy nhất cho cả scene (`Assets/_Project/Prefabs/System/InteractionPrompt.prefab`, component TextMeshPro 3D). Không đặt canvas riêng trên từng interactable.

Trên mỗi `InteractableBehaviour`:

| Field | Ý nghĩa |
|---|---|
| `Prompt` | Chữ khi tương tác được. Hiển thị `[E] Xem TV`. Để trống thì không hiện prompt |
| `Blocked Reason` | Chữ khi `CanInteract == false`, không kèm phím (ví dụ `Cửa bị khoá`). Để trống thì ẩn |
| `Prompt Anchor` | Transform đặt chữ. Để trống thì đặt giữa và ngay trên đỉnh bounds của mọi renderer đang bật, cộng `Height Offset` |

`Prompt` và `BlockedReason` là `virtual`, subclass có thể trả chuỗi động (ví dụ cửa đổi lý do theo trạng thái khoá).

Hành vi của view:

- Cập nhật text khi `FocusChanged`, khi đổi context, hoặc khi đổi thiết bị (nhãn phím đổi). Không tạo chuỗi mới mỗi frame.
- Mỗi `LateUpdate` đặt lại vị trí theo target và copy rotation của camera (billboard song song mặt màn hình, chữ không bị méo phối cảnh).
- Ẩn khi không focus, context khác `Gameplay`, hoặc target bị destroy.
- AABB của object xoay xiên to hơn hình thật, chữ có thể hơi cao. Khi đó dùng `Prompt Anchor`.
- Font cần có glyph tiếng Việt. Thấy ô vuông thì tạo TMP font asset dạng dynamic từ font hỗ trợ tiếng Việt.

## Crosshair

Prefab `Assets/_Project/Prefabs/System/InteractionCrosshair.prefab`, đặt làm con của Canvas Screen Space Overlay (`HUD`). Gán `Crosshair Graphic`. Không cần kéo reference interactor: view nhận `IPlayerInteractor` và `IInputService` qua inject.

| State | Feedback |
|---|---|
| Không có target | Alpha thấp, scale 1 |
| Target tương tác được | Trắng, scale 1.35 |
| Target bị chặn | Xám |
| Tương tác thành công | Pulse ngắn |
| Context khác `Gameplay` | Ẩn |

Animation dùng `Time.unscaledDeltaTime`, vẫn chạy khi pause.

## Tạo object destroy khi tương tác

Prefab test: `Assets/_Project/Prefabs/Interactables/InteractableItem_Base.prefab`.

1. Tạo GameObject có mesh và Collider, layer `Interactable`.
2. Add `DestroyOnInteract`, điền `Prompt`.
3. Add highlight component, kéo vào `Focus Feedback Sources`.
4. Nối SFX/VFX vào event `Before Destroyed` nếu cần.
5. Đặt dưới root `Interactables`.

`DestroyOnInteract` tắt collider ngay khi thành công để tránh tương tác hai lần, rồi destroy theo delay. Không dùng cho item Inventory thật.

## Tạo pickup Inventory

Dùng `Assets/_Project/Prefabs/Interactables/PickupItem_Base.prefab`, gán `Item`, đặt dưới root `Interactables`. `PickupItem` chỉ destroy world object khi `IInventoryService.TryAdd` thành công; inventory đầy thì hiện `Blocked Reason` và object ở lại. Chi tiết: [FlagsInventory.md](FlagsInventory.md#đặt-pickup-trong-scene).

## Outline/highlight

### Fallback có sẵn

Add `RendererMaterialInteractionHighlight`:

- `Target Renderers`: renderer cần highlight.
- `Highlight Material`: `Assets/_Project/Materials/InteractionOutline.mat`.

Component cache mảng shared material ban đầu, chỉ append material outline khi focus và restore khi mất focus. Material mặc định dùng `Radial Extrusion = 1`, width `0.003`. Model hữu cơ bị phồng thì giảm `Radial Extrusion` về `0`. Pivot mesh lệch tâm thì chỉnh `Mesh Local Center`.

### Adapter cho package ngoài

Add `BehaviourInteractionHighlight`, kéo component outline của package vào `Outline Behaviours`, rồi kéo adapter vào `Focus Feedback Sources`.

## Tạo loại interactable mới

Kế thừa `InteractableBehaviour`, nhận service qua `Construct`, chỉ cài phần gameplay:

```csharp
public sealed class DoorInteractable : InteractableBehaviour
{
    [SerializeField] private Door door;

    private IFlagService _flags;

    [Inject]
    public void Construct(IFlagService flags)
    {
        _flags = flags;
    }

    public override string BlockedReason => door.IsLocked ? "Cửa bị khoá" : null;

    public override bool CanInteract(InteractionContext context)
    {
        return _flags != null && !door.IsLocked && base.CanInteract(context);
    }

    protected override InteractionResult PerformInteraction(InteractionContext context)
    {
        door.Toggle();
        return InteractionResult.Success();
    }
}
```

Không đặt raycast, input, prompt hoặc crosshair logic trong từng object.

## Quy ước layer/tag

- Layer `Player`: index 8. Layer `Interactable`: index 9.
- Interaction dựa vào component `InteractableBehaviour`, không dựa vào tag.
- Geometry có thể che item phải nằm trong `Visibility Layers`.

## Test

EditMode: `Assets/_Project/Tests/EditMode/Interaction/` (assembly `TheShadowWood.Interaction.Tests`, dùng `FakeInputService`).

`PlayerInteractorTests`:

- Focus object ở tâm viewport.
- Không focus object sau vật chắn.
- `InteractPressed` gọi target đúng một lần.
- Context khác `Gameplay` thì không focus, không tương tác.
- Đổi context thì bỏ focus.
- Target bị destroy thì `FocusChanged` báo không còn target.

`InteractionPromptTextTests`: không target, tương tác được (`[E] …`), bị chặn có lý do, bị chặn không lý do, prompt rỗng.

Test của `PickupItem` nằm ở `Tests/EditMode/Inventory/PickupItemTests.cs`, xem [FlagsInventory.md](FlagsInventory.md#kiểm-thử).

Checklist play test:

- Nhìn cube → highlight, crosshair to lên, prompt nổi trên cube.
- Nhìn lệch hoặc ra xa quá 3 m → feedback tắt.
- Item sau tường không được focus.
- `E` và gamepad North gọi đúng một lần. Dùng gamepad thì nhãn phím đổi theo.
- Cube bị destroy khi đang focus → prompt và highlight biến mất.
- Vào TV → prompt và crosshair ẩn, thoát ra thì hiện lại.
- Nhặt pickup → subtitle `Picked up: …`, object biến mất.
