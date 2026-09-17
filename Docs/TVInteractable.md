# TV Interactable

Tài liệu cấu hình `TVInteractable` — object TV phát video, chiếm quyền camera trong lúc xem. Kế thừa `InteractableBehaviour` nên dùng chung raycast/input với [Interaction System](InteractionSystem.md).

## Luồng hoạt động

```text
PlayerInteractor.TryInteract
        │
        ▼
TVInteractable.PerformInteraction
        │
        ├─ SetFocused(false) + disable player controllers
        ├─ SetCameraDriverEnabled(false)   // tắt Cinemachine/driver camera cũ
        │
        ▼
   MoveCamera (DOTween) ──► camera tới Viewing Camera Pose
        │
        ▼
   focusToPlayDelay (nếu > 0)
        │
        ▼
   TurnScreenOn + VideoPlayer.Prepare/Play
        │
        ▼
   ... xem video ...
        │
        ├─ video hết (loopPointReached) ──► videoEndToUnfocusDelay ──► StopWatching
        └─ Escape / gamepad East / CancelWatching() ──► StopWatching ngay lập tức
                        │
                        ▼
              TurnScreenOff + MoveCamera (DOTween) về pose cũ
                        │
                        ▼
              SetCameraDriverEnabled(true) + enable lại player controllers
```

`StopWatchingImmediately` chạy trong `OnDisable` — snap camera về vị trí cũ ngay lập tức (không tween, không delay), dùng khi object bị disable/destroy giữa chừng.

## Camera transition

Camera được `DOTween` di chuyển bằng một `Sequence` join `DOMove` + `DORotateQuaternion`, chạy với `SetUpdate(true)` (unscaled time, không bị ảnh hưởng bởi pause/timescale).

| Field | Ý nghĩa |
|---|---|
| `Camera Transition Duration` | Thời lượng tween (giây). `0` = snap tức thì, không tween. |
| `Camera Transition Ease` | Ease áp cho cả move và rotate (mặc định `InOutSine`). |

Mỗi lần gọi `MoveCamera` sẽ `DOKill()` tween cũ trên camera trước khi tạo tween mới, tránh chồng tween khi player spam tương tác hoặc cancel giữa lúc đang di chuyển.

## Flow delay

| Field | Ý nghĩa |
|---|---:|
| `Focus To Play Delay` | Delay sau khi camera tới `Viewing Camera Pose`, trước khi `VideoPlayer.Play()`. |
| `Video End To Unfocus Delay` | Delay sau khi video phát xong, trước khi camera bắt đầu quay về. |

Cả hai mặc định `0` (không delay). Dùng `WaitForSecondsRealtime` nên không bị ảnh hưởng bởi `Time.timeScale`.

`Video End To Unfocus Delay` không áp dụng khi player cancel chủ động (Escape/gamepad/`CancelWatching()`) — trường hợp đó `StopWatching` chạy ngay.

## Cấu hình component

### TV Screen

| Field | Ý nghĩa |
|---|---|
| `Video Player` | Auto lấy từ `GetComponent<VideoPlayer>()` nếu để trống. |
| `Screen Renderer` | Renderer chứa material màn hình TV. |
| `Screen Material Index` | Slot material trên `Screen Renderer` (0-based). |
| `Screen Texture Property` | Tên property texture chính, mặc định `_BaseMap`. |
| `Video Render Texture` | RenderTexture cho video. Để trống thì component tự tạo (1920×1080, ARGB32) và tự `Release`/`Destroy` khi object bị destroy. |
| `Video Scale` / `Video Offset` | Tiling/offset áp cho texture khi phát video. |
| `Flip Video Horizontally` / `Flip Video Vertically` | Lật texture video. `Vertically` mặc định bật vì RenderTexture thường ngược trục Y so với video source. |
| `Off Screen Texture` | Texture hiển thị khi TV tắt. Để trống thì trả về texture gốc đã cache lúc `Awake`. |

### Screen Emission

Dùng khi shader màn hình cần tự phát sáng để video không bị tối do thiếu light trong scene.

| Field | Ý nghĩa |
|---|---|
| `Use Emission For Video` | Bật thì set luôn `Emission Map` + `Emission Color` khi video chạy. |
| `Emission Texture Property` | Mặc định `_EmissionMap`. |
| `Emission Color` | Màu/intensity emission (HDR) khi phát video. |

Component cache texture/scale/offset/color/keyword `_EMISSION` gốc lúc `Awake` và restore đúng trạng thái khi tắt TV, kể cả khi ban đầu emission đang tắt.

### Camera

| Field | Ý nghĩa |
|---|---|
| `Viewing Camera Pose` | Transform đặt trước TV, hướng thẳng vào màn hình. **Bắt buộc** — thiếu thì `CanInteract` luôn `false`. |
| `Camera Driver To Disable` | Component điều khiển camera hiện tại (ví dụ CinemachineCamera của `PlayerFollowCamera`). Bị disable trong lúc xem, restore đúng trạng thái enable trước đó khi xong. |
| `Camera Transition Duration` / `Camera Transition Ease` | Xem mục Camera transition. |
| `Player Controllers To Disable` | Các component như `FirstPersonController` cần disable trong lúc xem, restore đúng trạng thái cũ khi xong. |

### Cancel

| Field | Ý nghĩa |
|---|---|
| `Allow Gamepad Cancel` | Cho phép `buttonEast` (thường là B/Circle) hủy xem. Escape luôn hoạt động trên keyboard. |

Nối `CancelWatching()` vào một UI Button nếu game cần nút Cancel trên màn hình.

## Setup

1. Kéo prefab TV từ `Assets/X_ThirdParty/_Television_set/` (ví dụ `TV_00.prefab`) vào scene.
2. Add `TVInteractable` (tự thêm `VideoPlayer` do `RequireComponent`).
3. Gán `Screen Renderer` và `Screen Material Index` trỏ đúng material màn hình (asset mẫu: `1.mat`).
4. Gán `Video Player.clip` hoặc `url` là video cần phát (asset mẫu: `eFootball PES 2021 SEASON UPDATE 2026-08-30 23-55-12.mp4`).
5. Tạo một GameObject con rỗng đặt trước màn hình, xoay nhìn thẳng vào TV, gán vào `Viewing Camera Pose`.
6. Gán `Camera Driver To Disable` là CinemachineCamera đang điều khiển camera chính, và `Player Controllers To Disable` là các component input/movement của player.
7. Set layer object là `Interactable` để `PlayerInteractor` raycast trúng (xem [Interaction System](InteractionSystem.md#quy-ước-layertag)).
8. Tinh chỉnh `Camera Transition Duration/Ease` và hai field delay theo nhịp mong muốn.

## Test

Chưa có EditMode test riêng cho `TVInteractable`.

Checklist play test:

- Tương tác TV → camera cũ (driver + player controller) bị disable, camera tween mượt tới `Viewing Camera Pose` theo đúng ease.
- Sau `Focus To Play Delay`, màn hình bật và video bắt đầu phát (kể cả lần đầu cần `Prepare`).
- Video phát xong → chờ đúng `Video End To Unfocus Delay` → camera tween về vị trí cũ, driver/controller cũ được enable lại đúng trạng thái trước đó.
- Escape / gamepad East trong lúc đang xem → dừng ngay, không chờ `Video End To Unfocus Delay`.
- Disable/destroy object đang tween hoặc đang xem (ví dụ đổi scene) → camera snap về vị trí cũ ngay lập tức, không còn tween treo.
- Tương tác lại TV ngay sau khi vừa cancel (tween trả về camera cũ chưa xong) → không bị chồng tween, không bị giật vị trí.
- Tắt TV → `Off Screen Texture` (nếu có) hoặc texture/emission gốc được restore đúng scale/offset/color ban đầu.
