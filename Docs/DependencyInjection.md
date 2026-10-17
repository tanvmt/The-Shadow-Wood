# Dependency Injection

Tài liệu quy ước DI của **The Shadow Wood**: VContainer làm container, MessagePipe làm kênh sự kiện giữa các module, UniTask cho luồng bất đồng bộ.

## Cấu trúc scope

```text
RootLifetimeScope              (prefab, tạo qua VContainerSettings, DontDestroyOnLoad)
│  MessagePipe, ISceneLoader, ISceneTransition, IScreenFader, IInputService, IGameSettings
│
├── BootstrapLifetimeScope     (scene Bootstrap)  BootstrapFlow
├── MenuLifetimeScope          (scene Menu)       UIScreenService, main menu, settings
└── GameplayLifetimeScope      (scene Gameplay)   service, entry point và view của gameplay
```

Tại một thời điểm chỉ có một scope scene tồn tại, vì mọi scene load ở chế độ Single. Luồng giữa các scene: [SceneFlow.md](SceneFlow.md).

| Scope | Vị trí | Đăng ký gì |
|---|---|---|
| `RootLifetimeScope` | `Assets/_Project/Prefabs/System/RootLifetimeScope.prefab` | MessagePipe, `ISceneLoader`/`ISceneBackend`, `ISceneTransition`, `IScreenFader`, `IInputService`, `IGameSettings`, và service sống suốt game khác (audio…) |
| `BootstrapLifetimeScope` | GameObject trong `Bootstrap.unity` | `BootstrapFlow` |
| `MenuLifetimeScope` | GameObject trong `Menu.unity` | `UIScreenService`, view và presenter của main menu và settings |
| `GameplayLifetimeScope` | GameObject trong `Gameplay.unity` | Service, entry point và component chỉ tồn tại trong scene gameplay |

`Assets/_Project/Settings/VContainerSettings.asset` trỏ tới prefab Root và nằm trong **Player Settings → Preloaded Assets**. Vì vậy Root luôn được tạo trước, kể cả khi bấm Play trực tiếp ở một scene bất kỳ.

Scope trong scene **để trống field Parent**. Khi không có parent, VContainer tự lấy Root từ `VContainerSettings`.

## Chọn cách đăng ký

| Trường hợp | API | Ví dụ |
|---|---|---|
| Service C# thuần, có interface | `builder.Register<Impl>(Lifetime.Singleton).As<IService>()` | Flag, inventory, player control |
| Class cần chạy theo vòng đời Unity | `builder.RegisterEntryPoint<T>()` | `IStartable`, `ITickable`, `ILateTickable`, `IAsyncStartable`, `IDisposable` |
| MonoBehaviour có sẵn trong scene, đúng một instance và scene bắt buộc phải có | `builder.RegisterComponentInHierarchy<T>()` | `PlayerView`, `CameraRig`, prompt, crosshair |
| Nhiều MonoBehaviour do level designer đặt, số lượng không cố định | Đặt dưới một root, thêm root vào **Auto Inject Game Objects** của scope | Interactables (TV, cửa, pickup) |
| Asset cấu hình | `[SerializeField]` trên scope + `builder.RegisterInstance(asset)` | ScriptableObject config |
| Prefab spawn lúc runtime | `IObjectResolver.Instantiate(prefab)` | Object cần được inject |

Nguyên tắc:

- Logic đặt trong class C# thuần, nhận dependency qua constructor. MonoBehaviour chỉ giữ reference và nhận callback Unity.
- MonoBehaviour được inject qua method `[Inject] public void Construct(...)`. Không dùng field injection. `Awake`/`OnEnable` có thể chạy trước `Construct`, nên chỉ dùng dependency từ `Start` trở đi hoặc trong callback của event.
- Không gọi `FindObjectOfType`, `GameObject.Find` hoặc singleton tĩnh để lấy service.
- Mỗi service expose qua interface nằm ở assembly contract (thường là `Core`), implementation ở assembly của feature.

## Lifetime

| Lifetime | Ý nghĩa ở project này |
|---|---|
| `Singleton` | Một instance cho scope đăng ký nó. Đăng ký ở Root thì sống suốt game; ở Gameplay thì sống theo scene. |
| `Scoped` | Một instance cho mỗi scope con. Chỉ dùng khi thật sự cần scope lồng nhau. |
| `Transient` | Tạo mới mỗi lần resolve. |

Service đăng ký ở Gameplay **không được** inject vào service ở Root. Chiều phụ thuộc luôn từ scope con lên scope cha.

Service giữ vé (handle `IDisposable`) trên một thứ ở Root, như vé context của `IInputService` hay vé `IGamePause`, thì đăng ký ở **scope scene** và trả vé trong `Dispose()`. Scene unload thì scope tự dispose, không để lại vé mồ côi làm kẹt scene sau. Ví dụ: `UIScreenService`, `GamePause`. Tương tự, class ở scene đăng ký event của service Root (ví dụ `IGameSettings.Changed`) phải huỷ đăng ký trong `Dispose()`.

## MessagePipe

Root gọi `RegisterMessagePipe()` và đặt provider cho `GlobalMessagePipe`.

Message là `readonly struct`, đặt tên kết thúc bằng `Message`, và nằm ở assembly contract để cả bên publish và bên subscribe cùng thấy được:

```csharp
public readonly struct ItemAddedMessage
{
    public string ItemId { get; }

    public ItemAddedMessage(string itemId)
    {
        ItemId = itemId;
    }
}
```

Publish/subscribe qua injection:

```csharp
public sealed class InventoryService : IInventoryService
{
    private readonly IPublisher<ItemAddedMessage> _itemAdded;

    public InventoryService(IPublisher<ItemAddedMessage> itemAdded)
    {
        _itemAdded = itemAdded;
    }
}

public sealed class InventoryPresenter : IStartable, IDisposable
{
    private readonly ISubscriber<ItemAddedMessage> _itemAdded;
    private IDisposable _subscription;

    public InventoryPresenter(ISubscriber<ItemAddedMessage> itemAdded)
    {
        _itemAdded = itemAdded;
    }

    public void Start()
    {
        _subscription = _itemAdded.Subscribe(OnItemAdded);
    }

    public void Dispose()
    {
        _subscription?.Dispose();
    }

    private void OnItemAdded(ItemAddedMessage message) { }
}
```

Với nhiều subscription, gom lại bằng `DisposableBag.CreateBuilder()` rồi dispose một lần.

### Message chỉ sống trong scene

Mặc định, broker do Root tạo sống suốt game. Nếu message chỉ có ý nghĩa trong scene, đăng ký broker ở scope của scene bằng options của Root:

```csharp
protected override void Configure(IContainerBuilder builder)
{
    var options = Parent.Container.Resolve<MessagePipeOptions>();
    builder.RegisterMessageBroker<DoorOpenedMessage>(options);
}
```

Cách này đã có test tại `LifetimeScopeTests.SceneScope_CanRegisterSceneScopedBroker_WithRootOptions`.

### GlobalMessagePipe

`GlobalMessagePipe.GetPublisher<T>()` chỉ dùng cho code không được container tạo và không inject được. Mọi trường hợp khác dùng `IPublisher<T>`/`ISubscriber<T>` được inject.

## Assembly

```text
Core ◄── Interaction ◄── Inventory ◄── Puzzles
  ▲            ▲             ▲            ▲
  └── Player, Environment, Audio, Enemy, SaveSystem, UI (tham chiếu một chiều)

Bootstrap ──► tất cả assembly feature   (composition root)
```

- Chỉ `TheShadowWood.Bootstrap` tham chiếu tất cả assembly feature và `MessagePipe.VContainer`. Assembly này có `autoReferenced: false`, nên không assembly nào được tham chiếu ngược lại nó.
- Assembly feature tham chiếu `VContainer`, `MessagePipe`, `UniTask` và assembly contract mà nó cần. Không tham chiếu `MessagePipe.VContainer`.
- Hai feature cần trao đổi với nhau thì dùng interface ở `Core` hoặc message, không tham chiếu chéo.

## Thêm một feature mới

1. Đặt interface và message vào `Core`. Đặt implementation vào assembly của feature.
2. Đăng ký trong scope phù hợp: `RootLifetimeScope` nếu sống suốt game, `GameplayLifetimeScope` nếu theo scene. Đặt vào đúng nhóm comment theo module.
3. Nếu có subscription, event hoặc resource thì implement `IDisposable`. VContainer dispose khi scope bị huỷ.
4. Viết test EditMode cho class C# thuần. Với cấu hình scope, dùng cách trong `LifetimeScopeTests`: build container từ `Configure` mà không cần Play Mode.

## Kiểm tra

- **EditMode:** Test Runner → `TheShadowWood.Bootstrap.Tests`.
- **Play Mode:** Hierarchy có đúng một `RootLifetimeScope` dưới `DontDestroyOnLoad`.
- **Diagnostics:** bật **Enable Diagnostics** trong `VContainerSettings`, mở **Window → VContainer Diagnostics** để xem cây scope và các registration. Tắt lại trước khi commit.

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| `VContainerException: No such registration of type ...` | Chưa đăng ký, đăng ký ở scope con nhưng resolve từ scope cha, hoặc sai interface trong `.As<>()`. |
| `RegisterMessagePipe` / `GlobalMessagePipe` không tồn tại | Thiếu `using MessagePipe;` hoặc assembly thiếu reference `MessagePipe.VContainer`. |
| Hai `RootLifetimeScope` khi Play | Có thêm instance đặt tay trong scene. Chỉ tạo Root qua `VContainerSettings`. |
| Scene scope không thấy service của Root | Field Parent bị gán nhầm, hoặc `VContainerSettings` không nằm trong Preloaded Assets. |
| Callback vẫn chạy sau khi đổi scene | Subscription chưa được dispose. Class chưa implement `IDisposable` hoặc chưa được đăng ký qua container. |
| MonoBehaviour trong scene không chạy, `Construct` không được gọi, không có lỗi | Object không được đăng ký và không nằm dưới root nào trong **Auto Inject Game Objects**. |
| `Construct` bị gọi hai lần, event bị đăng ký đôi | Object vừa đăng ký bằng `RegisterComponentInHierarchy` vừa nằm dưới root auto-inject. Chỉ chọn một cách. |
