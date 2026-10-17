# TV Interactable

Tài liệu `TVInteractable`: TV phát video, camera blend tới trước màn hình trong lúc xem. Kế thừa `InteractableBehaviour` nên dùng chung raycast, input, prompt với [Interaction System](InteractionSystem.md). Camera do [`ICameraFocusService`](PlayerSystem.md#focus) điều khiển, TV không tự di chuyển camera.

## Luồng hoạt động

```text
PlayerInteractor (InteractPressed)
        │
        ▼
TVInteractable.PerformInteraction ── WatchAsync (UniTask, token huỷ khi disable/destroy)
        │
        ▼
ICameraFocusService.FocusAsync(viewingCameraPose, focusBlend)
   ├─ push context Focus ──► interactor bỏ focus, prompt/crosshair ẩn, Move/Look tắt, Cancel bật
   └─ FocusCamera lên trên FpsCamera, chờ CinemachineBrain blend xong
        │
        ▼
focusToPlayDelay (nếu > 0)
        │
        ▼
TurnScreenOn + VideoPlayer.Prepare/Play
        │
        ├─ hết video (loopPointReached) ──► videoEndToUnfocusDelay
        └─ IInputService.CancelPressed hoặc CancelWatching() ──► dừng ngay
        │
        ▼
StopVideo (TurnScreenOff) + ICameraFocusService.ReleaseAsync(focusBlend)
   └─ blend về FpsCamera, pop context Focus ──► điều khiển player trở lại
```

TV bị disable hoặc destroy giữa chừng: token bị huỷ, video dừng, camera **cắt** về FpsCamera (`ReleaseAsync(0)`), context `Focus` được trả lại. Context không bao giờ bị treo sau khi TV biến mất.

Không còn tắt controller hay driver camera bằng tay: context `Focus` đã tắt `Move`/`Look`, và Cinemachine chọn camera theo priority.

## Dependency

```csharp
[Inject]
public void Construct(ICameraFocusService cameraFocus, IInputService input)
```

TV phải nằm dưới root được auto-inject (`Interactables`). Chưa được inject thì `CanInteract` trả `false`, prompt không hiện và bấm E không có tác dụng.

## Cấu hình component

### TV Screen

| Field | Ý nghĩa |
|---|---|
| `Video Player` | Tự lấy `GetComponent<VideoPlayer>()` nếu để trống |
| `Screen Renderer` | Renderer chứa material màn hình |
| `Screen Material Index` | Slot material của màn hình trên `Screen Renderer` (0-based) |
| `Screen Texture Property` | Property texture chính, mặc định `_BaseMap` |
| `Video Render Texture` | RenderTexture cho video. Để trống thì tự tạo 1920×1080 và tự giải phóng khi destroy |
| `Video Scale` / `Video Offset` | Tiling/offset khi phát video |
| `Flip Video Horizontally` / `Flip Video Vertically` | Lật texture video. `Vertically` mặc định bật vì RenderTexture thường ngược trục Y |
| `Off Screen Texture` | Texture khi TV tắt. Để trống thì trả về texture gốc cache lúc `Awake` |

### Screen Emission

| Field | Ý nghĩa |
|---|---|
| `Use Emission For Video` | Set `Emission Map` + `Emission Color` khi phát để video không bị tối |
| `Emission Texture Property` | Mặc định `_EmissionMap` |
| `Emission Color` | Màu/cường độ emission (HDR) |

Texture, scale, offset, màu và keyword `_EMISSION` gốc được cache lúc `Awake` và restore khi tắt TV.

### Camera

| Field | Ý nghĩa |
|---|---|
| `Viewing Camera Pose` | Transform trước màn hình, hướng vào TV. **Bắt buộc**, thiếu thì `CanInteract == false` |
| `Focus Blend` | Thời gian blend (giây). Số âm dùng `Default Focus Blend` của `CameraRig`. `0` là cắt thẳng |

### Flow Delays

| Field | Ý nghĩa |
|---|---|
| `Focus To Play Delay` | Chờ sau khi camera blend xong, trước khi phát video |
| `Video End To Unfocus Delay` | Chờ sau khi hết video, trước khi camera rời TV. Không áp dụng khi người chơi cancel |

Hai delay dùng thời gian thực (`ignoreTimeScale`), không bị ảnh hưởng bởi `Time.timeScale`.

### Cancel

Thoát bằng action `Cancel` (`Escape`), đọc qua `IInputService.CancelPressed`. Context `Focus` bật `Cancel`. Nút trên màn hình (nếu có) gọi `CancelWatching()`.

### Prompt

Điền `Prompt` (ví dụ `Xem TV`). Nếu chữ nằm quá cao trên nóc TV, thêm child `PromptAnchor` và gán vào field cùng tên.

## Setup

1. Kéo prefab TV từ `Assets/X_ThirdParty/_Television_set/` (ví dụ `TV_80.prefab`) vào làm con của `Interactables` trong `Gameplay`.
2. Đặt layer `Interactable`, đảm bảo có Collider.
3. Add `TVInteractable` (tự thêm `VideoPlayer`). Gán clip hoặc url cho `VideoPlayer`.
4. Gán `Screen Renderer` và `Screen Material Index`.
5. Tạo child rỗng `ViewPose` trước màn hình, xoay nhìn thẳng vào TV, gán vào `Viewing Camera Pose`.
6. Điền `Prompt`, gán highlight vào `Focus Feedback Sources` nếu cần.
7. Chỉnh `Focus Blend` và hai delay theo nhịp mong muốn.

## Test

Chưa có EditMode test riêng cho `TVInteractable` (cần Cinemachine và VideoPlayer chạy thật).

Checklist play test:

- E vào TV → prompt và crosshair ẩn, camera blend mượt tới `ViewPose`, không đi lại hay xoay chuột được.
- Sau `Focus To Play Delay`, màn hình bật và video phát (kể cả lần đầu cần `Prepare`).
- Hết video → chờ `Video End To Unfocus Delay` → camera blend về, điều khiển được tiếp.
- `Escape` khi đang xem → dừng ngay, camera blend về.
- Tương tác lại TV ngay sau khi thoát → không lỗi, không bị kẹt context.
- Đổi scene khi đang xem → không còn context `Focus` treo, chuột không bị khoá sai.
- Tắt TV → `Off Screen Texture` hoặc texture/emission gốc được restore đúng.
