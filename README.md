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
5. Mở `Assets/Scenes/SampleScene.unity`, rồi nhấn **Play**.

`SampleScene` hiện là scene duy nhất được bật trong **File → Build Profiles / Build Settings**. Khi thêm scene mới vào game build, hãy thêm nó vào danh sách này và kiểm tra thứ tự tải scene.

## Điều khiển

Input dùng action map `Player` tại `Assets/X_ThirdParty/StarterAssets/InputSystem/StarterAssets.inputactions`.

| Hành động | Bàn phím/chuột | Gamepad |
| --- | --- | --- |
| Di chuyển | `W` `A` `S` `D` hoặc phím mũi tên | Left Stick |
| Nhìn | Chuột | Right Stick |
| Nhảy | `Space` | South button |
| Chạy | Giữ `Left Shift` | Left Trigger |
| Cúi | `Left Ctrl` | Chưa có binding |
| Tương tác | `E` | North button |

> Khi thay đổi input action, hãy lưu asset và kiểm tra `PlayerInput` trên Player vẫn dùng action map `Player` với behavior `Send Messages`.

## Cấu trúc project

```text
Assets/
├── _Project/                         # Nội dung do team phát triển
│   ├── Animations/                    # Animation nhân vật và môi trường
│   ├── Audio/                         # Ambience, music, SFX, voiceover
│   ├── Data/                          # Dữ liệu game
│   ├── Materials/                     # Material nội bộ
│   ├── Models/                        # Model nhân vật, môi trường và prop
│   ├── Prefabs/                       # Prefab character, environment, system, interactable
│   ├── Scenes/                        # Scene nội bộ theo chapter/hệ thống
│   ├── Scripts/                       # C# theo từng domain
│   │   ├── Core/                      # Thành phần nền tảng
│   │   ├── Interaction/               # Raycast, focus và interactable
│   │   ├── Inventory/                 # Pickup contract
│   │   ├── Player/                    # Stamina, head bob
│   │   ├── UI/                        # Crosshair và stamina UI
│   │   └── Audio/, Enemy/, Environment/, Puzzles/, SaveSystem/, Attributes/
│   ├── Tests/EditMode/                # EditMode test
│   └── UI/                            # Font, icon, texture UI
├── Plugins/Demigiant/DOTween/         # DOTween
├── X_ThirdParty/StarterAssets/        # Unity Starter Assets (FPS controller)
├── Scenes/SampleScene.unity           # Scene mẫu đang được build
└── Settings/                          # URP asset và renderer cho PC/Mobile
Docs/
├── InteractionSystem.md               # Tài liệu interaction
└── CharacterController.md              # Tài liệu controller, stamina, head bob
```

Không sửa trực tiếp `Library/`, `Temp/`, `Logs/` hoặc các file `.csproj`: chúng do Unity/IDE sinh tự động và đã được `.gitignore` loại trừ.

## Các hệ thống hiện có

### Player controller

Project dùng **Starter Assets – First Person Controller**. Các prefab nền tảng:

- `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab`
- `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/PlayerFollowCamera.prefab`
- `Assets/X_ThirdParty/StarterAssets/FirstPersonController/Prefabs/MainCamera.prefab`

Khi tạo scene mới, dùng ba prefab này để có `CharacterController`, `PlayerInput`, Cinemachine camera và input cơ bản. Scene chỉ nên có một camera gắn tag `MainCamera` và một `AudioListener` đang bật.

### Stamina và head bob

- `PlayerStamina` giảm stamina khi người chơi vừa di chuyển vừa sprint; mặc định: 100 stamina, tiêu hao 20/giây, hồi 15/giây sau 1,5 giây chờ.
- `PlayerHeadBob` tạo camera bob khi di chuyển trên mặt đất; cần gán `Camera Target`.
- `StaminaUIHandler` nhận `PlayerStamina`, `Image` fill và `CanvasGroup` để hiển thị thanh stamina.

Các component nằm trong `Assets/_Project/Scripts/Player/` và `Assets/_Project/Scripts/UI/`. Khi thêm chúng vào Player prefab/scene, hãy gán đủ reference trong Inspector trước khi play-test.

### Interaction và pickup

Luồng tương tác: camera raycast từ giữa màn hình → `PlayerInteractor` chọn collider gần nhất không bị che → `InteractableBehaviour` xử lý hành động → UI cập nhật focus.

| Thành phần | Vai trò |
| --- | --- |
| `PlayerInteractor` | Đặt cùng GameObject với `PlayerInput`; tự lấy `MainCamera` nếu chưa gán. Khoảng cách mặc định 3 m. |
| `InteractableBehaviour` | Base class cho mọi đối tượng tương tác. |
| `DestroyOnInteract` | Object dùng một lần; tắt collider rồi bị destroy khi tương tác thành công. |
| `PickupItem` | Chỉ hủy world item khi receiver trên player chấp nhận item. |
| `PickupReceiverBehaviour` | Contract để inventory quyết định nhận hoặc từ chối item. |
| `InteractionCrosshairUI` | Crosshair phản hồi target đang focus. |

Hướng dẫn cấu hình chi tiết, outline và test case: [Docs/InteractionSystem.md](Docs/InteractionSystem.md).

### Assembly definition

Mã nguồn chia assembly để kiểm soát dependency và rút ngắn thời gian compile. Khi thêm script, đặt nó vào đúng domain và không tạo dependency ngược:

```text
Core ───────► Interaction ───────► UI
                  │
                  └──────────────► Inventory

Player ──────────────────────────► UI
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

1. Thêm `PickupItem` vào world item.
2. Điền `Item Id` và `Quantity`.
3. Cài component kế thừa `PickupReceiverBehaviour` trên player.
4. Play-test cả trường hợp inventory chấp nhận và từ chối: item chỉ bị destroy khi nhận thành công.

```csharp
public sealed class PlayerInventory : PickupReceiverBehaviour
{
    public override bool TryReceive(PickupRequest request)
    {
        return TryAdd(request.ItemId, request.Quantity);
    }
}
```

### Scene mới

1. Tạo scene trong `Assets/_Project/Scenes/` theo nhóm chức năng/chapter phù hợp.
2. Thêm environment, player, follow camera, main camera và EventSystem cần thiết.
3. Kiểm tra `MainCamera`, `AudioListener`, `PlayerInput`, layer collision và các reference UI.
4. Lưu scene, rồi thêm scene vào Build Settings nếu scene phải xuất hiện trong bản build.
5. Test từ một session Unity sạch để phát hiện reference thiếu hoặc asset chưa commit.

## Kiểm thử

EditMode test cho interaction: `Assets/_Project/Tests/EditMode/PlayerInteractorTests.cs`.

Chạy qua **Window → General → Test Runner → EditMode → Run All**. Test hiện bao phủ:

- Focus object tương tác ở tâm màn hình.
- Không tương tác xuyên qua collider che khuất.
- Gọi action tương tác đúng một lần.

Trước khi tạo pull request, tối thiểu: Console không có compile error, EditMode tests pass, mở `SampleScene` và play-test di chuyển, nhảy, sprint, cúi, stamina và tương tác.

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
| Pickup không biến mất | Kiểm tra player có subclass `PickupReceiverBehaviour` và `TryReceive` trả `true`. |
| Không nhận phím tương tác | Kiểm tra `PlayerInput` dùng map `Player`, behavior `Send Messages`, và `PlayerInteractor` cùng GameObject. |
| Hai camera cùng nghe âm thanh | Tắt hoặc xóa `AudioListener` thừa; chỉ để một listener enabled. |

---

Tài liệu theo hệ thống: [Interaction System](Docs/InteractionSystem.md) · [Character Controller](Docs/CharacterController.md)
