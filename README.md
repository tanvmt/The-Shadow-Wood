# The Shadow Wood

Dự án game kinh dị sinh tồn góc nhìn thứ nhất (FPS Survival Horror).

## Cấu trúc thư mục (Folder Structure)

```text
Assets/
├── _Project/                     # Toàn bộ mã nguồn và tài nguyên do đội ngũ tự làm (để phân biệt với Asset Store)
│   ├── Animations/               # Chứa các Animation Controller và Clips
│   │   ├── Characters/           # Animation cho nhân vật
│   │   │   ├── Long/             # Tay góc nhìn thứ nhất, lúc trốn, lúc cầm gậy...
│   │   │   ├── BaySuong/         # Lúc đi cộc cộc, lúc biến thái bò 4 chân...
│   │   │   └── MyHoa/            # Oán linh cô đào hát bay lượn, tấn công...
│   │   └── Environment/          # Animation cho môi trường (Cửa mở cọt kẹt, rễ cây co quắp...)
│   │
│   ├── Audio/                    # Hệ thống âm thanh (Spatial Audio đóng vai trò cốt lõi)
│   │   ├── Ambience/             # Âm thanh môi trường (Tiếng mưa rừng Gia Lai, gió rít qua khung sắt...)
│   │   ├── SFX/                  # Hiệu ứng âm thanh ngắn (Tiếng gậy "cộc cộc", tiếng bước chân, tiếng xẻng đào...)
│   │   ├── Music/                # Nhạc nền, tiếng hát cải lương/vọng cổ thê lương của Mỹ Hoa
│   │   └── Voiceovers/           # Thoại nhân vật (Tiếng nấc Gia An, lời thì thầm của mẹ Phương, thoại Long...)
│   │
│   ├── Materials/                # Vật liệu tổng hợp (Vỏ cây cổ thụ, bùn đất nhão, đinh đồng rỉ sét...)
│   ├── Models/                   # Mô hình 3D (FBX/OBJ) và Textures đi kèm
│   │   ├── Characters/           # Model nhân vật (Long, Lão Oanh, Mỹ Hoa, Bé Gia An, Ông Tuấn)
│   │   ├── Environment/          # Kiến trúc môi trường (Cổng soát vé, nhà 1 trệt 2 lầu, văn phòng kế toán...)
│   │   └── Props/                # Đồ vật tương tác (Gậy Chiên Đàn, Lồng Chim, 10 hũ cốt, Sợi dây chuyền, Xẻng...)
│   │
│   ├── Prefabs/                  # Các đối tượng được đóng gói sẵn để tái sử dụng
│   │   ├── Characters/           # Prefab AI Bảy Sương, Mỹ Hoa, Player (Long) kèm Camera
│   │   ├── Environment/          # Các block nhà, cây cối, tủ trốn ẩn nấp (Hiding Spot)
│   │   ├── Interactables/        # Vật phẩm nhặt được (Key Items, Sổ nợ, Bật lửa, Đĩa văn tế...)
│   │   └── System/               # GameManager, UIManager, SaveSystem, AudioManager prefabs
│   │
│   ├── Presets/                  # Các file thiết kế cấu hình sẵn (Cài đặt Volume, Post-Processing...)
│   │
│   ├── Profiles/                 # Post-Processing Profiles (Đặc biệt cho cơ chế Mắt Âm Dương tông xám/đỏ)
│   │
│   ├── Scenes/                   # Quản lý các màn chơi và luồng vận hành game
│   │   ├── System/               # Menu chính (MainMenu), Màn hình Loading, Màn hình Game Over
│   │   ├── Chapter1_Intro/       # Phân đoạn 1: Trên chuyến xe bus đêm & Cổng công viên (Tutorial)
│   │   ├── Chapter1_Park/        # Phân đoạn 2 & 5: Khu công viên dạo, Hồ Sen, Vườn thực vật (Map mở)
│   │   ├── Chapter1_Office/      # Phân đoạn 3: Khu văn phòng đóng (Map stealth trốn tìm với Lão Oanh)
│   │   ├── Chapter1_Cemetery/    # Phân đoạn 4: Khu Nhà mồ cổ dòng họ Hồ (Hành Thổ)
│   │   └── Chapter1_Climax/      # Trận chiến cuối tại cây cổ thụ đại thụ
│   │
│   ├── Scripts/                  # Toàn bộ mã nguồn C# của trò chơi (Chia nhóm logic rõ ràng)
│   │   ├── Attributes/           # Custom Attributes cho Editor
│   │   ├── Audio/                # Điều khiển âm thanh (Spatial Audio, Volume Trigger, Nhiễu sóng Radio)
│   │   ├── Core/                 # GameManager, SceneLoader, GameLoop (Theo chuỗi Khám phá -> Trốn thoát)
│   │   ├── Enemy/                # AI của Lão Oanh/Mỹ Hoa (StateMachine: Idle, Patrol, Investigate, Chase, Attack)
│   │   ├── Environment/          # Logic cửa, tủ ẩn nấp (Hiding), bẫy rễ cây co quắp
│   │   ├── Interaction/          # Interface `IInteractable`, Raycast từ mắt người chơi để tương tác đồ vật
│   │   ├── Inventory/            # Hệ thống 8 Slots túi đồ, logic kết hợp vật phẩm (Gậy + Lưỡi xẻng)
│   │   ├── Player/               # PlayerController, Sprint, Crouch, Stamina, Hệ thống Sanity & HP
│   │   ├── Puzzles/              # Logic câu đố Ngũ Hành Nghịch (Mộc, Thủy, Kim, Hỏa, Thổ) và cơ chế Chân Danh
│   │   ├── SaveSystem/           # Lưu game tự động (AutoSave) và lưu tại Checkpoint/SaveRoom
│   │   └── UI/                   # Lời thoại (Subtitle Prompt), Văn tế hiển thị tài liệu, Menu điều khiển
│   │
│   ├── Settings/                 # Cấu hình Universal Render Pipeline (URP) hoặc HDRP
│   │
│   ├── Shaders/                  # Các Shader tùy biến (Hiệu ứng chớp đỏ Mắt Âm Dương, nhựa cây chảy màu đỏ sẫm...)
│   │
│   └── UI/                       # Tài nguyên giao diện người dùng
│       ├── Fonts/                # Font chữ Việt hóa cổ điển, ma mị phục vụ hiển thị Sớ/Văn tế
│       ├── Icons/                # Icon cho 8 ô chứa đồ (Vật phẩm: Con búp bê, bật lửa, mảnh sành...)
│       └── Textures/             # Hình nền Menu, viền màn hình khi Sanity thấp (Nhòe/Chớp máu)
│
├── Plugins/                      # Các thư viện bên thứ ba dạng mã nguồn mở hoặc kéo từ Github (DOTween, v.v.)
└── X_ThirdParty/                 # Các Asset mua từ Unity Asset Store (Để riêng ở đây để không trộn lẫn với code gốc)
```
