# Scene Flow

Tài liệu luồng scene của **The Shadow Wood**: thứ tự scene, cách load scene qua `ISceneLoader`, cách thêm scene mới và công cụ chuyển scene nhanh trong Editor.

## Luồng tổng quát

```text
Bootstrap ──► Menu ──(Bắt đầu)──► Gameplay
    │           │
    │           └─(Thoát)──► thoát Play Mode / Application.Quit
    │
    └─ RootLifetimeScope đã được tạo trước qua VContainerSettings (DontDestroyOnLoad)
```

| Scene | Build index | Scope | Vai trò |
|---|---:|---|---|
| `Assets/_Project/Scenes/Bootstrap.unity` | 0 | `BootstrapLifetimeScope` | Scene khởi động. `BootstrapFlow` load `Menu` ngay khi scope build xong |
| `Assets/_Project/Scenes/Menu.unity` | 1 | `MenuLifetimeScope` | Menu chính: nút Bắt đầu load `Gameplay`, nút Thoát |
| `Assets/_Project/Scenes/Gameplay.unity` | 2 | `GameplayLifetimeScope` | Scene chơi game. Scope đang rỗng, các feature sau sẽ đăng ký vào đây |

Mọi scene đều load ở chế độ **Single**: scene cũ bị unload, scope và các entry point của scene cũ được dispose. Chỉ `RootLifetimeScope` sống qua các scene.

Bấm Play thẳng ở `Menu` hoặc `Gameplay` vẫn chạy được, vì Root được tạo qua `VContainerSettings` chứ không phụ thuộc scene `Bootstrap`. Bản build thật luôn bắt đầu từ scene ở index 0, nên `Bootstrap` phải đứng đầu Build Settings.

## File chính

| File | Assembly | Vai trò |
|---|---|---|
| `Core/Scenes/SceneId.cs` | Core | Enum định danh scene: `Bootstrap`, `Menu`, `Gameplay` |
| `Core/Scenes/SceneCatalog.cs` | Core | Map `SceneId` → tên scene trong Build Settings bằng `switch` |
| `Core/Scenes/ISceneLoader.cs` | Core | Contract load scene cho mọi feature |
| `Core/Scenes/SceneLoader.cs` | Core | Chặn load trùng, kiểm tra Build Settings, reset trạng thái khi lỗi/cancel |
| `Core/Scenes/ISceneBackend.cs` | Core | Lớp bọc quanh Unity `SceneManager`, để thay bằng bản giả khi test |
| `Core/Scenes/UnitySceneBackend.cs` | Core | Implementation thật: `Application.CanStreamedLevelBeLoaded` + `SceneManager.LoadSceneAsync` |
| `Bootstrap/BootstrapFlow.cs` | Bootstrap | `IAsyncStartable` của scene Bootstrap, load `Menu` |
| `Bootstrap/BootstrapLifetimeScope.cs` | Bootstrap | Đăng ký `BootstrapFlow` |
| `Bootstrap/MenuLifetimeScope.cs` | Bootstrap | Đăng ký `MainMenuView` + `MainMenuPresenter` |
| `Bootstrap/GameplayLifetimeScope.cs` | Bootstrap | Scope của scene Gameplay |
| `UI/Menu/MainMenuView.cs` | UI | MonoBehaviour chỉ giữ reference hai nút |
| `UI/Menu/MainMenuPresenter.cs` | UI | Xử lý nút Bắt đầu/Thoát |
| `Editor/SceneSwitcherToolbar.cs` | Editor | Dropdown chuyển scene trên main toolbar |

`UnitySceneBackend` và `SceneLoader` được đăng ký Singleton ở `RootLifetimeScope`.

## Load scene từ code

Inject `ISceneLoader` rồi gọi `LoadAsync`:

```csharp
public sealed class SomePresenter
{
    private readonly ISceneLoader _sceneLoader;

    public SomePresenter(ISceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
    }

    private async UniTaskVoid GoToMenuAsync()
    {
        bool loaded = await _sceneLoader.LoadAsync(SceneId.Menu);
        if (!loaded)
        {
            // Không load: đang có lượt load khác, hoặc scene chưa bật trong Build Settings.
        }
    }
}
```

Hành vi của `LoadAsync`:

| Tình huống | Kết quả |
|---|---|
| Load thành công | Trả `true` |
| Đang có lượt load khác chạy | Trả `false` ngay, log warning, không load |
| Scene không có hoặc bị tắt trong Build Settings | Trả `false`, log error, không gọi `SceneManager` |
| `SceneManager` không bắt đầu được | Throw `InvalidOperationException` |
| `CancellationToken` bị cancel giữa chừng | Throw `OperationCanceledException` |

Trong mọi trường hợp, `IsLoading` luôn trở về `false` sau khi `LoadAsync` kết thúc.

Lưu ý khi dùng:

- **Khoá UI trong lúc load**, như `MainMenuPresenter` làm với `SetInteractable(false)`. Nếu `LoadAsync` trả `false` thì mở khoá lại.
- **Sau khi load Single thành công, object của scene cũ đã bị huỷ.** Code chạy tiếp sau `await` không được đụng vào view của scene cũ khi kết quả là `true`.
- `progress` nhận giá trị 0..1 từ `AsyncOperation`, dùng được cho loading bar sau này.
- Không gọi `SceneManager.LoadScene` trực tiếp trong code gameplay. Mọi lượt chuyển scene đi qua `ISceneLoader` để có chặn load trùng và kiểm tra Build Settings ở một chỗ.

## Thêm scene mới

1. Tạo scene trong `Assets/_Project/Scenes/` (scene thử nghiệm đặt trong `Assets/_Project/Scenes/Dev/`).
2. Tạo GameObject có một `LifetimeScope` riêng cho scene. **Để trống field Parent** để scope tự lấy Root làm cha.
3. Nếu scene cần được load bằng code:
   - Thêm giá trị vào `SceneId`.
   - Thêm một `case` tương ứng vào `SceneCatalog.GetSceneName`. Thiếu `case` thì `GetSceneName` throw `ArgumentOutOfRangeException`.
   - Thêm scene vào **File → Build Profiles → Scene List** và bật tick.
4. Scene chỉ dùng để test trong Editor thì không cần bước 3.
5. Chạy lại EditMode test và play-test từ `Bootstrap`.

Tên scene trong `SceneCatalog` phải trùng tên file `.unity` (không có đuôi). Đổi tên file scene thì sửa `SceneCatalog` trong cùng commit.

## Scene Switcher (Editor)

`SceneSwitcherToolbar` thêm một dropdown vào giữa main toolbar của Unity 6.3, hiện tên scene đang mở.

- Mục `Build/…`: scene trong Build Settings theo thứ tự build. Scene bị tắt nằm trong `Build (disabled)/…`.
- Bên dưới: mọi scene khác trong `Assets/_Project/Scenes` và `Assets/Scenes`. Thư mục con (ví dụ `Dev/`) thành menu con.
- Scene đang mở có dấu tick.
- Scene hiện tại có thay đổi chưa lưu thì Unity hỏi Save/Don't Save/Cancel. Chọn Cancel thì không chuyển.
- Đang Play thì các mục bị tắt.

Không thấy dropdown: chuột phải lên main toolbar và bật `The Shadow Wood/Scene Switcher`.

Muốn tìm scene ở thư mục khác, sửa mảng `SearchFolders` trong `SceneSwitcherToolbar.cs`.

## Kiểm thử

**EditMode:** Test Runner → `TheShadowWood.Bootstrap.Tests` → `SceneLoaderTests`. Test dùng `FakeSceneBackend` nên không load scene thật và không phụ thuộc Build Settings:

- Load thành công trả `true` và gọi đúng tên scene.
- Scene không có trong Build Settings trả `false`, không gọi backend.
- Lượt load thứ hai trong lúc lượt đầu đang chạy bị bỏ qua.
- `IsLoading` về `false` khi backend throw và khi bị cancel.

**Play Mode:**

| Test | Kết quả mong đợi |
|---|---|
| Play từ `Bootstrap` | Chuyển sang `Menu` ngay. Có đúng một `RootLifetimeScope` dưới `DontDestroyOnLoad` |
| Bấm Bắt đầu | Hai nút bị khoá, chuyển sang `Gameplay` |
| Bấm Bắt đầu liên tục nhiều lần | Chỉ load một lần, không có warning load trùng từ menu |
| Bấm Thoát | Thoát Play Mode |
| Play thẳng từ `Menu` hoặc `Gameplay` | Scene chạy được, Root vẫn được tạo |
| Tắt `Gameplay` trong Build Settings rồi bấm Bắt đầu | Console báo scene chưa bật, hai nút mở khoá lại |

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| Play ở `Bootstrap` nhưng không sang `Menu` | `BootstrapLifetimeScope.Configure` thiếu `RegisterEntryPoint<BootstrapFlow>()`, hoặc GameObject trong scene chưa gắn component `Bootstrap Lifetime Scope`. |
| `[SceneLoader] Scene '...' is not enabled in Build Settings` | Scene chưa có hoặc bị bỏ tick trong Scene List, hoặc tên trong `SceneCatalog` không khớp tên file. |
| Nút menu không phản hồi | `MenuLifetimeScope` chưa đăng ký `MainMenuPresenter`, `MainMenuView` chưa gán hai nút, hoặc scene thiếu `EventSystem` (dùng `InputSystemUIInputModule`). |
| `VContainerException` về `MainMenuView` | Scene `Menu` không có GameObject nào gắn `MainMenuView`. |
| Bản build mở thẳng vào Menu hoặc Gameplay | `Bootstrap` không nằm ở index 0 trong Build Settings. |
| `ArgumentOutOfRangeException` từ `SceneCatalog` | Đã thêm giá trị `SceneId` nhưng chưa thêm `case` tương ứng. |
