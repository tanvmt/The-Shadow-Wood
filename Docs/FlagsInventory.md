# Flags và Inventory

Tài liệu hai service ghi nhớ tiến trình chơi của **The Shadow Wood**: flag (chuyện gì đã xảy ra) và inventory (người chơi đang cầm gì). Door, readable, sequence, puzzle và journal đều đọc từ hai service này. Kèm theo là pickup item và đèn pin, item đầu tiên được dùng.

## Tổng quan

```text
GameplayLifetimeScope (state sống trong scene, vào lại Gameplay là game mới)
├─ broker FlagChangedMessage, ItemAddedMessage, ItemRemovedMessage   (options của Root)
├─ FlagService       (IFlagService)       HashSet<FlagId>
├─ InventoryService  (IInventoryService)  danh sách ItemStack theo thứ tự nhặt
├─ ItemPickupNotifier                     ItemAddedMessage ──► ISubtitleService "Picked up: …"
├─ FlashlightView    (IFlashlightView)    Light dưới CameraRoot + item cần có
└─ FlashlightController                   FlashlightPressed ──► bật/tắt nếu đã có item

Pickup trong scene (dưới root Interactables, được auto-inject)
  PickupItem ──Construct──► IInventoryService
     Interact ──► TryAdd ──► thành công: tắt collider, Destroy
                         └─► thất bại: object ở lại
```

Flag và item đều là **ScriptableObject**, so sánh theo reference của asset. Đổi tên hay chuyển file không làm hỏng liên kết, và Inspector chỉ cho chọn asset có thật nên không có lỗi gõ sai tên như với string.

## File chính

| File | Assembly | Vai trò |
|---|---|---|
| `Core/Flags/FlagId.cs` | Core | Asset định danh một flag, có ô `Note` để ghi khi nào flag được bật |
| `Core/Flags/IFlagService.cs`, `FlagService.cs` | Core | `Has`, `Set`, `Clear`. Chỉ publish khi giá trị thật sự đổi |
| `Core/Flags/FlagChangedMessage.cs` | Core | `(FlagId Flag, bool Value)` |
| `Core/Items/ItemDefinition.cs` | Core | Asset item: `Display Name`, `Description`, `Icon`, `Max Count` |
| `Core/Items/ItemStack.cs` | Core | `(ItemDefinition Item, int Count)` |
| `Core/Items/IInventoryService.cs` | Core | `Items`, `Count`, `Has`, `TryAdd`, `TryRemove` |
| `Core/Items/ItemAddedMessage.cs`, `ItemRemovedMessage.cs` | Core | `(Item, Count, Total)`: `Count` là số vừa thêm/bỏ, `Total` là số còn lại |
| `Inventory/InventoryService.cs` | Inventory | Cài `IInventoryService` |
| `Inventory/PickupItem.cs` | Inventory | Interactable nhặt item vào inventory |
| `UI/Items/ItemPickupNotifier.cs` | UI | Báo nhặt đồ bằng subtitle |
| `Player/Flashlight/IFlashlightView.cs`, `FlashlightView.cs`, `FlashlightController.cs` | Player | Đèn pin |

Asset:

| Asset | Nội dung |
|---|---|
| `Data/Flags/` | `Digit1Revealed` … `Digit4Revealed`. Flag khác tạo ở branch dùng tới nó |
| `Data/Items/` | `Flashlight`, `Candle`, `Lighter`, `ShrineKey`, `ToyPieceA`, `ToyPieceB` (đều `Max Count` 1) |
| `Prefabs/Interactables/PickupItem_Base.prefab` | Cube nhỏ có highlight, `PickupItem` chưa gán item, prompt `Pick up` |

## Đăng ký trong scope

```csharp
// GameplayLifetimeScope.Configure
MessagePipeOptions options = Parent.Container.Resolve<MessagePipeOptions>();
builder.RegisterMessageBroker<FlagChangedMessage>(options);
builder.Register<FlagService>(Lifetime.Singleton).As<IFlagService>();
builder.RegisterMessageBroker<ItemAddedMessage>(options);
builder.RegisterMessageBroker<ItemRemovedMessage>(options);
builder.Register<InventoryService>(Lifetime.Singleton).As<IInventoryService>();

builder.RegisterComponentInHierarchy<FlashlightView>().As<IFlashlightView>();
builder.RegisterEntryPoint<FlashlightController>();
builder.RegisterEntryPoint<ItemPickupNotifier>();
```

Broker đăng ký ở scope Gameplay theo quy ước "message chỉ sống trong scene" ([DependencyInjection.md](DependencyInjection.md#message-chỉ-sống-trong-scene)): unload scene là subscriber cũ mất theo, không rò sang lượt chơi sau.

## Flag

Tạo flag: **Create → The Shadow Wood → Flags → Flag**, đặt trong `Data/Flags/`, ghi vào `Note` lúc nào flag được bật.

```csharp
public sealed class ParentsNote
{
    private readonly IFlagService _flags;
    private readonly FlagId _digit2Revealed;

    public void OnRead()
    {
        _flags.Set(_digit2Revealed);   // gọi lại lần nữa không publish lại
    }
}
```

- Flag chỉ là bool. Chuyện cần đếm (đủ 2 mảnh đồ chơi) thì dùng số lượng item.
- `Set`/`Clear` với flag `null` ném `ArgumentNullException`: field flag chưa gán là lỗi setup, báo ngay thay vì lặng lẽ bỏ qua beat. `Has(null)` trả `false`.
- Muốn phản ứng khi flag đổi thì subscribe `ISubscriber<FlagChangedMessage>` và lọc theo `message.Flag`. Subscription huỷ trong `Dispose()`.

## Inventory

Tạo item: **Create → The Shadow Wood → Items → Item**, đặt trong `Data/Items/`.

| Field | Ý nghĩa |
|---|---|
| `Display Name` | Tên hiện trên UI. Để trống thì lấy tên asset |
| `Description` | Mô tả, dùng cho màn xem đồ sau này |
| `Icon` | Sprite, chưa có UI nào dùng |
| `Max Count` | Số tối đa mang được, tối thiểu 1. Item cốt truyện để 1 |

```csharp
if (_inventory.Has(_shrineKey))
{
    door.Unlock();
}

if (_inventory.Has(_candle) && _inventory.Has(_lighter))
{
    _inventory.TryRemove(_candle);   // đốt mất đèn cầy
}
```

- `TryAdd` trả `false` và không đổi gì khi vượt `Max Count`. `TryRemove` trả `false` khi không đủ số lượng.
- Item `null` hoặc `count < 1` ném exception, cùng quy tắc với flag.
- `Items` giữ thứ tự nhặt. Bỏ hết một item thì stack của nó biến mất khỏi danh sách.
- Chưa có màn hình xem túi đồ. Khi cần, làm một `UIScreen` mới ([UISystem.md](UISystem.md#thêm-một-màn-hình-mới)) đọc `Items` và nghe `ItemAdded/RemovedMessage`.

## Đặt pickup trong scene

1. Kéo `PickupItem_Base.prefab` vào **dưới root `Interactables`** (hoặc root khác trong Auto Inject Game Objects của `GameplayLifetimeScope`).
2. Gán `Item` (asset trong `Data/Items/`). Đổi `Quantity` nếu nhặt nhiều cái một lúc.
3. Thay mesh/model và highlight theo đồ thật. Giữ layer `Interactable` và có Collider.
4. Nối SFX vào `Pickup Accepted` / `Pickup Rejected` nếu cần.

Hành vi:

| Tình huống | Kết quả |
|---|---|
| Còn chỗ | Prompt `[E] Pick up`. Nhấn: item vào inventory, collider tắt ngay, object bị destroy theo `Destroy Delay`, subtitle `Picked up: Candle` 2.5 s |
| Đã đủ `Max Count` | `CanInteract == false`, prompt hiện `Blocked Reason` (`Can't carry more`), object ở lại |
| Chưa gán `Item` | Bị chặn như trên |
| Đặt ngoài root auto-inject | Không được inject, bị chặn như trên |

Subtitle nhặt đồ dùng chung dòng subtitle với lời thoại, dòng mới thay dòng cũ.

## Đèn pin

`Player.prefab` có object `Flashlight` làm con của `CameraRoot` (đèn xoay theo hướng nhìn): spot light 55°, range 18, intensity 6, soft shadow, mặc định tắt. `FlashlightView` gán `Beam` = light đó và `Required Item` = `Data/Items/Flashlight`.

| Phím | Bàn phím | Gamepad |
|---|---|---|
| Bật/tắt | `F` | West button (X / □) |

- Chưa có item `Flashlight` trong inventory thì bấm không có tác dụng.
- Action `Flashlight` chỉ bật ở context `Gameplay`: pause, xem TV, puzzle, đọc note đều không bật/tắt được.
- Pin không hết, không chập chờn. Chập chờn sẽ do `feat/audio-tension` hoặc scripted sequence điều khiển.
- Chỉnh độ sáng, góc, màu trên Light trong `Player.prefab`. Scene `Gameplay` hiện không có light nào khác nên đèn trông rất sáng.

## Kiểm thử

**EditMode:**

| Assembly | File | Nội dung |
|---|---|---|
| `TheShadowWood.Core.Tests` | `Core/FlagServiceTests.cs` | Set/Clear/Has, Set lặp không publish lại, Clear flag chưa set không publish, null |
| `TheShadowWood.Core.Tests` | `Core/InputContextRulesTests.cs` | `Flashlight` chỉ bật ở `Gameplay` |
| `TheShadowWood.Inventory.Tests` | `Inventory/InventoryServiceTests.cs` | Add/remove/count, vượt `Max Count`, remove thiếu, message đúng count/total, thứ tự `Items`, tham số sai |
| `TheShadowWood.Inventory.Tests` | `Inventory/PickupItemTests.cs` | Còn chỗ thì nhặt được; chưa gán item, inventory đầy, chưa inject thì bị chặn và object ở lại |
| `TheShadowWood.UI.Tests` | `UI/ItemPickupNotifierTests.cs` | Câu thông báo một cái và nhiều cái |
| `TheShadowWood.Player.Tests` | `Player/FlashlightControllerTests.cs` | Chưa có item thì không bật, có item thì toggle, không bấm thì giữ nguyên |

`PickupItemTests` chỉ test nhánh bị chặn: nhánh thành công gọi `Destroy`, Edit Mode không cho phép. Nhánh đó kiểm bằng play test.

**Play Mode** (scene `Gameplay`, cạnh cube test có `Pickup_Candle`, `Pickup_Lighter`, `Pickup_Flashlight`):

| Test | Kết quả mong đợi |
|---|---|
| Bấm `F` khi chưa nhặt đèn pin | Không có gì |
| Nhặt Candle, Lighter, Flashlight | Mỗi lần: subtitle `Picked up: …`, object biến mất, prompt và highlight tắt |
| `F` sau khi nhặt đèn pin | Bật/tắt, đèn theo hướng nhìn |
| Esc pause rồi `F` | Đèn không đổi |
| Xem TV rồi `F` | Đèn không đổi |
| Về Menu rồi vào lại Gameplay | Inventory rỗng, đèn tắt, pickup có lại đủ |

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| Pickup hiện `Can't carry more` ngay từ đầu | Chưa gán `Item`, hoặc object nằm ngoài root auto-inject nên `Construct` không được gọi |
| Nhặt được nhưng không thấy subtitle | `SubtitleView` trong scene bị tắt (GameObject inactive). View tự ẩn label, GameObject phải để active |
| `ArgumentNullException: Flag is not assigned` / `Item is not assigned` | Field flag/item trên component hoặc action chưa gán asset |
| `VContainerException` về `FlashlightView` | `Player` trong scene không phải bản prefab mới, thiếu object `Flashlight` |
| `F` vừa bật đèn vừa focus camera | `CameraSandboxDebug` được thêm lại vào scene, nó cũng đọc phím `F`. Bỏ component này khi test đèn pin |
| Message không tới subscriber | Subscribe trước khi scope build xong hoặc ở scope khác. Subscriber phải resolve từ scope Gameplay (hoặc scope con của nó) |
