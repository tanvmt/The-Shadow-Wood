# UI System

Tài liệu nền tảng UI của **The Shadow Wood**: mở/đóng màn hình qua `UIScreenService`, pause menu, settings, màn đen chuyển scene và subtitle. Game kinh dị nên UI giữ tối giản, không dùng package UI manager; mọi màn hình dựa trên VContainer + `InputContextStack`.

## Tổng quan

| Phần | Làm gì | Scope |
|---|---|---|
| `UIScreenService` | Chồng màn hình. Mỗi màn hình mở ra push một context `UI`, chỉ màn trên cùng bấm được, Esc đóng màn trên cùng | Menu, Gameplay (mỗi scene một bản) |
| Pause | Esc ở `Gameplay` mở Pause, game dừng hẳn (`timeScale = 0`, âm thanh dừng). Resume / Settings / Về Menu | Gameplay |
| Settings | Âm lượng Master/Music/SFX, độ nhạy chuột. Áp dụng ngay khi kéo, lưu khi đóng màn hình | Menu, Gameplay |
| Fade + chuyển scene | `ISceneTransition`: fade đen → load → fade sáng | Root |
| Subtitle | Một dòng chữ ở đáy màn hình, tự ẩn sau `duration` | Gameplay |

## Luồng hoạt động

```text
Esc (action Pause, chỉ bật ở Gameplay)
  └─► PausePresenter.Tick ──► UIScreenService.Open(PauseScreen)
        ├─ push context UI          → player đứng yên, con trỏ hiện, Pause tắt, Cancel bật
        ├─ PauseScreen.Show()       → Shown → IGamePause.Pause() → timeScale = 0
        │
        ├─ Settings ──► Open(SettingsScreen)   Pause vẫn hiện nhưng không bấm được
        │                 Esc / Back ──► Close → Pause bấm lại được
        ├─ Esc / Resume ──► Close(PauseScreen) → Hidden → trả vé pause, trả vé context
        └─ Main Menu ──► ISceneTransition.LoadAsync(Menu)
                          fade đen (unscaled time) → load Menu
                          → scope Gameplay huỷ: GamePause, UIScreenService trả hết vé
```

Esc dùng chung cho hai action: `Pause` chỉ bật ở `Gameplay`, `Cancel` bật ở `Focus`/`Puzzle`/`UI`. Mỗi context bật đúng một trong hai, nên Esc thoát TV không mở Pause, và lần nhấn mở Pause không đóng luôn Pause trong cùng frame. `InputContextRulesTests` kiểm tra quy tắc này.

## File chính

| File | Assembly | Vai trò |
|---|---|---|
| `UI/Screens/IUIScreen.cs` | UI | Contract một màn hình: `Show`, `Hide`, `SetInteractive`, `CloseOnCancel` |
| `UI/Screens/UIScreen.cs` | UI | MonoBehaviour base. Ẩn/hiện bằng `SetActive`, khoá/mở bằng `CanvasGroup`, event `Shown`/`Hidden` |
| `UI/Screens/IUIScreenService.cs`, `UIScreenService.cs` | UI | C# thuần, `ITickable`. Chồng màn hình, giữ vé context, Esc đóng màn trên cùng |
| `UI/Pause/PauseScreen.cs`, `PausePresenter.cs` | UI | Màn Pause và logic Esc / Resume / Settings / Về Menu |
| `UI/Settings/SettingsScreen.cs`, `SettingsPresenter.cs` | UI | Bốn slider (Master, Music, SFX, Look) + nút Back, đồng bộ với `IGameSettings` |
| `UI/Menu/MainMenuView.cs`, `MainMenuPresenter.cs` | UI | Main menu là một `UIScreen` không đóng bằng Esc |
| `UI/Fade/ScreenFader.cs` | UI | C# thuần, tự tạo Canvas đen (sort 1000, `DontDestroyOnLoad`) ở lần dùng đầu |
| `UI/Subtitles/SubtitleService.cs`, `SubtitleView.cs`, `ISubtitleView.cs` | UI | Đếm thời gian subtitle / giữ label |
| `Core/UI/IScreenFader.cs`, `ISubtitleService.cs` | Core | Contract cho feature khác dùng |
| `Core/Scenes/ISceneTransition.cs`, `SceneTransition.cs` | Core | Fade → `ISceneLoader` → fade |
| `Core/Pause/IGamePause.cs`, `GamePause.cs` | Core | `Pause()` trả vé, vé cuối trả thì game chạy lại |
| `Core/Settings/IGameSettings.cs`, `GameSettings.cs`, `ISettingsStore.cs`, `PlayerPrefsSettingsStore.cs` | Core | Giá trị settings, giới hạn, lưu PlayerPrefs |
| `Prefabs/UI/PauseScreen.prefab`, `SettingsScreen.prefab`, `SubtitleView.prefab` | – | Prefab dựng sẵn, đã gán mọi reference |

## Đăng ký trong scope

```csharp
// RootLifetimeScope: sống suốt game
builder.Register<ScreenFader>(Lifetime.Singleton).As<IScreenFader>();
builder.Register<SceneTransition>(Lifetime.Singleton).As<ISceneTransition>();
builder.Register<PlayerPrefsSettingsStore>(Lifetime.Singleton).As<ISettingsStore>();
builder.Register<GameSettings>(Lifetime.Singleton).As<IGameSettings>();

// MenuLifetimeScope
builder.RegisterEntryPoint<UIScreenService>();
builder.RegisterComponentInHierarchy<MainMenuView>();
builder.RegisterComponentInHierarchy<SettingsScreen>();
builder.RegisterEntryPoint<MainMenuPresenter>();
builder.RegisterEntryPoint<SettingsPresenter>();

// GameplayLifetimeScope
builder.RegisterEntryPoint<UIScreenService>();
builder.Register<GamePause>(Lifetime.Singleton).As<IGamePause>();
builder.RegisterComponentInHierarchy<PauseScreen>();
builder.RegisterComponentInHierarchy<SettingsScreen>();
builder.RegisterEntryPoint<PausePresenter>();
builder.RegisterEntryPoint<SettingsPresenter>();
builder.RegisterComponentInHierarchy<SubtitleView>().As<ISubtitleView>();
builder.RegisterEntryPoint<SubtitleService>();
builder.RegisterEntryPoint<ItemPickupNotifier>();
```

**Vì sao `UIScreenService` và `GamePause` đăng ký theo scene, không ở Root:** stack context của `InputService` là global (Root), còn màn hình là object của scene. Service ở scene scope bị dispose cùng scene, trả hết vé context và vé pause. Nếu đặt ở Root, về Menu lúc đang mở Pause sẽ để lại vé `UI` mồ côi và vào lại Gameplay bị kẹt chuột, không đi được.

`ScreenFader` thì ngược lại: phải sống qua lúc đổi scene để che màn hình, và nó không push context, nên ở Root.

## Màn hình: `UIScreen` và `UIScreenService`

- **Chỉ mở/đóng qua service** (`Open`, `Close`), không gọi `Show`/`Hide` trực tiếp. Service lo push/trả context và khoá màn bên dưới.
- **Ẩn/hiện bằng `SetActive`.** Màn hình để active hay inactive sẵn trong scene đều được: active thì tự tắt ở `Awake`. `RegisterComponentInHierarchy` tìm được cả object đang tắt.
- **Khoá/mở bằng `CanvasGroup`** (`interactable`, `blocksRaycasts`). CanvasGroup của màn hình đặt `ignoreParentGroups = true`, nên màn hình nằm trong cây con của màn khác vẫn bấm được khi màn cha bị khoá. Tuy vậy không đặt màn hình làm con của màn **có thể bị đóng**: cha tắt thì con biến mất theo.
- **`CloseOnCancel`**: `true` thì Esc/B đóng màn hình. `MainMenuView` override thành `false`. `PauseScreen` trả `false` khi nút Resume đang bị khoá (lúc fade về Menu), để Esc không resume giữa chừng.
- **`First Selected`** chỉ được chọn sẵn khi có tay cầm (`Gamepad.current != null`). Không có tay cầm thì selection bị xoá, tránh nút sáng như đang hover khi dùng chuột.
- **Event `Shown`/`Hidden`** cho presenter biết màn hình mở/đóng, ví dụ Pause lấy vé `IGamePause` khi `Shown`, Settings `Save()` khi `Hidden`.
- Dữ liệu → UI dùng `SetValueWithoutNotify`, UI → dữ liệu đi qua `onValueChanged`, để mở màn hình không ghi ngược vào settings.

### Thêm một màn hình mới

Ví dụ màn đọc note ở `feat/doors-readables`:

1. View kế thừa `UIScreen`, chỉ giữ reference:

   ```csharp
   public sealed class ReadableScreen : UIScreen
   {
       [SerializeField] private TMP_Text body;
       public TMP_Text Body => body;
   }
   ```

2. Presenter C# thuần nhận view + `IUIScreenService`, mở màn bằng `Open`:

   ```csharp
   public sealed class ReadablePresenter
   {
       private readonly ReadableScreen _view;
       private readonly IUIScreenService _screens;

       public ReadablePresenter(ReadableScreen view, IUIScreenService screens)
       {
           _view = view;
           _screens = screens;
       }

       public void Open(string text)
       {
           _view.Body.text = text;
           _screens.Open(_view);   // context UI, chuột hiện, Esc đóng
       }
   }
   ```

3. Đăng ký trong `GameplayLifetimeScope`: `RegisterComponentInHierarchy<ReadableScreen>()` và presenter (`RegisterEntryPoint` nếu cần `IStartable`/`ITickable`/`IDisposable`).
4. Đặt màn hình vào canvas UI của scene, **trước** `PauseScreen` trong Hierarchy để Pause luôn vẽ đè lên.

Không push `InputContext.UI` tay cho màn hình: service đã làm. Push tay chỉ dành cho thứ không phải màn hình (ví dụ `ICameraFocusService` push `Focus`).

## Pause

- Chỉ mở được ở context `Gameplay`. Đang xem TV, puzzle hay đang mở màn hình khác thì Esc là `Cancel`.
- `GamePause.Pause()` đếm vé. Vé đầu: lưu `timeScale` hiện tại, đặt `0`, `AudioListener.pause = true`. Vé cuối trả: khôi phục `timeScale` cũ. Scope huỷ: `Dispose()` luôn cho game chạy lại.
- Về Menu: game giữ nguyên pause trong lúc fade đen, các nút và Esc bị khoá. `timeScale` được khôi phục khi scope Gameplay bị huỷ (`PausePresenter.Dispose` trả vé, `GamePause.Dispose` làm lưới an toàn). Load thất bại thì fade sáng lại, mở khoá nút, game vẫn đang pause.
- Code chạy theo scaled time (vật lý, `PlayerController`, subtitle) tự đứng yên. UI cần chạy khi pause phải dùng `Time.unscaledDeltaTime`.

## Fade và chuyển scene

Chuyển scene người chơi nhìn thấy (menu → game, game → menu, ending) dùng `ISceneTransition`, không gọi thẳng `ISceneLoader`:

```csharp
bool loaded = await _transition.LoadAsync(SceneId.Menu);
if (!loaded && _view != null)
{
    _view.SetButtonsEnabled(true);   // load bị từ chối: màn đã sáng lại, mở khoá UI
}
```

| Tình huống | Kết quả |
|---|---|
| Thành công | Fade đen 0.6 s → load → fade sáng 0.8 s, trả `true` |
| `ISceneLoader` từ chối (đang load, scene chưa bật) | Vẫn fade sáng lại, trả `false` |
| Lỗi hoặc cancel | Xoá màn đen ngay (`SetAlpha(0)`) rồi ném lại exception |
| Gọi khi đang chuyển scene | Trả `false`, log warning |

- `BootstrapFlow` đặt fader đen ngay từ đầu rồi chuyển sang Menu, nên Menu fade sáng dần. Play thẳng `Gameplay` trong Editor thì fader trong suốt.
- Fade chạy theo `Time.unscaledDeltaTime`, mỗi frame tính tối đa 1/30 s: frame giật (ngay sau load scene) làm fade chậm lại chứ không nhảy cóc.
- Khi màn còn đen, fader chặn raycast, không bấm UI phía dưới được.
- Không truyền cancellation token của scope scene hiện tại vào `LoadAsync`: chính lượt load huỷ scope đó và sẽ cắt fade giữa chừng.

## Settings

| Setting | Khoảng | Mặc định | Áp dụng |
|---|---|---|---|
| `MasterVolume` | 0–1 | 1 | `AudioListener.volume`, ngay khi đổi |
| `MusicVolume` | 0–1 | 1 | Nhân vào âm bus `Music`. `AudioService` áp lại cho âm đang phát |
| `SfxVolume` | 0–1 | 1 | Nhân vào âm bus `Sfx`, `Ambience`, `Ui` (bước chân, mưa, radio...) |
| `LookSensitivity` | 0.2–3 | 1 | Hệ số nhân với `FirstPersonLookConfig` (chuột và tay cầm). `PlayerController` nghe event `Changed` |

- Lưu ở PlayerPrefs với key `settings.masterVolume`, `settings.musicVolume`, `settings.sfxVolume`, `settings.lookSensitivity`, chỉ khi gọi `Save()` (đóng màn Settings).
- Bus và cách âm lượng được tính: [AudioSystem.md](AudioSystem.md#bus-và-âm-lượng).
- Prefab `SettingsScreen` xếp hàng từ trên xuống: Master, Music, SFX, Look, rồi nút Back. Thêm slider mới: nhân bản một hàng, thêm field vào `SettingsScreen`, nối trong `SettingsPresenter` bằng `SetupVolumeSlider`.
- Đổi cảm giác mặc định cho mọi người: chỉnh `mouseSensitivity`/`gamepadSensitivity` trong `FirstPersonLookConfig.asset`, giữ hệ số mặc định là 1.
- Đưa về mặc định khi test: **Edit → Clear All PlayerPrefs**.
- Ai đăng ký `IGameSettings.Changed` từ scene scope phải huỷ đăng ký trong `Dispose()`, vì `GameSettings` ở Root sống lâu hơn scene.

## Subtitle

```csharp
_subtitles.Show("Có tiếng khóc trên sân thượng...", 3f);   // tự ẩn sau 3 s
_subtitles.Show("...", 0f);                                // giữ tới khi Hide()
_subtitles.Hide();
```

- Dòng mới thay dòng cũ và reset thời gian. Text rỗng thì ẩn.
- Đếm theo scaled time, nên đứng yên khi pause.
- `ItemPickupNotifier` (`UI/Items`) dùng subtitle để báo `Picked up: …` khi nhặt đồ. Xem [FlagsInventory.md](FlagsInventory.md).
- Action `ShowSubtitle` của scripted sequence (`feat/scripted-sequence`) sẽ gọi service này.
- `SubtitleView` trong scene phải để **active**. View tự ẩn label ở `Awake`; tắt cả GameObject thì `Display` vẫn chạy nhưng không bao giờ thấy chữ.

## Setup scene

**Menu**

- `[MainCanvas]`: Canvas Scaler **Scale With Screen Size 1920×1080**, Match 0.5. `MainMenuView` trên canvas, `First Selected` = Start.
- `SettingsScreen.prefab` đặt sau Panel menu.
- `EventSystem` dùng `InputSystemUIInputModule`.

**Gameplay**

- Một Canvas Screen Space Overlay (Scale With Screen Size 1920×1080) chứa theo thứ tự: `SubtitleView.prefab`, `PauseScreen.prefab`, `SettingsScreen.prefab`. Phần tử sau vẽ đè lên phần tử trước.
- `EventSystem` dùng `InputSystemUIInputModule`.

Thứ tự vẽ: canvas màn hình (sort 0–100) < `ScreenFader` (sort 1000).

Style chung: nút xám đen `0.12`, hover/chọn `0.28`, nhấn `0.4`; chữ xám nhạt `0.85`, font LiberationSans SDF. Font này không có dấu tiếng Việt, nên chữ trên UI đang để tiếng Anh tới khi có font tiếng Việt.

## Kiểm thử

**EditMode:**

| Assembly | File | Nội dung |
|---|---|---|
| `TheShadowWood.UI.Tests` | `UI/UIScreenServiceTests.cs` | Mở/đóng, mở trùng, chồng màn hình, Cancel đóng màn trên cùng, màn không đóng bằng Cancel, `Dispose` trả context |
| `TheShadowWood.UI.Tests` | `UI/SubtitleServiceTests.cs` | Hiện, hết giờ thì ẩn, dòng mới reset giờ, duration 0, text rỗng |
| `TheShadowWood.UI.Tests` | `UI/ItemPickupNotifierTests.cs` | Câu `Picked up: …` một cái và nhiều cái |
| `TheShadowWood.Bootstrap.Tests` | `Bootstrap/SceneTransitionTests.cs` | Thứ tự fade → load → fade, load bị từ chối, lỗi không kẹt màn đen, chặn gọi chồng |
| `TheShadowWood.Core.Tests` | `Core/GamePauseTests.cs` | Dừng time/audio, vé lồng nhau, dispose vé hai lần, `Dispose` luôn resume |
| `TheShadowWood.Core.Tests` | `Core/GameSettingsTests.cs` | Mặc định, đọc + clamp giá trị đã lưu (cả Music/SFX), event `Changed`, chỉ ghi khi `Save()` |
| `TheShadowWood.Core.Tests` | `Core/InputContextRulesTests.cs` | Mỗi context bật đúng một trong `Pause`/`Cancel` |
| `TheShadowWood.Player.Tests` | `Player/FirstPersonLookTests.cs` | `SensitivityScale` nhân cả chuột và tay cầm |

**Play Mode:**

| Test | Kết quả mong đợi |
|---|---|
| Play từ `Bootstrap` | Màn đen rồi Menu sáng dần |
| Menu → Settings → kéo slider → Esc | Đóng Settings, bấm được menu tiếp, giá trị còn sau khi Play lại |
| Menu → Start | Tối dần, vào Gameplay, sáng dần |
| Esc trong Gameplay | Pause mở, vật lý và âm thanh dừng, con trỏ hiện |
| Pause → Settings → Esc → Esc | Về Pause rồi Resume, điều khiển lại bình thường |
| Đổi độ nhạy trong Pause rồi Resume | Xoay chuột thấy khác ngay |
| Kéo slider SFX/Music trong Pause rồi Resume | Mưa, radio (SFX) hoặc nhạc (Music) to nhỏ theo |
| Xem TV rồi Esc | Thoát TV, không mở Pause |
| Pause → Main Menu → Start | Về Menu có fade, vào lại Gameplay không kẹt chuột, không đứng hình |
| Chỉ dùng chuột | Mở Pause không có nút nào sáng sẵn |
| Cắm tay cầm | Resume được chọn sẵn, d-pad chuyển nút, Start mở/đóng Pause, B quay lại |

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| `VContainerException` về `PauseScreen`/`SettingsScreen`/`SubtitleView`/`MainMenuView` | Scene thiếu prefab tương ứng. Các view này đăng ký bằng `RegisterComponentInHierarchy` nên bắt buộc có trong scene |
| Mở màn hình nhưng không bấm được | Scene thiếu `EventSystem`, hoặc `EventSystem` còn dùng `StandaloneInputModule` thay vì `InputSystemUIInputModule` |
| Esc không mở Pause | Đang ở context khác `Gameplay` (TV, màn hình khác đang mở), hoặc `GameInputActions.cs` chưa được sinh lại sau khi thêm action `Pause` |
| Vào Gameplay bị kẹt chuột, không đi được | Có vé context chưa trả. Màn hình phải mở qua `UIScreenService`, class push context tay phải dispose trong `Dispose()` |
| Game đứng hình sau khi về Menu | `GamePause` không được đăng ký ở scope scene, hoặc có class giữ vé `IGamePause` sống ở Root |
| Màn hình bị Pause đè sai thứ tự | Đổi thứ tự sibling trong Canvas: phần tử sau vẽ đè lên phần tử trước |
| Chữ tiếng Việt hiện ô vuông | Font LiberationSans SDF không có dấu. Cần font asset tiếng Việt |
| Gọi `ISubtitleService.Show` mà không thấy chữ | GameObject `SubtitleView` trong scene bị tắt. Bật lại, view tự ẩn label khi không có chữ |
