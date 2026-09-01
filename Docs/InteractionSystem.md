# Interaction System

Tài liệu cấu hình hệ thống interaction góc nhìn thứ nhất của **The Shadow Wood**.

## Luồng hoạt động

```text
MainCamera.ViewportPointToRay(0.5, 0.5)
        │
        ▼
PlayerInteractor — raycast + occlusion
        │
        ├── focus changed ──► InteractableBehaviour ──► outline/highlight
        │                  └─► InteractionCrosshairUI
        │
        └── E / Gamepad North
                     │
                     ▼
               IInteractable
                 ├── DestroyOnInteract
                 └── PickupItem ──► PickupReceiverBehaviour
```

Core interaction không tham chiếu UI, Inventory hoặc package outline. UI và Inventory tham chiếu một chiều tới Interaction.

## File chính

| File | Vai trò |
|---|---|
| `IInteractable.cs` | Contract cho mọi object tương tác |
| `InteractionContext.cs` | Interactor, camera và RaycastHit của lần tương tác |
| `InteractionResult.cs` | Kết quả success/rejected và message tùy chọn |
| `InteractableBehaviour.cs` | Base class quản lý availability và focus feedback |
| `PlayerInteractor.cs` | Raycast, occlusion, focus state và nhận input |
| `InteractionHighlight.cs` | Abstraction cho visual highlight |
| `BehaviourInteractionHighlight.cs` | Adapter bật/tắt component outline từ package khác |
| `RendererMaterialInteractionHighlight.cs` | Fallback thêm material outline vào Renderer |
| `DestroyOnInteract.cs` | Interactable one-shot, dùng cho object test/disposable |
| `PickupItem.cs` | Pickup chỉ destroy khi receiver chấp nhận |
| `PickupReceiverBehaviour.cs` | Boundary để Inventory thật implement |
| `InteractionCrosshairUI.cs` | Dot crosshair phản ứng theo focus event |

## Cấu hình Player

`PlayerCapsule.prefab` đã có `PlayerInteractor` với:

| Setting | Giá trị |
|---|---:|
| Interaction Camera | Auto resolve từ tag `MainCamera` |
| Interaction Distance | `3 m` |
| Visibility Layers | Tất cả trừ `Ignore Raycast` và `Player` |
| Trigger Interaction | `Ignore` |
| Hit Buffer Size | `16` |

`Visibility Layers` phải chứa cả object interactable lẫn geometry chặn tầm nhìn. Nếu mask chỉ chứa `Interactable`, ray có thể nhìn xuyên tường thuộc layer `Default`.

Raycast bỏ qua mọi collider là child của Player. Kết quả gần nhất được chọn thủ công vì `RaycastNonAlloc` không bảo đảm thứ tự hit.

## Input

Action `Interact` đã được thêm vào asset Player đang sử dụng:

| Device | Binding |
|---|---|
| Keyboard | `E` |
| Gamepad | `buttonNorth` |

Action dùng `Press`; callback chỉ xử lý khi `InputValue.isPressed`. `PlayerInput` giữ behavior `Send Messages`, vì vậy `PlayerInteractor` phải nằm cùng GameObject với `PlayerInput`.

## Tạo object destroy khi tương tác

Prefab test dùng được ngay:

`Assets/_Project/Prefabs/Interactables/InteractableItem_Base.prefab`

1. Tạo GameObject có mesh và Collider.
2. Đặt layer `Interactable`, tag `Item` nếu cần phân loại.
3. Add `DestroyOnInteract`.
4. Add một highlight component.
5. Kéo highlight component vào `Focus Feedback Sources` của `DestroyOnInteract`.
6. Có thể nối SFX/VFX vào event `Before Destroyed`.

`DestroyOnInteract` disable toàn bộ collider ngay khi thành công để ngăn double interaction, sau đó destroy theo delay.

Không dùng component này cho item Inventory thật. Dùng `PickupItem` để tránh làm mất item khi túi đầy.

## Tạo pickup Inventory

Trên world item:

1. Add `PickupItem`.
2. Điền `Item Id` và `Quantity`.
3. Gán highlight qua `Focus Feedback Sources`.
4. Nối `Pickup Accepted`/`Pickup Rejected` nếu cần audio hoặc UI.

Inventory Player phải có component kế thừa:

```csharp
public sealed class PlayerInventory : PickupReceiverBehaviour
{
    public override bool TryReceive(PickupRequest request)
    {
        return TryAdd(request.ItemId, request.Quantity);
    }
}
```

`PickupItem` chỉ disable collider và destroy world object khi `TryReceive` trả `true`. Khi receiver không tồn tại hoặc trả `false`, item vẫn còn nguyên trong scene.

## Outline/highlight

### Fallback có sẵn

Add `RendererMaterialInteractionHighlight` và cấu hình:

- `Target Renderers`: renderer cần highlight.
- `Highlight Material`: `Assets/_Project/Materials/InteractionOutline.mat`.

Component cache chính xác mảng shared material ban đầu, chỉ append material khi focus và restore khi mất focus. Nó không instantiate material mỗi frame.

Material mặc định dùng `Radial Extrusion = 1` và width `0.003` để outline bám liền quanh cube/hard-surface mesh. Nếu model hữu cơ bị phồng không tự nhiên, giảm `Radial Extrusion` dần về `0`; nếu pivot mesh không nằm giữa model, chỉnh `Mesh Local Center` theo local-space center của mesh.

Fallback inverted-hull phù hợp prop đơn giản. Với mesh nhiều submesh, skinned mesh phức tạp hoặc yêu cầu xuyên vật cản, nên dùng outline package/render feature chuyên dụng.

### Adapter cho package ngoài

Add `BehaviourInteractionHighlight`, kéo các component outline của package vào `Outline Behaviours`, sau đó kéo adapter vào `Focus Feedback Sources` của interactable.

Core interaction không cần biết `Outlinable`, shader hay render feature cụ thể.

## Crosshair

Prefab có sẵn:

`Assets/_Project/Prefabs/System/InteractionCrosshair.prefab`

Setup:

1. Kéo prefab làm child của một `Canvas` Screen Space Overlay.
2. Gán `Interactor` tới `PlayerCapsule/PlayerInteractor` của scene.
3. Crosshair đã anchor ở tâm, size `6×6` và tắt `Raycast Target`.

Trạng thái mặc định:

| State | Feedback |
|---|---|
| Không có target | Alpha thấp, scale 1 |
| Target khả dụng | Trắng, scale 1.35 |
| Target không khả dụng | Xám |
| Interaction thành công | Pulse ngắn |

Animation dùng `Time.unscaledDeltaTime`, vẫn cập nhật khi game pause.

## Tạo loại interactable mới

Kế thừa `InteractableBehaviour` và chỉ cài gameplay action:

```csharp
public sealed class DoorInteractable : InteractableBehaviour
{
    [SerializeField] private Door door;

    public override bool CanInteract(InteractionContext context)
    {
        return door != null && !door.IsLocked && base.CanInteract(context);
    }

    protected override InteractionResult PerformInteraction(InteractionContext context)
    {
        door.Toggle();
        return InteractionResult.Success();
    }
}
```

Không đặt raycast, input hoặc crosshair logic trong từng object.

## Quy ước layer/tag

- Layer `Player`: index 8.
- Layer `Interactable`: index 9.
- Tag `Item`: chỉ dành cho object item nếu hệ thống khác cần phân loại.
- Interaction dựa vào component `InteractableBehaviour`, không dựa vào tag.
- World geometry có khả năng che item phải nằm trong `Visibility Layers`.

## Test

EditMode tests nằm tại:

`Assets/_Project/Tests/EditMode/PlayerInteractorTests.cs`

Các case tự động hiện có:

- Focus object nhìn thấy ở tâm viewport.
- Không focus object sau geometry chặn.
- `TryInteract` gọi đúng target một lần.

Checklist play test:

- Nhìn item → outline và crosshair bật.
- Nhìn lệch/ra khỏi 3 m → feedback tắt.
- Item sau tường không được focus.
- `E` và gamepad North gọi đúng một lần.
- Hai collider cùng một item không làm focus flicker.
- Inventory reject → item không biến mất.
- Object bị disable/destroy trong lúc focus không để lại outline.
