# Character Controller

> Tài liệu nội bộ cho hệ thống điều khiển nhân vật góc nhìn thứ nhất của **The Shadow Wood**.  
> Phạm vi được rà soát: movement, camera, jump, crouch, sprint, stamina, head bob, input, đẩy Rigidbody và stamina UI.  
> Cập nhật theo code và scene `Playground` ngày 03/08/2026.

## 1. Giới thiệu

Character Controller hiện tại được xây trên **Unity Starter Assets – First Person Controller**, sau đó project mở rộng thêm:

- Crouch có thay đổi chiều cao collider và camera, đồng thời kiểm tra trần trước khi đứng lên.
- Sprint hỗ trợ hai chế độ `Hold` và `Toggle`.
- Stamina tiêu hao khi sprint, có delay trước khi hồi và phát event cho UI.
- Head bob riêng cho đi bộ, chạy và crouch.
- Stamina UI tự hiện khi stamina không đầy và tự ẩn khi hồi đầy.
- Tùy chọn đẩy các Rigidbody khi người chơi va vào.

Hệ thống sử dụng `CharacterController`, không dùng Rigidbody để di chuyển Player. Hướng di chuyển tương đối theo hướng quay của Player/camera.

## 2. Các file liên quan

| Hạng mục | Đường dẫn | Vai trò |
|---|---|---|
| Controller chính | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Scripts/FirstPersonController.cs` | Di chuyển, quay camera, gravity, jump, crouch, sprint, grounded check |
| Input bridge | `Assets/X_ThirdParty/StarterAssets/InputSystem/StarterAssetsInputs.cs` | Nhận callback từ Input System và lưu trạng thái input |
| Input Actions | `Assets/X_ThirdParty/StarterAssets/InputSystem/StarterAssets.inputactions` | Khai báo action, binding keyboard/mouse và gamepad |
| Stamina | `Assets/_Project/Scripts/Player/PlayerStamina.cs` | Tiêu hao, hồi stamina và phát event |
| Head bob | `Assets/_Project/Scripts/Player/PlayerHeadBob.cs` | Tạo dao động camera khi di chuyển |
| Stamina UI | `Assets/_Project/Scripts/UI/StaminaUIHandler.cs` | Cập nhật thanh stamina và fade bằng DOTween |
| Rigidbody push | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Scripts/BasicRigidBodyPush.cs` | Đẩy Rigidbody theo hướng va chạm |
| Player prefab | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab` | Prefab nền của Player |
| Virtual camera | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/PlayerFollowCamera.prefab` | Cinemachine camera theo `PlayerCameraRoot` |
| Main camera | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/MainCamera.prefab` | Camera thật, Audio Listener và Cinemachine Brain |
| Scene mẫu đang tích hợp đủ | `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Scenes/Playground.unity` | Cấu hình tham chiếu để test |

> Lưu ý: controller chính và input bridge nằm trong `X_ThirdParty` nhưng đã được project chỉnh sửa. Khi update/reimport Starter Assets cần kiểm tra diff để không làm mất crouch, sprint mode và các API mà code project đang dùng.

## 3. Kiến trúc và luồng hoạt động

```text
Keyboard / Mouse / Gamepad
            │
            ▼
StarterAssets.inputactions
            │  PlayerInput — Send Messages
            ▼
StarterAssetsInputs
  move, look, jump, sprint, crouch
      │             │
      │             ├──────────────► PlayerStamina ──event──► StaminaUIHandler
      │             │
      ▼             ▼
FirstPersonController ◄──────────── PlayerHeadBob đọc velocity/grounded
      │
      ├── CharacterController.Move()
      └── PlayerCameraRoot rotation/height ──► Cinemachine ──► MainCamera
```

Luồng xử lý trong mỗi frame:

1. Bên trong `FirstPersonController.Update`: crouch → sprint → jump/gravity → grounded check → move.
2. `PlayerStamina.Update` độc lập đọc trạng thái input để consume hoặc regenerate stamina.
3. Trong `LateUpdate`, controller quay `PlayerCameraRoot`; head bob thay đổi local position của target.
4. Cinemachine cập nhật camera thật theo target.

Project chưa đặt Script Execution Order riêng, vì vậy không nên phụ thuộc vào thứ tự tương đối giữa `Update`/`LateUpdate` của hai component khác nhau.

Các component sau phải ở **cùng GameObject Player** vì chúng dùng `GetComponent`:

- `CharacterController`
- `PlayerInput`
- `StarterAssetsInputs`
- `FirstPersonController`
- `PlayerStamina` nếu dùng stamina
- `PlayerHeadBob` nếu dùng head bob
- `BasicRigidBodyPush` nếu Player cần đẩy vật thể

## 4. Điều khiển mặc định

| Action | Keyboard/Mouse | Gamepad | Ghi chú |
|---|---|---|---|
| Move | `WASD` hoặc phím mũi tên | Left Stick | Vector2 |
| Look | Mouse Delta | Right Stick | Input đã có processor invert Y và scale |
| Jump | `Space` | South Button | Thường là A/Cross |
| Sprint | `Left Shift` | Left Trigger | `Hold` hoặc `Toggle` do component input quyết định |
| Crouch | `Left Ctrl` | Chưa có binding | `Hold` hoặc `Toggle` |
| Interact | `E` | North Button | Raycast interaction từ tâm camera |

Input asset đang có hai control scheme chính: `KeyboardMouse` và `Gamepad`. `Xbox Controller` và `PS4 Controller` có tên trong asset nhưng chưa khai báo device/binding group riêng.

`PlayerInput` phải có:

- **Actions**: `StarterAssets.inputactions`
- **Default Map**: `Player`
- **Behavior**: `Send Messages`

Tên action movement phải khớp các method `OnMove`, `OnLook`, `OnJump`, `OnSprint`, `OnCrouch` trong `StarterAssetsInputs`. Action `Interact` được nhận bởi `PlayerInteractor.OnInteract` trên cùng GameObject. Khi thêm hoặc đổi tên action, phải cập nhật callback tương ứng.

## 5. Cấu hình Player

### 5.1 CharacterController

Cấu hình hiện tại trong prefab:

| Setting | Giá trị | Ý nghĩa |
|---|---:|---|
| Height | `2` | Chiều cao đứng |
| Radius | `0.5` | Bán kính capsule |
| Center | `(0, 0.93, 0)` | Tâm collider lúc bắt đầu |
| Slope Limit | `45°` | Độ dốc tối đa có thể đi |
| Step Offset | `0.25` | Bậc cao tối đa có thể bước lên |
| Skin Width | `0.02` | Lớp đệm va chạm |
| Min Move Distance | `0` | Không bỏ qua movement nhỏ |

Khi crouch, script lerp `Height` về `CrouchHeight` và tự đặt `Center.y = currentHeight / 2`. Khi đứng lên, center cũng được tính theo công thức này, không trả lại chính xác vector center ban đầu. Vì vậy nên đặt pivot Player ở chân và tránh dựa vào center X/Z khác `0`.

### 5.2 FirstPersonController — Player

| Setting | Mặc định hiện tại | Mô tả / cách chỉnh |
|---|---:|---|
| Move Speed | `4 m/s` | Tốc độ đi thường |
| Sprint Speed | `6 m/s` | Tốc độ chạy; nên lớn hơn Move Speed |
| Rotation Speed | `1` | Độ nhạy look tổng; còn chịu ảnh hưởng processor trong Input Actions |
| Speed Change Rate | `10` | Độ nhanh khi tăng/giảm tốc. Cao = phản hồi gắt, thấp = có quán tính |
| Jump Height | `1.2 m` | Độ cao jump mong muốn |
| Gravity | `-15` | Gravity riêng của controller; phải là số âm |
| Jump Timeout | `0.1 s` | Khoảng chờ trước lần jump tiếp theo sau khi chạm đất |
| Fall Timeout | `0.15 s` | Timer fall hiện chưa được nối với animation/state khác |

Vận tốc nhảy được tính theo `sqrt(JumpHeight × -2 × Gravity)`. Thay Gravity sẽ đồng thời thay cảm giác bay/rơi và vận tốc nhảy ban đầu.

### 5.3 FirstPersonController — Crouch

| Setting | Mặc định trong code | Mô tả / cách chỉnh |
|---|---:|---|
| Crouch Speed | `2 m/s` | Tốc độ khi cúi |
| Crouch Height | `1 m` | Chiều cao collider khi cúi; phải nhỏ hơn height đứng |
| Crouch Transition Speed | `10` | Tốc độ lerp collider và camera |

Camera crouch được tính tự động:

```text
crouchedCameraY = originalCameraY - (standingHeight - crouchHeight)
```

Với prefab hiện tại: camera target Y `1.375`, standing height `2`, crouch height `1` → camera crouch mục tiêu Y `0.375`.

Khi người chơi đang crouch và có collider thuộc `GroundLayers` nằm trong sphere kiểm tra trần, script giữ trạng thái crouch. Jump khi đang crouch chỉ xảy ra nếu có thể đứng lên; nếu trần thấp, input jump bị consume nhưng Player vẫn crouch.

### 5.4 FirstPersonController — Grounded

| Setting | Prefab hiện tại | Mô tả |
|---|---:|---|
| Grounded | `true` | Trạng thái runtime, không phải setting gameplay |
| Grounded Offset | `-0.14` | Dịch vị trí sphere check theo công thức trong code |
| Grounded Radius | `0.5` | Bán kính sphere check, thường gần bằng controller radius |
| Ground Layers | `Default` | Layer được xem là mặt đất và cũng được dùng để check trần |

Chọn Player trong Scene view để thấy sphere gizmo: xanh khi grounded, đỏ khi không grounded.

Khi chỉnh:

- Player không nhận grounded: kiểm tra layer nền, radius và offset.
- Player grounded khi chưa chạm đất: giảm radius hoặc chỉnh offset.
- Không đứng lên được dù trần thoáng: kiểm tra object lân cận có nằm nhầm trong `GroundLayers` hay không.
- Không đưa layer của chính Player vào `GroundLayers`.

> Hiện `GroundLayers` đồng thời dùng cho mặt đất và kiểm tra trần. Mọi collider trần cần thuộc một layer có trong mask này.

### 5.5 FirstPersonController — Camera

| Setting | Prefab hiện tại | Mô tả |
|---|---:|---|
| Cinemachine Camera Target | `PlayerCameraRoot` | Bắt buộc gán; controller sẽ lỗi null nếu thiếu |
| Top Clamp | `89°` | Giới hạn nhìn lên |
| Bottom Clamp | `-89°` | Giới hạn nhìn xuống |

Camera setup gồm ba object:

1. `PlayerCameraRoot`: child của Player, target để controller xoay pitch và thay đổi độ cao.
2. `PlayerFollowCamera`: Cinemachine Camera, `Tracking Target` trỏ tới `PlayerCameraRoot`.
3. `MainCamera`: tag `MainCamera`, chứa Unity Camera, Audio Listener và Cinemachine Brain.

`PlayerFollowCamera` hiện dùng lens FOV `40`, near clip `0.2`, far clip `500`, `CinemachineThirdPersonFollow` với camera distance `0` để hoạt động như first-person camera. Prefab cũng đang có Cinemachine noise; cân nhắc tổng biên độ khi dùng cùng head bob để tránh rung quá mạnh.

## 6. Sprint và stamina

### 6.1 Sprint mode

Trong `StarterAssetsInputs`:

| Setting | Tùy chọn | Hành vi |
|---|---|---|
| Sprint Mode | `Hold` | Chỉ sprint khi giữ nút |
| Sprint Mode | `Toggle` | Nhấn một lần để bật/tắt; tự tắt khi input move về zero |
| Crouch Mode | `Hold` | Chỉ crouch khi giữ nút |
| Crouch Mode | `Toggle` | Nhấn một lần để bật/tắt |

Scene `Playground` hiện dùng **Hold cho cả sprint và crouch** (`enum value 0`). Default khai báo trong code là sprint `Hold`, crouch `Toggle`; vì vậy object mới add component có thể khác scene mẫu.

Sprint bị tắt khi:

- Player crouch.
- Stamina bằng `0`.
- Dừng di chuyển trong chế độ sprint `Toggle`.

### 6.2 PlayerStamina

| Setting | Scene `Playground` | Ý nghĩa |
|---|---:|---|
| Max Stamina | `100` | Stamina tối đa và giá trị khởi tạo |
| Stamina Drain Rate | `20/s` | Mức tiêu hao mỗi giây khi vừa có move input vừa sprint |
| Stamina Regen Rate | `15/s` | Mức hồi mỗi giây |
| Regen Delay Duration | `1.5 s` | Thời gian chờ tính từ lần consume cuối trước khi hồi |

Với cấu hình hiện tại:

- Chạy liên tục từ đầy đến cạn: khoảng `5 giây`.
- Sau khi ngừng chạy: chờ `1.5 giây`.
- Hồi từ cạn đến đầy sau thời gian chờ: khoảng `6.67 giây`.

API public để hệ thống khác dùng:

| API | Kiểu | Dùng cho |
|---|---|---|
| `CurrentStamina` | `float`, chỉ đọc ngoài class | Giá trị stamina tuyệt đối |
| `IsExhausted` | `bool` | Kiểm tra stamina đã cạn |
| `OnStaminaChanged` | `Action<float>` | Ratio chuẩn hóa `0..1`, phù hợp cho UI/audio/effect |
| `OnStaminaExhausted` | `Action` | Trigger thở dốc, âm thanh hoặc feedback một lần khi chạm 0 |
| `Consume(deltaTime)` | method | Tiêu hao theo drain rate |
| `Regenerate(deltaTime)` | method | Hồi theo regen rate và delay |

`OnStaminaExhausted` hiện chưa có subscriber trong project. Đây là hook sẵn cho SFX thở dốc hoặc vignette.

## 7. Head bob

`PlayerHeadBob` đọc vận tốc ngang thực tế từ `FirstPersonController.HorizontalVelocity`. Head bob chỉ chạy khi tốc độ từ `0.1 m/s` trở lên và Player đang grounded.

| Setting | Default trong code | Scene `Playground` | Ý nghĩa |
|---|---:|---:|---|
| Camera Target | chưa gán | `PlayerCameraRoot` | Bắt buộc để có hiệu ứng |
| Walk Bob Speed | `14` | `4` | Tần số khi đi |
| Sprint Bob Speed | `18` | `6` | Tần số khi sprint |
| Crouch Bob Speed | `10` | `2` | Tần số khi crouch |
| Bob Horizontal Amount | `0.05` | `0.02` | Biên độ ngang theo local X |
| Bob Vertical Amount | `0.05` | `0.0015` | Biên độ dọc theo local Y |

Khi sprint, cả offset ngang và dọc được nhân thêm `1.3`. Khi dừng hoặc ở trên không, local X lerp về vị trí ban đầu; local Y tiếp tục do controller crouch quản lý.

Khuyến nghị tuning cho horror FPS:

- Chỉnh **frequency** trước để khớp nhịp bước chân, sau đó mới tăng amount.
- Giữ vertical amount nhỏ để tránh say chuyển động; cấu hình scene `0.0015` an toàn hơn rất nhiều so với default code `0.05`.
- Test đồng thời walk, sprint, crouch, lên dốc, xuống cầu thang và chuyển trạng thái crouch.
- Nếu có accessibility setting, cho phép giảm hoặc tắt head bob bằng cách disable component/đặt amount về `0`.

## 8. Stamina UI

`StaminaUIHandler` cần ba reference:

| Field | Cần gán |
|---|---|
| Stamina Fill Image | `Image` dùng làm phần fill |
| Stamina Canvas Group | `CanvasGroup` của cụm UI cần fade |
| Target Stamina System | `PlayerStamina` trên Player |

`Fade Duration` trong `Playground` là `0.3 giây`. UI hiện khi stamina ratio `< 0.99` và ẩn khi ratio đạt ít nhất `0.99`. Tween dùng unscaled time nên vẫn chạy khi `Time.timeScale = 0`.

Thanh stamina hiện được cập nhật bằng `transform.localScale.x`, không dùng `Image.fillAmount`. Để thanh rút từ phải sang trái đúng cách:

- Pivot của fill nên nằm bên trái (`Pivot X = 0`).
- Không để layout component khác liên tục ghi đè scale.
- Parent/background giữ nguyên size; chỉ scale child fill.

Component tự subscribe trong `OnEnable`, unsubscribe và kill tween trong `OnDisable`.

## 9. Đẩy Rigidbody

`BasicRigidBodyPush` là tính năng tùy chọn:

| Setting | Prefab hiện tại | Ý nghĩa |
|---|---:|---|
| Can Push | `false` | Bật/tắt tính năng |
| Push Layers | `Nothing` | Chỉ Rigidbody thuộc các layer này mới bị đẩy |
| Strength | `1.1` | Impulse ngang khi va chạm |

Object bị đẩy phải có Rigidbody không kinematic và nằm trong `Push Layers`. Va chạm phía dưới Player không bị đẩy để tránh Player tạo lực lên vật đang đứng trên.

## 10. Setup controller trong scene mới

### Cách nhanh từ prefab hiện tại

1. Kéo `PlayerCapsule.prefab` vào scene.
2. Kéo `PlayerFollowCamera.prefab` vào scene.
3. Kéo `MainCamera.prefab` vào scene; bảo đảm chỉ có một enabled `AudioListener`.
4. Trên `PlayerFollowCamera`, gán `Tracking Target = PlayerCapsule/PlayerCameraRoot`.
5. Trên `FirstPersonController`, xác nhận `Cinemachine Camera Target = PlayerCameraRoot`.
6. Add `PlayerStamina` và chỉnh stamina nếu scene cần sprint giới hạn.
7. Add `PlayerHeadBob`, gán `Camera Target = PlayerCameraRoot` và dùng profile amount đã test.
8. Nếu cần stamina HUD, tạo/instantiate UI rồi gán đủ ba reference cho `StaminaUIHandler`.
9. Kiểm tra `GroundLayers`, layer của mặt đất và layer của trần.
10. Play test keyboard/mouse, gamepad, crouch dưới trần thấp, jump, stamina cạn/hồi và camera.

> Quan trọng: `PlayerStamina` và `PlayerHeadBob` hiện là **component override chỉ có trong scene `Playground`**, chưa nằm sẵn trong `PlayerCapsule.prefab`. Kéo prefab vào scene mới sẽ không tự có hai hệ thống này.

### Checklist Inspector

- [ ] Player có tag `Player`.
- [ ] Scene có đúng một camera mang tag `MainCamera`.
- [ ] `PlayerInput` dùng đúng action asset, map `Player`, behavior `Send Messages`.
- [ ] `PlayerCameraRoot` được gán cho controller, head bob và Cinemachine Camera.
- [ ] Ground/ceiling collider nằm trong `GroundLayers`.
- [ ] Player layer không nằm trong `GroundLayers`.
- [ ] Stamina UI trỏ đúng instance Player của scene.
- [ ] Fill image có pivot phù hợp với cách scale X.
- [ ] Không có hai `AudioListener` enabled.

## 11. Hướng dẫn tuning theo mục tiêu

### Player nặng và căng thẳng hơn

- Giảm `Move Speed` và `Sprint Speed`.
- Giảm `Speed Change Rate` để tăng thời gian tăng/giảm tốc.
- Giảm `Jump Height`; không nên tăng gravity quá mạnh nếu chưa test cầu thang.
- Tăng `Stamina Drain Rate`, tăng `Regen Delay Duration`.
- Giữ head bob amount nhỏ, giảm frequency để tạo bước chân nặng.

### Player phản hồi nhanh hơn

- Tăng `Speed Change Rate`.
- Tăng `Rotation Speed` nhẹ; test riêng mouse và gamepad vì cách áp dụng delta time khác nhau.
- Giảm `Regen Delay Duration` hoặc tăng `Stamina Regen Rate`.

### Crouch qua không gian hẹp

- Chọn `Crouch Height` theo chiều cao tunnel thực tế.
- Bảo đảm tunnel/ceiling thuộc `GroundLayers` để chặn đứng lên.
- Test sphere check trần ở mép cửa, sát tường và dưới dốc; check radius trong code đang cố định `0.2`.

## 12. Lưu ý và giới hạn hiện tại

1. **Prefab và scene chưa đồng bộ:** stamina/head bob chỉ được add ở `Playground`. Nên tạo prefab variant hoặc apply các component sau khi team chốt profile chuẩn.
2. **Layer convention:** Player dùng layer 8 (`Player`), object tương tác dùng layer 9 (`Interactable`). Khi thêm raycast/mask mới phải giữ Player ngoài interaction visibility mask.
3. **Crouch chưa có gamepad binding:** action `Crouch` mới chỉ bind `Left Ctrl`.
4. **Stamina dựa trên input, không dựa trên vận tốc thực:** chỉ cần sprint đang bật và move khác zero là stamina tiêu hao, kể cả khi Player bị tường chặn hoặc đang ở trên không.
5. **Exhausted không có ngưỡng hồi tối thiểu:** stamina vừa lớn hơn 0 là `IsExhausted` thành false, nhưng sprint đã bị set false và người chơi thường phải nhả/nhấn lại nút sprint.
6. **Ground và ceiling dùng chung mask:** chưa thể cấu hình riêng hai nhóm collider.
7. **Ceiling radius hard-code:** bán kính check trần `0.2` chưa xuất ra Inspector.
8. **Head bob có nguy cơ cộng dồn local Y:** code dùng `finalLocalPosition.y += offsetY` mỗi frame. Khi thấy camera drift/rung dọc bất thường, đây là vị trí cần kiểm tra đầu tiên; nên tính Y từ một baseline do crouch/controller cung cấp.
9. **Fall Timeout chưa tạo gameplay effect:** timer được cập nhật nhưng hiện không phát event hoặc điều khiển animation.
10. **Không có runtime rebind/settings UI:** độ nhạy và binding đang chỉnh trực tiếp trong asset/component.
11. **Input asset có control scheme thừa/chưa hoàn tất:** `Xbox Controller` và `PS4 Controller` chưa có device; gamepad crouch chưa được map.
12. **Source thuộc Third Party đã bị chỉnh:** update Starter Assets có thể ghi đè controller/input tùy biến.
13. **Terminal velocity chưa được clamp đúng:** `_terminalVelocity` là số dương `53` trong khi vận tốc rơi là số âm; điều kiện hiện tại vẫn tiếp tục cộng gravity khi rơi. Các cú rơi dài có thể tăng tốc không giới hạn theo giá trị terminal dự kiến.

Các mục trên là mô tả trạng thái hiện tại, không đồng nghĩa đều là bug bắt buộc sửa ngay. Khi sửa hành vi, cập nhật lại tài liệu và profile scene/prefab trong cùng PR.

## 13. Quy ước khi thay đổi hệ thống

- Gameplay tuning nên thực hiện trên prefab variant/profile chuẩn, tránh mỗi scene có một bộ số khác nhau.
- Nếu thêm input action, cập nhật đồng thời `.inputactions`, `StarterAssetsInputs`, binding gamepad và bảng điều khiển trong tài liệu này.
- Nếu đổi `CrouchHeight` hoặc CharacterController height, test lại camera Y, ceiling check, step offset và các lối chui.
- Nếu đổi stamina, ghi lại thời gian chạy đến cạn và thời gian hồi đầy để designer dễ đánh giá.
- Hệ thống khác nên đọc property/event public của `PlayerStamina`, không truy cập field serialized bằng reflection.
- Subscribe event trong `OnEnable` và unsubscribe trong `OnDisable` để tránh callback vào object đã tắt.
- Mọi thay đổi trong `X_ThirdParty` cần được ghi rõ trong review để tránh mất khi nâng version package/asset.

## 14. Test cases tối thiểu

| Nhóm | Test | Kết quả mong đợi |
|---|---|---|
| Move | Đi đủ 8 hướng, dừng/đổi hướng | Tốc độ lerp ổn định, không trượt xuyên collider |
| Look | Mouse và gamepad | Pitch bị clamp, yaw quay Player, sensitivity hợp lý |
| Jump | Jump đứng yên/đang chạy | Chỉ jump khi grounded, đạt độ cao gần setting |
| Crouch | Hold/Toggle theo config | Collider và camera chuyển mượt, tốc độ đúng |
| Ceiling | Crouch dưới trần rồi nhả nút | Không đứng xuyên trần |
| Crouch jump | Jump dưới trần thấp và nơi thoáng | Bị chặn dưới trần; đứng lên và jump ở nơi thoáng |
| Sprint | Hold/Toggle, dừng move, crouch | Mode hoạt động đúng; crouch hủy sprint |
| Stamina | Chạy đến cạn, dừng và hồi | Consume, delay, regen và exhausted đúng |
| UI | Stamina giảm/hồi đầy, pause | UI hiện/ẩn đúng, tween vẫn chạy bằng unscaled time |
| Head bob | Walk/sprint/crouch/jump | Frequency đúng mode, không bob trên không, không drift |
| Physics | Va vật pushable/non-pushable | Chỉ đúng layer và Rigidbody hợp lệ bị đẩy |
| Scene setup | Load scene mới | Đúng camera, đúng Player refs, không duplicate AudioListener |
