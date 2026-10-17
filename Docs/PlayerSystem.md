# Player System

Tài liệu phần player của **The Shadow Wood**: input và context stack, camera FPS bằng Cinemachine, focus/rung camera, và controller di chuyển (motor, crouch, stamina, head bob).

## Tổng quan

```text
RootLifetimeScope
└─ InputService (IInputService) ── context stack: Gameplay / Focus / Puzzle / UI
                                   bật tắt action trong map Player, khoá/hiện con trỏ

GameplayLifetimeScope
├─ PlayerView (MB)        CharacterController, CameraRoot, 2 config SO
├─ CameraRig (MB)         Brain, FpsCamera, FocusCamera, ImpulseSource
├─ PlayerController       entry point: Tick (di chuyển) + LateTick (nhìn, head bob)
│    ├─ FirstPersonLook   yaw/pitch
│    ├─ FirstPersonMotor  tốc độ, gravity, terminal velocity
│    ├─ CrouchState       chiều cao, giữ cúi khi bị trần chặn
│    ├─ StaminaModel      tiêu hao theo vận tốc thực
│    └─ HeadBob           offset quanh vị trí gốc của CameraRoot
├─ CameraRigBinder        gán FpsCamera theo CameraRoot lúc Start
├─ CameraFocusService     (ICameraFocusService) tự push context Focus
└─ CameraShakeService     (ICameraShakeService) bọc CinemachineImpulseSource
```

Logic nằm trong class C# thuần, nhận config qua constructor nên test EditMode được. `PlayerView` và `CameraRig` chỉ giữ reference. Quy ước DI: [DependencyInjection.md](DependencyInjection.md).

## File chính

| File | Assembly | Vai trò |
|---|---|---|
| `Core/Input/InputSystem_Actions.inputactions` | Core | Action map `Player` và `UI`. Sinh class `GameInputActions.cs` cùng thư mục |
| `Core/Input/InputContext.cs`, `InputContextRules.cs` | Core | Các context và bảng quy tắc action/con trỏ |
| `Core/Input/InputContextStack.cs` | Core | Stack các handle, dispose theo thứ tự bất kỳ |
| `Core/Input/IInputService.cs`, `InputService.cs` | Core | Đọc input theo context, đăng ký ở Root |
| `Core/Cameras/ICameraFocusService.cs`, `ICameraShakeService.cs` | Core | Contract camera cho feature khác dùng |
| `Player/PlayerView.cs` | Player | Reference của prefab Player |
| `Player/PlayerController.cs` | Player | Entry point, ghép các module dưới đây |
| `Player/Look/FirstPersonLook.cs`, `FirstPersonLookConfig.cs` | Player | Yaw/pitch, sensitivity, clamp |
| `Player/Movement/*.cs` | Player | `FirstPersonMotor`, `CrouchState`, `StaminaModel`, `HeadBob`, `PlayerMovementConfig` |
| `Player/Camera/*.cs` | Player | `CameraRig`, `CameraRigBinder`, `CameraFocusService`, `CameraShakeService` |
| `Player/Dev/CameraSandboxDebug.cs` | Player | Phím test focus/rung, chỉ dùng khi dev |

Asset:

| Asset | Vai trò |
|---|---|
| `Assets/_Project/Prefabs/Characters/Player.prefab` | Player: `CharacterController`, `PlayerView`, `CameraRoot`, `Visual` |
| `Assets/_Project/Prefabs/System/CameraRig.prefab` | `Main Camera` (Brain), `FpsCamera`, `FocusCamera` |
| `Assets/_Project/Data/Player/FirstPersonLookConfig.asset` | Độ nhạy chuột/gamepad, invert Y, giới hạn pitch |
| `Assets/_Project/Data/Player/PlayerMovementConfig.asset` | Tốc độ, gravity, crouch, stamina, head bob |
| `Assets/_Project/Settings/InputSystem.inputsettings.asset` | Input System settings của project (Project Settings → Input System Package) |

## Input

### Điều khiển

| Hành động | Bàn phím/chuột | Gamepad | Kiểu |
|---|---|---|---|
| Di chuyển | `W` `A` `S` `D` / phím mũi tên | Left Stick | |
| Nhìn | Chuột | Right Stick | |
| Chạy | Giữ `Left Shift` | Nhấn Left Stick | Giữ |
| Cúi | `C` | East button | Bật/tắt |
| Tương tác | `E` | North button | |
| Huỷ / thoát (`Cancel`) | `Esc` | East button (B), Start | Thoát TV, đóng màn hình |
| Pause | `Esc` | Start | Chỉ bật ở `Gameplay` |

Không có jump: nhà là màn chơi trong nhà, nhảy dễ phá nhịp horror.

### Context stack

Hệ thống nào cần chiếm quyền điều khiển thì push một context và giữ handle, dispose handle để trả lại. Context trên đỉnh stack quyết định action nào bật và con trỏ:

| Context | Move | Look | Sprint | Crouch | Interact | Cancel | Pause | Con trỏ | Dùng cho |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|---|---|
| `Gameplay` | ✓ | ✓ | ✓ | ✓ | ✓ | – | ✓ | Khoá, ẩn | Mặc định (đáy stack) |
| `Focus` | – | – | – | – | – | ✓ | – | Khoá, ẩn | TV, cutscene, ending |
| `Puzzle` | – | – | – | – | ✓ | ✓ | – | Hiện | Lá bùa, keypad, bàn thờ |
| `UI` | – | – | – | – | – | ✓ | – | Hiện | Note, pause, menu |

`Pause` và `Cancel` dùng chung phím Esc nên mỗi context bật đúng một trong hai.

Màn hình UI không tự push context: mở qua `IUIScreenService.Open`, service push `UI` và trả lại khi đóng. Xem [UISystem.md](UISystem.md). Push tay chỉ dành cho thứ không phải màn hình:

```csharp
public sealed class SomeCutscene : IDisposable
{
    private readonly IInputService _input;
    private IDisposable _focusContext;

    public void Begin()
    {
        _focusContext ??= _input.PushContext(InputContext.Focus);
    }

    public void End()
    {
        _focusContext?.Dispose();
        _focusContext = null;
    }

    public void Dispose() => End();
}
```

Quy tắc:

- Action bị tắt trả về giá trị mặc định (`Vector2.zero`, `false`), nên `PlayerController` không cần tự kiểm tra context.
- Handle dispose được theo thứ tự bất kỳ và dispose nhiều lần không lỗi. Luôn dispose trong `Dispose()` của class giữ handle để không kẹt context khi unload scene.
- Map `UI` không bị đụng tới, `EventSystem` dùng nó bình thường.
- Unity nhả khoá con trỏ khi cửa sổ mất focus. `InputService` khoá lại khi focus quay về, theo context hiện tại.
- Menu mở qua `UIScreenService`, nên context `UI` được push khi vào Menu và trả lại khi scene Menu unload.
- `Look` của chuột là delta mỗi frame, của gamepad là giá trị stick. `LookIsPointerDelta` cho biết nguồn để `FirstPersonLook` không nhân `deltaTime` với delta chuột. Đọc `Look` trước `LookIsPointerDelta` trong cùng frame.

Thêm action mới: sửa `InputSystem_Actions.inputactions`, lưu để sinh lại `GameInputActions.cs`, thêm cột vào `InputContextRule` và `InputContextRules`, thêm property vào `IInputService`/`InputService`, cập nhật `InputContextRulesTests`. Trường hợp như "vẫn đi được nhưng không nhìn được" thì thêm context mới vào bảng, không thêm cơ chế khoá riêng.

## Camera

`PlayerController` tự xoay yaw cho thân player và pitch cho `CameraRoot`. Cinemachine chỉ bám theo `CameraRoot`, blend giữa các vcam và tạo rung. Không dùng `CinemachinePanTilt`/`CinemachineInputAxisController` vì chúng đọc Input System trực tiếp, đi vòng qua context stack.

| Thành phần trong `CameraRig.prefab` | Cấu hình |
|---|---|
| `Main Camera` | `CinemachineBrain`, `AudioListener` |
| `FpsCamera` | Priority 10. Position Control: Hard Lock to Target, Rotation Control: Rotate With Follow Target. `CinemachineImpulseSource` + `CinemachineImpulseListener`. Tracking Target do `CameraRigBinder` gán lúc Start |
| `FocusCamera` | Priority 0, có `CinemachineImpulseListener`. `CameraFocusService` đặt pose và đổi priority |
| `CameraRig` (MB trên root) | Reference tới các phần trên, `Default Focus Blend` = 0.6 s |

### Focus

```csharp
await _cameraFocus.FocusAsync(tvViewPose, cancellationToken: ct);   // push Focus, blend tới pose
// ... phát video, chờ Cancel ...
await _cameraFocus.ReleaseAsync(cancellationToken: ct);             // blend về FpsCamera, trả context
```

- `FocusAsync` push context `Focus` (một lần, gọi lại để đổi pose không push thêm), đặt `FocusCamera` lên pose, nâng priority trên `FpsCamera` và chờ Brain blend xong.
- `ReleaseAsync` hạ priority, chờ blend xong rồi trả context. Context vẫn được trả nếu bị cancel giữa chừng.
- `blendSeconds` âm thì dùng `Default Focus Blend`, bằng 0 thì cut.
- Pose là một `Transform` đặt trong scene, hướng `forward` về vật cần nhìn.

### Rung

`ICameraShakeService.Shake(force)` gọi `GenerateImpulseWithForce` trên `ImpulseSource` của `FpsCamera`. Hình dạng rung chỉnh trên component `CinemachineImpulseSource` trong prefab.

## Controller

### Prefab Player

Pivot của Player nằm **dưới chân**. `PlayerController` ghi đè `height` và `center` của `CharacterController` mỗi frame theo `CrouchState`, nên không chỉnh tay hai giá trị này để đổi chiều cao, hãy chỉnh `Standing Height`/`Crouch Height` trong config.

| Object | Giá trị |
|---|---|
| `Player` (root) | Layer `Player`. `CharacterController`: Height 1.8, Center (0, 0.9, 0), Radius 0.5, Skin Width 0.08. `PlayerView`. `PlayerInteractorView` (xem [InteractionSystem.md](InteractionSystem.md)) |
| `CameraRoot` | Local (0, 1.6, 0). Pitch đặt ở đây |
| `Visual` | Capsule, local (0, 0.9, 0), scale (1, 0.9, 1) |

Đặt Player trong scene với `y` bằng mặt sàn. Nếu đặt pivot ở giữa thân, controller sẽ rơi xuống cho tới khi đáy collider chạm đất và `Visual` bị chìm nửa dưới sàn.

### Thứ tự trong một frame

`Tick` (Update):

1. Kiểm tra trần nếu đang cúi, cập nhật `CrouchState`, ghi `height`/`center` cho `CharacterController`.
2. Chọn tốc độ tối đa: cúi → `Crouch Speed`, đang giữ sprint và còn stamina → `Sprint Speed`, còn lại → `Walk Speed`. Không sprint khi đang cúi.
3. `FirstPersonMotor` tính vận tốc, `CharacterController.Move`.
4. Lấy vận tốc ngang thực tế từ `CharacterController.velocity`. Chỉ tính là đang sprint khi tốc độ thực lớn hơn `Walk Speed` + 0.1, nên đâm vào tường không tốn stamina.
5. Cập nhật `StaminaModel`.

`LateTick` (LateUpdate): xoay yaw thân và pitch `CameraRoot`, cập nhật `HeadBob`, ghi vị trí `CameraRoot` = vị trí gốc − độ hạ khi cúi + offset bob. Chạy sau Update để Cinemachine đọc pose cuối cùng của frame.

### Module

| Module | Hành vi |
|---|---|
| `FirstPersonMotor` | Tăng/giảm tốc tuyến tính theo `Acceleration`. Khi chạm đất giữ vận tốc đứng ở `Grounded Stick Velocity` để bám dốc/cầu thang. Rơi tự do bị giới hạn ở `-Terminal Velocity` |
| `CrouchState` | Nhấn `Crouch` để bật/tắt. Chiều cao chuyển dần theo `Crouch Transition Speed`. Muốn đứng lên mà trần chặn thì vẫn cúi, tự đứng khi ra khỏi chỗ thấp |
| Check trần | `Physics.CheckSphere` tại đỉnh chiều cao đứng, bán kính `Ceiling Check Radius`, theo `Ceiling Mask`. Mask không được chứa layer `Player` |
| `StaminaModel` | Tiêu hao khi sprint thực sự, chờ `Regen Delay` rồi hồi. Cạn về 0 thì khoá sprint tới khi hồi đủ `Stamina To Resume Sprint`. `PlayerController.Stamina` expose `Current`, `Ratio`, `IsExhausted` cho UI |
| `HeadBob` | Offset tính lại từ đầu mỗi frame, không cộng dồn. Biên độ tỉ lệ với tốc độ so với `Walk Speed`. Đứng yên hoặc trên không thì offset trở về 0 theo `Bob Return Speed` |
| `FirstPersonLook` | Chuột: độ/pixel, không nhân `deltaTime`. Gamepad: độ/giây. Cả hai nhân thêm `SensitivityScale` (độ nhạy người chơi chỉnh trong Settings, `PlayerController` cập nhật qua `IGameSettings.Changed`). Pitch clamp trong `[Min Pitch, Max Pitch]`, giữ góc pitch đặt sẵn trên `CameraRoot` lúc Start |

### Config

`PlayerMovementConfig` (giá trị mặc định):

| Nhóm | Field | Giá trị | Ghi chú |
|---|---|---|---|
| Speed | Walk / Sprint / Crouch Speed | 2 / 3.8 / 1.2 m/s | Sprint không thấp hơn Walk |
| | Acceleration | 12 m/s² | |
| Gravity | Gravity | −15 | Luôn âm |
| | Terminal Velocity | 50 m/s | Số dương |
| | Grounded Stick Velocity | −2 | |
| Crouch | Standing / Crouch Height | 1.8 / 1.1 m | Crouch không cao hơn Standing |
| | Crouch Transition Speed | 4 m/s | |
| | Ceiling Mask / Check Radius | Default / 0.25 m | |
| Stamina | Max / Drain / Regen | 100 / 20/s / 15/s | |
| | Regen Delay | 1.5 s | |
| | Stamina To Resume Sprint | 25 | |
| Head Bob | Walk / Sprint / Crouch Frequency | 1.8 / 2.6 / 1.2 bước/s | |
| | Horizontal / Vertical Amplitude | 0.02 / 0.03 m | |
| | Bob Return Speed | 8 | |

`FirstPersonLookConfig`: Mouse Sensitivity 0.1 độ/pixel, Gamepad Sensitivity 180 độ/s, Invert Y tắt, pitch từ −80 tới 80.

## Setup scene gameplay

Scene `Gameplay` hiện vừa là scene chơi vừa là sandbox: có `Map` (dựng từ `StarterAssets/Environment`), `CameraSandboxDebug` và `FocusPose`.

1. Kéo `Player.prefab` vào scene, `y` bằng mặt sàn.
2. Kéo `CameraRig.prefab` vào scene. Xoá `Main Camera` mặc định của scene để chỉ còn một camera và một `AudioListener`.
3. `GameplayLifetimeScope` tự tìm `PlayerView`, `PlayerInteractorView` và `CameraRig` trong hierarchy (`RegisterComponentInHierarchy`), không cần kéo reference. Camera của scene được đăng ký từ `CameraRig.OutputCamera`, không cần tag `MainCamera`.
4. Kiểm tra `PlayerView` đã gán `Controller`, `Camera Root`, `Look Config`, `Movement Config`.

Test nhanh camera bằng `CameraSandboxDebug`: thêm vào danh sách **Auto Inject Game Objects** của `GameplayLifetimeScope`, gán `Focus Pose`. Khi Play: `F` focus/thả, `Esc` thả, `G` rung. Bỏ component này khỏi scene khi dựng màn thật.

## Kiểm thử

| Assembly | File | Bao phủ |
|---|---|---|
| `TheShadowWood.Core.Tests` | `Core/InputContextRulesTests.cs` | Bảng quy tắc từng context |
| `TheShadowWood.Core.Tests` | `Core/InputContextStackTests.cs` | Push/dispose lồng nhau, dispose sai thứ tự, dispose hai lần, event `Changed` |
| `TheShadowWood.Player.Tests` | `Player/FirstPersonLookTests.cs` | Sensitivity chuột/gamepad, `SensitivityScale` từ settings, invert, clamp pitch |
| `TheShadowWood.Player.Tests` | `Player/FirstPersonMotorTests.cs` | Tăng tốc, gravity, grounded, terminal velocity |
| `TheShadowWood.Player.Tests` | `Player/CrouchStateTests.cs` | Toggle, chuyển chiều cao, giữ cúi khi trần chặn |
| `TheShadowWood.Player.Tests` | `Player/StaminaModelTests.cs` | Tiêu hao, regen delay, khoá khi cạn |
| `TheShadowWood.Player.Tests` | `Player/HeadBobTests.cs` | Offset không cộng dồn, trở về 0 khi dừng |

Play-test thủ công khi sửa phần player: đi/chạy/cúi dưới vật thấp, chạy tới khi cạn stamina, focus rồi `Esc`, mở menu rồi vào game xem con trỏ có khoá lại không, alt-tab trong lúc chơi.

## Việc còn lại

- Chưa có stamina HUD và footstep. `PlayerController` sẽ publish `PlayerStateMessage`/`FootstepMessage` khi có UI và AudioService.
