# Audio System

Tài liệu phần âm thanh của **The Shadow Wood**: service phát âm, cue, ambience, audio state theo đoạn truyện, bước chân, radio. Chưa dùng AudioMixer, âm lượng theo nhóm tính bằng code.

## Tổng quan

```text
RootLifetimeScope (sống suốt game)
└─ AudioService (IAudioService)   pool AudioSource dưới object "Audio" (DontDestroyOnLoad)
                                  fade, bám target, âm lượng theo bus, trả source khi phát xong

GameplayLifetimeScope
├─ broker TensionChangedMessage   (options của Root)
├─ GameplayAudioConfig (SO)       default + danh sách AudioState
├─ AmbiencePlayer                 phát một AmbienceProfile: loop 2D + spot 3D quanh người nghe
└─ AudioDirector                  FlagChangedMessage ──► chọn AudioState ──► đổi ambience + crossfade nhạc
                                                                       └──► TensionChangedMessage nếu tension đổi

Ai đang phát âm
├─ PlayerController       bước chân (HeadBob.Stepped ──► raycast mặt đất ──► PlayAt)
├─ FlashlightController   tiếng click khi bật/tắt
└─ RadioInteractable      tiếng radio 3D gắn vào object, tiếng click
```

Mọi âm thanh đi qua `IAudioService`. Không đặt `AudioSource` tay trong scene, trừ tiếng TV (vẫn ra từ `VideoPlayer`).

## File chính

| File | Assembly | Vai trò |
|---|---|---|
| `Core/Audio/AudioBus.cs` | Core | `Sfx`, `Ambience`, `Music`, `Ui` |
| `Core/Audio/AudioCue.cs` | Core | Asset mô tả một âm: clip, bus, biến thiên volume/pitch, loop, 2D/3D |
| `Core/Audio/AudioHandle.cs` | Core | Id của một lần phát, dùng để dừng hoặc hỏi còn phát không |
| `Core/Audio/IAudioService.cs` | Core | `Play`, `PlayAt`, `PlayAttached`, `IsPlaying`, `Stop` |
| `Core/Audio/TensionStage.cs`, `TensionChangedMessage.cs` | Core | `Calm`, `Unease`, `Tension`, `Climax` và message `(Previous, Current)` |
| `Audio/AudioService.cs` | Audio | Cài `IAudioService`, pool tối đa 32 source |
| `Audio/CueClipPicker.cs` | Audio | Chọn clip ngẫu nhiên, không lặp clip vừa phát nếu bật `Avoid Repeat` |
| `Audio/AmbienceProfile.cs` | Audio | Asset một bộ ambience: `Loops[]`, `Spots[]` |
| `Audio/AmbienceScheduler.cs` | Audio | C# thuần: đếm giờ từng spot, tính vị trí ngẫu nhiên |
| `Audio/AmbiencePlayer.cs` | Audio | Phát profile hiện tại, `SetProfile(profile, crossfade)` |
| `Audio/AudioState.cs` | Audio | Một dòng state: tên, flag, ambience, nhạc, tension, crossfade |
| `Audio/AudioStateResolver.cs` | Audio | C# thuần: chọn state đang thắng |
| `Audio/AudioDirector.cs` | Audio | Nơi duy nhất quyết định ambience và nhạc đang phát |
| `Audio/GameplayAudioConfig.cs` | Audio | Asset default + `States[]` |
| `Player/Footsteps/FootstepConfig.cs` | Player | Cue mặc định, bảng `PhysicsMaterial → cue`, volume theo dáng đi |
| `Interaction/RadioInteractable.cs` | Interaction | Radio bật/tắt bằng interact |

Asset (trong `Assets/_Project/Data/`):

| Asset | Nội dung |
|---|---|
| `Audio/Cues/Rain`, `Frog` | Ambience mưa (loop 2D) và ếch (one-shot 3D). Đã có clip |
| `Audio/Cues/Footstep_Default` | Bước chân mặc định. Đã có clip |
| `Audio/Cues/Music_Tension`, `Music_Climax` | Nhạc loop 2D bus Music, chưa có clip |
| `Audio/Cues/Radio_Broadcast`, `Radio_Switch` | Tiếng radio (loop 3D, nghe tới 12 m) và tiếng click |
| `Audio/Cues/Flashlight_Switch` | Tiếng click đèn pin |
| `Audio/Ambience/Ambience_Calm` | Mưa + ếch dày (3–9 s, cách 6–15 m) |
| `Audio/Ambience/Ambience_Unease` | Mưa + ếch thưa (8–16 s, cách 10–20 m) |
| `Audio/Ambience/Ambience_Climax` | Chỉ còn mưa |
| `Audio/GameplayAudioConfig` | Default = `Ambience_Calm`, không nhạc. 3 state mẫu, xem [Audio state](#audio-state-theo-đoạn-truyện) |
| `Player/FootstepConfig` | Default = `Footstep_Default`, chưa map bề mặt nào |
| `Flags/TensionUnease`, `TensionHigh`, `TensionClimax` | Flag của 3 state mẫu |

Clip gốc đặt trong `Assets/_Project/Audio/` (`Ambience/`, `SFX/`, `Music/`, `Voiceovers/`).

## Đăng ký trong scope

```csharp
// RootLifetimeScope.Configure
builder.RegisterEntryPoint<AudioService>();

// GameplayLifetimeScope.Configure
builder.RegisterInstance(audioConfig);                        // field "Audio Config", bắt buộc gán
builder.RegisterMessageBroker<TensionChangedMessage>(options);
builder.RegisterEntryPoint<AmbiencePlayer>().AsSelf();
builder.RegisterEntryPoint<AudioDirector>();
```

- `AudioService` ở Root vì nhạc và âm UI có thể kéo dài qua lúc chuyển scene. Object `Audio` chỉ được tạo lần đầu có âm phát, nên test build container Root không cần scene.
- `GameplayLifetimeScope` thiếu `Audio Config` thì ném `InvalidOperationException` ngay khi vào scene.
- `AmbiencePlayer` lấy vị trí người nghe từ `Camera` của `CameraRig`.

## Audio cue

Tạo cue: **Create → The Shadow Wood → Audio → Audio Cue**, đặt trong `Data/Audio/Cues/`, kéo clip vào `Clips`.

| Field | Ý nghĩa |
|---|---|
| `Clips` | Một hoặc nhiều clip. Mỗi lần phát chọn ngẫu nhiên một clip |
| `Bus` | Quyết định slider nào điều khiển âm này, xem [Bus và âm lượng](#bus-và-âm-lượng) |
| `Min/Max Volume` | Volume ngẫu nhiên trong khoảng này mỗi lần phát. Hai số bằng nhau thì cố định |
| `Min/Max Pitch` | Pitch ngẫu nhiên, giúp tiếng lặp lại (bước chân, ếch) đỡ giống nhau |
| `Avoid Repeat` | Không chọn lại clip vừa phát. Chỉ có tác dụng khi có từ 2 clip |
| `Loop` | Phát lặp tới khi `Stop`. One-shot tự trả source khi phát xong |
| `Spatial Blend` | 0 = 2D (nghe như trong đầu: nhạc, mưa nền), 1 = 3D (có vị trí, to nhỏ theo khoảng cách) |
| `Min/Max Distance`, `Rolloff` | Chỉ cho âm 3D: trong `Min` nghe to nhất, `Max` là xa nhất. `Linear` thì tắt hẳn ở `Max` |

- **Cue không có clip là hợp lệ:** phát ra handle không hợp lệ, không lỗi, không log. Cứ tạo cue và nối vào code/scene trước, có clip thì chỉ cần kéo vào asset.
- Phát cue `null` cũng trả handle không hợp lệ.
- Volume tổng của một source: `volume ngẫu nhiên của cue × volumeScale × fade × slider của bus × Master`.

## Phát âm từ code

```csharp
public sealed class DoorSounds
{
    private readonly IAudioService _audio;
    private AudioHandle _creak;

    public void OnOpen(Transform door)
    {
        _audio.PlayAt(_slam, door.position);                         // one-shot 3D tại một điểm
        _creak = _audio.PlayAttached(_creakLoop, door, fadeIn: 0.5f); // loop 3D bám theo object
    }

    public void OnClosed()
    {
        _audio.Stop(_creak, fadeOut: 0.3f);
    }
}
```

| Hàm | Dùng khi |
|---|---|
| `Play(cue, volumeScale, fadeIn)` | Âm không có vị trí: nhạc, UI, click của đèn pin. Chỉ dùng với cue 2D (`Spatial Blend` 0): cue 3D sẽ kêu từ gốc toạ độ (0, 0, 0) |
| `PlayAt(cue, position, …)` | Âm 3D tại một điểm cố định: bước chân, đồ rơi |
| `PlayAttached(cue, target, …)` | Âm 3D đi theo object: radio, quái vật |
| `IsPlaying(handle)` | Còn phát không. One-shot phát xong thì trả `false` |
| `Stop(handle, fadeOut)` | Dừng, có thể fade. Handle cũ hoặc không hợp lệ thì bỏ qua |

- Handle không bao giờ bị dùng lại, nên giữ handle cũ rồi gọi `Stop` không dừng nhầm âm khác đang dùng lại source đó.
- Ai phát loop thì phải tự dừng (thường trong `Dispose()`/`OnDisable()`). `AudioService` sống ở Root, loop không ai dừng sẽ kêu sang scene sau.
- **Pool:** tối đa 32 source, tạo dần khi cần. Hết chỗ thì cướp one-shot phát lâu nhất; không bao giờ cướp loop. Cả 32 đều là loop thì log warning và trả handle không hợp lệ.

## Bus và âm lượng

| Bus | Slider | Bỏ qua pause | Dùng cho |
|---|---|---|---|
| `Music` | Music | Không | Nhạc nền |
| `Sfx` | SFX | Không | Bước chân, đồ vật, radio, đèn pin |
| `Ambience` | SFX | Không | Mưa, ếch, gió |
| `Ui` | SFX | **Có** | Âm nút bấm (vẫn kêu khi đang pause) |

- `Master` đặt thẳng vào `AudioListener.volume`. `Music` và `SFX` nhân vào từng source; kéo slider thì mọi âm đang phát đổi ngay. Chi tiết settings: [UISystem.md](UISystem.md#settings).
- **Pause:** `GamePause` đặt `AudioListener.pause`, mọi bus trừ `Ui` đứng yên. Fade cũng dừng theo, Resume thì chạy tiếp từ chỗ dừng.
- Chưa có AudioMixer nên chưa có lowpass/reverb. Sẽ thêm ở `feat/polish-qa`: chỉ cần map mỗi bus vào một mixer group.

## Ambience

Tạo profile: **Create → The Shadow Wood → Audio → Ambience Profile**, đặt trong `Data/Audio/Ambience/`.

| Phần | Ý nghĩa |
|---|---|
| `Loops[]` | Cue loop phát liên tục suốt lúc profile được chọn (mưa, gió). Nên để 2D (`Spatial Blend` 0) |
| `Spots[]` | Cue one-shot phát ngẫu nhiên quanh người nghe (ếch, cành gãy). Nên để 3D |

Mỗi spot:

| Field | Ý nghĩa |
|---|---|
| `Cue` | Cue one-shot |
| `Min/Max Interval` | Giây nghỉ giữa hai lần phát, ngẫu nhiên trong khoảng |
| `Min/Max Distance` | Khoảng cách ngang tới người nghe |
| `Min/Max Height` | Độ cao so với camera. Mặc định −1.6..0 là từ mặt đất tới ngang tai |

- Vị trí spot chọn **lúc bắt đầu phát** rồi giữ nguyên. Người chơi đi tiếp thì tiếng ở lại phía sau, như con ếch thật.
- Spot đếm giờ theo thời gian game (`Time.deltaTime`), nên pause thì không có spot mới.
- Đổi profile: loop cũ fade out, loop mới fade in cùng lúc. Chọn lại đúng profile đang phát thì không có gì xảy ra, mưa không bị phát lại từ đầu.
- Không gọi `AmbiencePlayer.SetProfile` từ chỗ khác. Đổi ambience bằng cách thêm [audio state](#audio-state-theo-đoạn-truyện).

## Audio state theo đoạn truyện

Ambience và nhạc đổi theo **đoạn truyện** (đã bật TV, mất điện, bị săn đuổi...), không đặt trực tiếp từ code gameplay. Gameplay chỉ set flag; `AudioDirector` tự chọn âm thanh.

`GameplayAudioConfig`:

| Phần | Ý nghĩa |
|---|---|
| `Default Ambience`, `Default Music` | Phát khi không state nào bật. Trống = im |
| `Default Crossfade` | Giây crossfade khi quay về default |
| `Start Fade In` | Giây fade in lúc vào scene |
| `States[]` | Danh sách state, **xếp theo thứ tự truyện** |

Mỗi state:

| Field | Ý nghĩa |
|---|---|
| `Name` | Chỉ để đọc trong Inspector |
| `Enter Flag` | Flag bật state này. Thiếu flag thì state không bao giờ chạy, Inspector cảnh báo |
| `Ambience` | Profile. Trống = không ambience |
| `Music` | Cue nhạc. Trống = không nhạc |
| `Tension` | Mức căng của đoạn này, cho hệ thống khác đọc qua `TensionChangedMessage` |
| `Crossfade` | Giây crossfade khi **vào** state này |

**State nào thắng:** duyệt list từ dưới lên, state đầu tiên có flag đang bật thắng; không có thì dùng default. Flag đoạn cũ vẫn bật cũng không sao, vì state nằm dưới đè lên state nằm trên.

| Flag đang bật (3 state mẫu) | State |
|---|---|
| không có | Default: `Ambience_Calm`, không nhạc |
| `TensionUnease` | Unease: `Ambience_Unease`, không nhạc |
| `TensionUnease` + `TensionHigh` | Tension: `Ambience_Unease` + `Music_Tension` |
| thêm `TensionClimax` | Climax: `Ambience_Climax` + `Music_Climax` |
| Clear `TensionClimax` | Quay về Tension |

Khi state đổi:

- Ambience crossfade sang profile mới. Cùng profile thì giữ nguyên.
- Nhạc cũ fade out, nhạc mới fade in. **Cùng cue nhạc thì chạy tiếp**, không về đầu bài.
- Chỉ publish `TensionChangedMessage` khi `Tension` đổi. Hai state khác âm thanh nhưng cùng mức tension thì không publish.
- Lúc vào scene áp state hiện tại với `Start Fade In`, không publish. Rời scene thì nhạc fade out 1 s.

Thêm một đoạn truyện mới:

1. Tạo flag trong `Data/Flags/` (ví dụ `TvTurnedOn`), ghi `Note`.
2. Tạo profile ambience và cue nhạc nếu cần, hoặc dùng lại cái có sẵn.
3. Thêm một dòng vào `States[]` của `GameplayAudioConfig`, đặt đúng vị trí theo thứ tự truyện.
4. Ở chỗ xảy ra sự kiện (sequence, trigger, interactable), gọi `IFlagService.Set(flag)`.

Không cần sửa code. Tiếng một lần (đập cửa, stinger) thì phát thẳng bằng `IAudioService`, không làm state.

Nghe `TensionChangedMessage`:

```csharp
_subscription = _tensionChanged.Subscribe(message =>
{
    _flicker.SetIntensity(message.Current >= TensionStage.Tension ? 1f : 0f);
});
```

Hiện chưa có ai nghe. Message không bắn lúc vào scene và chưa có hàm hỏi tension hiện tại, nên bên nghe chỉ biết khi có thay đổi. Sẽ thêm cách đọc stage hiện tại khi có bên cần (AudioMixer ở `feat/polish-qa`).

## Bước chân

Mỗi lần nhịp nhún camera (`HeadBob`) đi qua điểm thấp nhất là một bước, nên tiếng chân luôn khớp với cái nhún. Sprint và cúi tự đổi nhịp theo `Walk/Sprint/Crouch Frequency` trong `PlayerMovementConfig`. Đứng yên hoặc đang trên không thì không có bước.

Mỗi bước: raycast từ chân xuống, lấy `PhysicsMaterial` của collider trúng, chọn cue theo bảng, phát 3D tại điểm chạm.

`FootstepConfig` (gán vào `PlayerView` trên `Player.prefab`):

| Field | Mặc định | Ý nghĩa |
|---|---|---|
| `Default Cue` | `Footstep_Default` | Dùng khi bề mặt không có trong bảng hoặc collider không có material |
| `Surfaces[]` | trống | Cặp `Material` → `Cue` |
| `Crouch/Walk/Sprint Volume` | 0.35 / 0.7 / 1 | Nhân vào volume của cue |
| `Ground Mask` | Everything | Layer được tính là mặt đất. Player không tự trúng chính mình |
| `Ray Length` | 0.4 m | Độ dài tia dò xuống dưới chân |

Thêm một loại bề mặt (ví dụ sàn gỗ):

1. **Create → Physics Material**, đặt tên `Wood`.
2. Gán material đó vào ô `Material` của collider sàn gỗ (nhiều collider dùng chung một material).
3. Tạo cue `Footstep_Wood` (bus `Sfx`, 3D, vài clip, pitch 0.9–1.1).
4. Thêm dòng `Wood → Footstep_Wood` vào `Surfaces` của `FootstepConfig`.

Không cần thêm component vào level. Sàn gỗ và material sẽ làm ở `feat/level-house`.

## Radio

`RadioInteractable` (kế thừa `InteractableBehaviour`): interact để bật/tắt, tiếng phát ra từ chính radio.

| Field | Ý nghĩa |
|---|---|
| `Broadcast` | Tiếng radio. Loop thì phát tới khi tắt; one-shot (bản tin) thì phát hết tự về trạng thái tắt |
| `Switch Cue` | Tiếng click mỗi lần bật/tắt. Tuỳ chọn |
| `Speaker` | Chỗ phát ra tiếng. Trống = chính object |
| `Fade` | Giây fade khi bật/tắt, mặc định 0.3 |
| `On Prompt` / `Off Prompt` | `Turn on` / `Turn off`, prompt đổi ngay khi bấm |
| `Play On Start` | Đang bật sẵn khi vào scene |
| `Set On First Turn On` | Flag set ở lần đầu người chơi bật. Dùng để nối radio với đoạn truyện |

Đặt radio:

1. Đặt model dưới root `Interactables` để được inject. Ngoài root đó thì không có prompt, không bấm được.
2. Model cần Collider. Nên để layer `Interactable`.
3. Thêm `RadioInteractable`, gán `Broadcast` (và `Switch Cue` nếu có).
4. Highlight: thêm `RendererMaterialInteractionHighlight`, rồi kéo **component highlight đó** (không phải `RadioInteractable`) vào `Focus Feedback Sources`.

`Broadcast` chưa có clip thì interact bị từ chối, không set flag. Tắt hoặc huỷ object thì tiếng dừng.

## Đèn pin

`FlashlightView` có field `Switch Cue` (đang gán `Flashlight_Switch`), phát 2D mỗi lần bật/tắt. Để trống thì không có tiếng. Phần còn lại của đèn pin: [FlagsInventory.md](FlagsInventory.md#đèn-pin).

## Kiểm thử

**EditMode:**

| Assembly | File | Nội dung |
|---|---|---|
| `TheShadowWood.Audio.Tests` | `Audio/CueClipPickerTests.cs` | Chọn clip, không lặp clip vừa phát, khoảng ngẫu nhiên |
| `TheShadowWood.Audio.Tests` | `Audio/AudioServiceTests.cs` | Cue rỗng/null không lỗi, volume = cue × scale × bus, đổi settings áp lại âm đang phát, handle cũ không dừng nhầm, hết pool cướp one-shot cũ nhất, bus Ui bỏ qua pause |
| `TheShadowWood.Audio.Tests` | `Audio/AmbienceSchedulerTests.cs` | Khoảng nghỉ, vị trí spot nằm trong khoảng cách/độ cao |
| `TheShadowWood.Audio.Tests` | `Audio/AmbiencePlayerTests.cs` | Phát loop với fade, cùng profile không phát lại, đổi profile crossfade, spot quanh người nghe, profile null và `Dispose` dừng hết |
| `TheShadowWood.Audio.Tests` | `Audio/AudioStateResolverTests.cs` | Không flag thì default, state dưới cùng thắng, Clear quay về state trước, bỏ qua state thiếu flag |
| `TheShadowWood.Audio.Tests` | `Audio/AudioDirectorTests.cs` | Áp state lúc Start, flag có sẵn, crossfade ambience + nhạc, cùng profile/nhạc không phát lại, cùng tension không publish, Clear về state trước/default, `Dispose` dừng nhạc và huỷ subscribe |
| `TheShadowWood.Core.Tests` | `Core/GameSettingsTests.cs` | `MusicVolume`/`SfxVolume`: mặc định, clamp, lưu |
| `TheShadowWood.Bootstrap.Tests` | `Bootstrap/LifetimeScopeTests.cs` | Root resolve được `IAudioService` |
| `TheShadowWood.Player.Tests` | `Player/HeadBobTests.cs` | Số bước theo nhịp, sprint nhiều bước hơn, đứng yên/trên không không có bước |
| `TheShadowWood.Player.Tests` | `Player/FootstepConfigTests.cs` | Chọn cue theo material, fallback default, volume crouch < walk < sprint |
| `TheShadowWood.Interaction.Tests` | `Interaction/RadioInteractableTests.cs` | Bật/tắt, prompt, click, flag chỉ set một lần, phát hết tự tắt, cue rỗng bị từ chối, thiếu inject bị chặn |

`Audio.Tests` dùng `FakeAudioService` và `TestBroker` (broker MessagePipe đồng bộ, ghi lại message). `Interaction.Tests` có `FakeAudioService` riêng.

**Play Mode** (scene `Gameplay`):

| Test | Kết quả mong đợi |
|---|---|
| Vào Gameplay | Mưa fade in, ếch kêu rải rác quanh người, mỗi con một hướng |
| Đi bộ / chạy / cúi | Tiếng chân khớp nhịp nhún, chạy nhanh và to hơn, cúi nhỏ hơn |
| Đứng yên, nhảy xuống | Không có tiếng chân khi đứng yên hoặc đang rơi |
| Esc pause | Mọi âm đứng yên, Resume phát tiếp |
| Kéo slider Music/SFX trong Pause | Âm đang phát đổi ngay |
| Bật/tắt radio | Fade ngắn, prompt đổi `Turn on`/`Turn off`, đi vòng quanh nghe đổi hướng, xa 12 m thì im |
| Gán `TensionUnease` vào `Set On First Turn On` rồi bật radio | Ếch thưa dần (state Unease) |
| Nhặt đèn pin, bấm `F` | Có tiếng click |
| Về Menu | Mưa và nhạc fade out, không kêu sang Menu |

## Lỗi thường gặp

| Triệu chứng | Nguyên nhân / cách xử lý |
|---|---|
| Không có tiếng gì, không lỗi | Cue chưa có clip (hợp lệ, phát ra im). Kéo clip vào cue. Kiểm tra cả slider Master/SFX/Music trong Settings |
| `InvalidOperationException: Audio Config is not assigned` | `GameplayLifetimeScope` trong scene chưa gán `Audio Config` |
| Âm 3D nghe như phát trong đầu | `Spatial Blend` của cue đang là 0 |
| Âm phát ra từ một góc map, không phải chỗ đáng lẽ | Cue 3D được phát bằng `Play` (không vị trí). Dùng `PlayAt`/`PlayAttached`, hoặc đổi cue sang 2D |
| Âm 3D nghe được ở rất xa | `Rolloff` là `Logarithmic` không tắt hẳn ở `Max Distance`. Dùng `Linear` |
| Mưa bị phát lại từ đầu khi đổi state | Hai state dùng hai profile khác nhau nên crossfade. Muốn liền mạch thì dùng chung profile |
| Set flag mà ambience không đổi | Flag không gắn vào state nào, hoặc có state nằm dưới đang bật đè lên. Xem thứ tự `States[]` |
| `Audio state '…' has no enter flag` | Dòng state chưa gán flag, sẽ không bao giờ chạy |
| `AudioService: no free source …` | 32 source đều đang phát loop. Có loop không được `Stop` |
| Loop vẫn kêu sau khi về Menu | Class phát loop không dừng nó trong `Dispose()`/`OnDisable()` |
| Bước chân luôn dùng tiếng mặc định | Collider sàn chưa gán `Physics Material`, hoặc material đó chưa có trong `Surfaces`, hoặc layer sàn không nằm trong `Ground Mask` |
| `RadioInteractable on Radio does not implement IInteractionFocusFeedback` | Kéo nhầm `RadioInteractable` vào `Focus Feedback Sources`. Kéo component highlight vào |
| Nhìn radio không có prompt | Radio nằm ngoài root `Interactables` nên không được inject, hoặc `Broadcast` chưa gán |
| Tiếng TV không theo slider | TV vẫn phát qua `VideoPlayer`, chưa qua `AudioService`. Sửa ở `feat/polish-qa` |

## Việc còn lại

- **Ambience zone** (`feat/level-house`): vào nhà thì mưa nhỏ lại thay vì đổi bộ ambience. Cần cờ `Outdoor` cho từng loop trong profile, `IAudioService.SetVolume(handle, volume, fade)` và component `AmbienceZone` (trigger box). Âm của chính ngôi nhà (đồng hồ, tủ lạnh) đặt thành nguồn 3D trong level.
- **AudioMixer** (`feat/polish-qa`): map bus → group, snapshot lowpass/reverb theo tension, mưa nghe đục khi ở trong nhà, tiếng TV đi qua AudioSource.
- Chưa có nhạc Menu, voiceover, tiếng cửa (`feat/doors-readables`), action `PlaySfx` cho sequence (`feat/scripted-sequence`).
