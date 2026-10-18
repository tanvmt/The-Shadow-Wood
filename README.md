# The Shadow Wood

> Project game kinh dị sinh tồn góc nhìn thứ nhất (FPS Survival Horror), phát triển bằng Unity.

## Mục lục

- [Yêu cầu](#yêu-cầu)
- [Mở và chạy project](#mở-và-chạy-project)
- [Điều khiển](#điều-khiển)
- [Cấu trúc project](#cấu-trúc-project)
- [Các hệ thống hiện có](#các-hệ-thống-hiện-có)
- [Thêm nội dung mới](#thêm-nội-dung-mới)
- [Kiểm thử](#kiểm-thử)
- [Quy ước đóng góp](#quy-ước-đóng-góp)
- [Khắc phục sự cố](#khắc-phục-sự-cố)

## Yêu cầu

| Thành phần | Phiên bản / ghi chú |
| --- | --- |
| Unity Hub | Bản mới nhất |
| Unity Editor | **6000.3.12f1** (Unity 6.3) |
| IDE C# | Visual Studio, Rider hoặc VS Code có C# extension |
| Git + Git LFS | Cần khi clone project vì một số asset được quản lý bằng LFS |

Project sử dụng Universal Render Pipeline (URP) và Unity Input System. Các package chính được khai báo trong `Packages/manifest.json`, gồm Input System, Cinemachine, AI Navigation, Timeline, Test Framework và URP.

## Mở và chạy project

1. Clone repository và tải asset LFS:

   ```bash
   git lfs install
   git clone <repository-url>
   cd The-Shadow-Wood
   git lfs pull
   ```

2. Trong Unity Hub, chọn **Add** → chọn thư mục gốc chứa `Assets`, `Packages` và `ProjectSettings`.
3. Mở project bằng Unity Editor **6000.3.12f1**. Unity sẽ tự khôi phục package và tạo lại `Library` trong lần mở đầu tiên.
4. Chờ Unity import asset và hết lỗi compile trong Console.
5. Mở `Assets/_Project/Scenes/Bootstrap.unity` (hoặc chọn `Build/Bootstrap` trong dropdown Scene Switcher trên toolbar), rồi nhấn **Play**.

Build Settings gồm ba scene theo thứ tự `Bootstrap` → `Menu` → `Gameplay`. `Bootstrap` phải luôn ở index 0. Luồng scene, cách load scene bằng code và cách thêm scene mới: [Docs/SceneFlow.md](Docs/SceneFlow.md).

## Điều khiển

Input dùng action map `Player` tại `Assets/_Project/Scripts/Core/Input/InputSystem_Actions.inputactions`, đọc qua `IInputService`.

| Hành động | Bàn phím/chuột | Gamepad |
| --- | --- | --- |
| Di chuyển | `W` `A` `S` `D` hoặc phím mũi tên | Left Stick |
| Nhìn | Chuột | Right Stick |
| Chạy | Giữ `Left Shift` | Nhấn Left Stick |
| Cúi (bật/tắt) | `C` | East button |
| Tương tác | `E` | North button |
| Huỷ / thoát (TV, đóng màn hình) | `Esc` | East button, Start |
| Pause | `Esc` | Start |
| Đèn pin (bật/tắt, cần đã nhặt) | `F` | West button |

> Khi thay đổi input action, lưu asset để sinh lại `GameInputActions.cs` và cập nhật bảng quy tắc context. Chi tiết: [Docs/PlayerSystem.md](Docs/PlayerSystem.md#input).

## Cấu trúc project

```text
Assets/
├── _Project/                         # Nội dung do team phát triển
│   ├── Animations/                    # Animation nhân vật và môi trường
│   ├── Audio/                         # Ambience, music, SFX, voiceover
│   ├── Data/                          # Config SO, Flags/, Items/
│   ├── Materials/                     # Material nội bộ
│   ├── Models/                        # Model nhân vật, môi trường và prop
│   ├── Prefabs/                       # Prefab character, environment, system, interactable
│   ├── Scenes/                        # Bootstrap, Menu, Gameplay
│   ├── Scripts/                       # C# theo từng domain
│   │   ├── Bootstrap/                 # Composition root: LifetimeScope và flow của từng scene
│   │   ├── Core/                      # Contract dùng chung, scene loader, input service, flag service, item contract
│   │   ├── Editor/                    # Công cụ Editor (Scene Switcher)
│   │   ├── Interaction/               # Raycast, focus và interactable
│   │   ├── Inventory/                 # InventoryService, PickupItem
│   │   ├── Player/                    # FPS controller, look, camera rig, focus/rung camera, đèn pin
│   │   ├── UI/                        # Screen service, menu, pause, settings, fade, subtitle, crosshair
│   │   └── Audio/, Enemy/, Environment/, Puzzles/, SaveSystem/, Attributes/
│   ├── Settings/                      # VContainerSettings, Input System settings
│   ├── Tests/EditMode/                # EditMode test
│   └── UI/                            # Font, icon, texture UI
├── Plugins/Demigiant/DOTween/         # DOTween
├── X_ThirdParty/StarterAssets/        # Chỉ giữ Environment/ (mesh, material dựng map thử)
└── Settings/                          # URP asset và renderer cho PC/Mobile
Docs/
├── DependencyInjection.md             # Quy ước VContainer, MessagePipe, scope
├── SceneFlow.md                       # Luồng scene, ISceneLoader, Scene Switcher
├── PlayerSystem.md                    # Input context, camera, FPS controller
├── InteractionSystem.md               # Tài liệu interaction
├── UISystem.md                        # Màn hình, pause, settings, fade, subtitle
├── FlagsInventory.md                  # Flag, inventory, pickup, đèn pin
└── DevelopmentPlan.md                 # Kế hoạch phát triển theo branch
```

Không sửa trực tiếp `Library/`, `Temp/`, `Logs/` hoặc các file `.csproj`: chúng do Unity/IDE sinh tự động và đã được `.gitignore` loại trừ.

## Các hệ thống hiện có

### Player controller

FPS controller tự viết trong `Assets/_Project/Scripts/Player/`: `PlayerController` (entry point VContainer) ghép các module C# thuần `FirstPersonMotor`, `FirstPersonLook`, `CrouchState`, `StaminaModel`, `HeadBob`. Số liệu chỉnh trong `Assets/_Project/Data/Player/PlayerMovementConfig.asset` và `FirstPersonLookConfig.asset`.

Scene gameplay cần hai prefab, `GameplayLifetimeScope` tự tìm chúng trong hierarchy:

- `Assets/_Project/Prefabs/Characters/Player.prefab`: pivot ở chân, đặt `y` bằng mặt sàn.
- `Assets/_Project/Prefabs/System/CameraRig.prefab`: `Main Camera` + `CinemachineBrain`, `FpsCamera`, `FocusCamera`.

Hệ thống khác muốn khoá player hay hiện con trỏ thì push context qua `IInputService.PushContext`. Muốn đưa camera tới một điểm nhìn (TV, puzzle) thì dùng `ICameraFocusService`; rung camera dùng `ICameraShakeService`. Chi tiết: [Docs/PlayerSystem.md](Docs/PlayerSystem.md).

### Interaction và pickup

Luồng tương tác: camera raycast từ giữa màn hình → `PlayerInteractor` chọn collider gần nhất không bị che → `InteractableBehaviour` xử lý hành động → UI cập nhật focus.

| Thành phần | Vai trò |
| --- | --- |
| `PlayerInteractor` | C# thuần, raycast từ camera của `CameraRig`, khoảng cách mặc định 3 m. Đọc `IInputService`, chỉ chạy ở context `Gameplay`. |
| `InteractableBehaviour` | Base class cho mọi đối tượng tương tác. |
| `DestroyOnInteract` | Object dùng một lần; tắt collider rồi bị destroy khi tương tác thành công. |
| `PickupItem` | Nhặt `ItemDefinition` vào `IInventoryService`; chỉ hủy world item khi inventory nhận. |
| `InteractionCrosshairUI` | Crosshair phản hồi target đang focus. |

Hướng dẫn cấu hình chi tiết, outline và test case: [Docs/InteractionSystem.md](Docs/InteractionSystem.md).

### Flag và inventory

Tiến trình chơi lưu ở hai service trong scope Gameplay: `IFlagService` (flag bool, asset `FlagId`) và `IInventoryService` (item, asset `ItemDefinition`). Flag và item đều là ScriptableObject trong `Assets/_Project/Data/Flags/` và `Data/Items/`, nên đổi tên không làm hỏng reference. Nhặt đồ hiện subtitle `Picked up: …`. Đèn pin chỉ bật được sau khi nhặt item `Flashlight`. Chi tiết: [Docs/FlagsInventory.md](Docs/FlagsInventory.md).

### UI

UI tối giản, không dùng package UI manager. Mọi màn hình (menu, pause, settings, sau này note, inventory, keypad) kế thừa `UIScreen` và mở/đóng qua `IUIScreenService`: service tự khoá player, hiện con trỏ, xử lý Esc và chồng màn hình. Pause dừng hẳn game; settings gồm âm lượng và độ nhạy chuột, lưu PlayerPrefs. Chuyển scene có fade đen qua `ISceneTransition`; subtitle qua `ISubtitleService`.

Prefab dựng sẵn trong `Assets/_Project/Prefabs/UI/`: `PauseScreen`, `SettingsScreen`, `SubtitleView`. Chi tiết, cách thêm màn hình mới và setup scene: [Docs/UISystem.md](Docs/UISystem.md).

### Assembly definition

Mã nguồn chia assembly để kiểm soát dependency và rút ngắn thời gian compile. Khi thêm script, đặt nó vào đúng domain và không tạo dependency ngược:

```text
Core ───────► Interaction ───────► UI
  │               │
  │               ├──────────────► Inventory
  │               └──────────────► Player
  └──────────────────────────────► Player ───────► UI
```

Nếu cần tham chiếu domain khác, cập nhật `.asmdef` tương ứng qua Inspector của Unity. Tránh tham chiếu sang `X_ThirdParty` nếu có thể; thay đổi ở đó dễ bị ghi đè khi nâng version asset.

## Thêm nội dung mới

### Object tương tác đơn giản

1. Tạo GameObject có mesh và `Collider`.
2. Đặt object trên layer `Interactable` (layer 9) nếu dùng quy ước mặc định.
3. Thêm `DestroyOnInteract` để thử nghiệm, hoặc tạo class kế thừa `InteractableBehaviour` cho gameplay riêng.
4. Nếu cần highlight, thêm `RendererMaterialInteractionHighlight`; gán renderer và `Assets/_Project/Materials/InteractionOutline.mat`.
5. Kéo component highlight vào `Focus Feedback Sources` của interactable.
6. Đảm bảo `Visibility Layers` của `PlayerInteractor` chứa layer của object lẫn geometry che khuất.

Không chỉ raycast vào layer `Interactable`: raycast phải nhìn thấy tường/địa hình để người chơi không tương tác xuyên vật cản.

### Pickup

1. Tạo item nếu chưa có: **Create → The Shadow Wood → Items → Item** trong `Assets/_Project/Data/Items/`.
2. Kéo `Assets/_Project/Prefabs/Interactables/PickupItem_Base.prefab` vào dưới root `Interactables` của scene.
3. Gán `Item`, đổi mesh theo đồ thật.
4. Play-test cả trường hợp nhặt được và inventory đầy: item chỉ bị destroy khi nhận thành công.

Flag mới tạo bằng **Create → The Shadow Wood → Flags → Flag** trong `Data/Flags/`. Chi tiết: [Docs/FlagsInventory.md](Docs/FlagsInventory.md).

### Scene mới

1. Tạo scene trong `Assets/_Project/Scenes/`.
2. Thêm một `LifetimeScope` riêng cho scene, để trống field Parent.
3. Nếu scene được load bằng code: thêm giá trị vào `SceneId`, thêm `case` vào `SceneCatalog`, thêm scene vào Build Settings. Chi tiết: [Docs/SceneFlow.md](Docs/SceneFlow.md#thêm-scene-mới).
4. Thêm environment, `Player.prefab`, `CameraRig.prefab` và EventSystem cần thiết. Kiểm tra chỉ có một `AudioListener`, layer collision và các reference UI. Setup player: [Docs/PlayerSystem.md](Docs/PlayerSystem.md#setup-scene-gameplay).
5. Test từ một session Unity sạch để phát hiện reference thiếu hoặc asset chưa commit.

## Kiểm thử

Chạy qua **Window → General → Test Runner → EditMode → Run All**. Test hiện có:

| Assembly | File | Bao phủ |
|---|---|---|
| `TheShadowWood.Interaction.Tests` | `Tests/EditMode/PlayerInteractorTests.cs` | Focus object ở tâm màn hình, không tương tác xuyên collider che khuất, gọi action đúng một lần |
| `TheShadowWood.Bootstrap.Tests` | `Tests/EditMode/Bootstrap/LifetimeScopeTests.cs` | Container Root/Gameplay build được, MessagePipe publish/subscribe đúng |
| `TheShadowWood.Bootstrap.Tests` | `Tests/EditMode/Bootstrap/SceneLoaderTests.cs` | Chặn load trùng, kiểm tra Build Settings, reset `IsLoading` khi lỗi/cancel |
| `TheShadowWood.Core.Tests` | `Tests/EditMode/Core/InputContext*Tests.cs` | Bảng quy tắc context, push/dispose context stack |
| `TheShadowWood.Core.Tests` | `Tests/EditMode/Core/FlagServiceTests.cs` | Set/Clear flag, chỉ publish khi đổi |
| `TheShadowWood.Inventory.Tests` | `Tests/EditMode/Inventory/*Tests.cs` | Inventory add/remove/giới hạn, pickup bị chặn |
| `TheShadowWood.Player.Tests` | `Tests/EditMode/Player/*Tests.cs` | Look, motor, crouch, stamina, head bob, đèn pin |

Trước khi tạo pull request, tối thiểu: Console không có compile error, EditMode tests pass, Play từ `Bootstrap` và đi hết luồng `Menu` → `Gameplay`. Nếu thay đổi controller hoặc interaction, play-test thêm trong `Gameplay`.

## Quy ước đóng góp

- Giữ asset do team tạo trong `Assets/_Project/`; asset vendor nằm ở `Assets/X_ThirdParty/` hoặc `Assets/Plugins/`.
- Commit cùng file `.meta` với mọi asset Unity mới, di chuyển hoặc đổi tên.
- Không commit `Library/`, `Temp/`, `Logs/`, `UserSettings/`, solution hay project file do Unity sinh ra.
- Không tự ý sửa asset bên thứ ba. Nếu bắt buộc, ghi rõ phạm vi và lý do trong pull request.
- Đặt namespace theo domain, ví dụ `TheShadowWood.Interaction`, `TheShadowWood.Player`.
- Khi thay đổi input, layer, prefab chung hoặc API public, cập nhật tài liệu trong `Docs/` và kiểm tra các scene phụ thuộc.

## Khắc phục sự cố

| Vấn đề | Cách xử lý |
| --- | --- |
| Unity yêu cầu upgrade project | Cài đúng Unity `6000.3.12f1`; không commit thay đổi upgrade nếu team chưa thống nhất. |
| Asset thiếu hoặc là file pointer nhỏ | Chạy `git lfs pull`, rồi mở lại project. |
| Package/compile lỗi sau clone | Đóng Unity, xóa **chỉ** `Library/` rồi mở lại để Unity import lại package/asset. |
| Không thể tương tác | Kiểm tra camera `MainCamera`, `PlayerInteractor`, collider, visibility layer và vật cản gần camera. |
| Pickup không biến mất / hiện `Can't carry more` | Kiểm tra `PickupItem` đã gán `Item`, nằm dưới root `Interactables` (auto-inject), và inventory chưa đủ `Max Count`. |
| Player không đi/nhìn được, con trỏ không khoá | Có context `UI`/`Focus`/`Puzzle` chưa được dispose. Kiểm tra `IInputService.Context` và class đã push context. |
| Player lún nửa dưới sàn | Pivot của Player phải ở chân: `Visual` và `CameraRoot` nằm trên pivot, đặt `y` của Player bằng mặt sàn. |
| Hai camera cùng nghe âm thanh | Tắt hoặc xóa `AudioListener` thừa; chỉ để một listener enabled. |

---

Tài liệu theo hệ thống: [Dependency Injection](Docs/DependencyInjection.md) · [Scene Flow](Docs/SceneFlow.md) · [Player System](Docs/PlayerSystem.md) · [Interaction System](Docs/InteractionSystem.md) · [UI System](Docs/UISystem.md) · [Flags & Inventory](Docs/FlagsInventory.md)
