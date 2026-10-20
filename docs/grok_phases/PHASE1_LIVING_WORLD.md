# Giai đoạn 1: thế giới sống động và Poe đúng phong cách

Người giám sát (Claude) sẽ chấm bằng ảnh chụp và video sau khi bạn xong. Tiêu chuẩn là **game indie thương mại hạng nặng** (Cuphead, Ori, Hollow Knight về độ sống động), không phải bản demo.

Đọc trước:
- `docs/THIET_KE_COT_TRUYEN.md`
- mục D và E của `docs/HUONG_DAN_GROK_LEVELS.md`

Giữ đúng mọi luật kỹ thuật ở mục D.

## Hiện trạng bị chấm trượt (ảnh: `Assets/Screenshots/base_lv1.png`, `base_lv5.png`, `base_lv12.png`, `base_map.png`)

1. **Nền mỗi màn là một bức tranh đứng yên.** Không có chiều sâu, không có gì chuyển động. Đây là lỗi nặng nhất.
2. **Sprite Poe trong màn chơi là cục pixel thô** (khối đồng + ô teal), lệch hẳn phong cách tranh vẽ tay của nền. Trông như hai game khác nhau.
3. **Màn Bolt (12):** đất là các khối hộp phẳng màu nâu, hố là ô đen trơn.
4. **Bản đồ:** thấy rõ đường nối giữa hai ảnh nền thế giới (khoảng x=1440 trên ảnh chụp). Các nút màn đè lên thùng hàng và thuyền nên khó đọc.
5. Số điểm Flappy ở đầu màn hình là một khung rỗng mảnh, nhìn như lỗi.

## Việc phải làm

### 1. Khoá thiết kế Poe và làm sprite mới (làm đầu tiên)

- **Thiết kế Poe:** máy bay đưa thư cánh quạt nhỏ, thân màu ĐỒNG, kính buồng lái TEAL có đôi mắt hoạt hình, cánh quạt đằng trước, túi thư nhỏ bên hông. Giống Poe trong `Art/story/b0_free.png` và `w4_hop.png`.
- **Tờ tham chiếu:** tạo bằng Grok image_gen, lưu `tools/art/ref/poe_ref.png`. Mọi ảnh có Poe sau này đều dùng image_edit từ tờ này.
- **Sprite trong màn:** phải cùng phong cách vẽ tay với nền, viền mực, đổ bóng kiểu hoạt hình. Không dùng pixel thô.
  - Nền trong suốt.
  - Có các animation: bay (cánh quạt quay, thân nhún, ít nhất 4 frame), ngóc lên, chúi xuống, bị trúng đòn.
  - Thay ở mọi nơi đang dùng sprite máy bay: Flappy, boss, MissileDodge, PerfectLanding, SkyRefuel, SupplyDrop, bản đồ.
  - Kích thước hiển thị phải đọc rõ, Poe là nhân vật chính.
- **Tiếng và hiệu ứng:** khói phụt từ động cơ khi bấm bay, thân nghiêng theo vận tốc, có squash/stretch nhẹ.

### 2. Nền parallax nhiều lớp và chuyển động cho cả 5 thế giới

Mỗi thế giới (DAWN VALLEY, SUNNY SEA, STORM PEAKS, SUNDOWN, STARLIGHT) phải có **tối thiểu 5 lớp**, mỗi lớp cuộn một tốc độ:
1. Bầu trời: gradient, mặt trời hoặc trăng, sao lấp lánh ở đêm.
2. Lớp xa: núi, đảo, đường chân trời.
3. Lớp giữa: đồi, thành phố, hải đăng, cảng.
4. Lớp gần: cây, mái nhà, cột buồm.
5. Tiền cảnh mờ: lá, mây, sương lướt qua phía trước Poe, alpha thấp, không che gameplay.

**Các lớp phải lặp liền mạch** (tileable theo chiều ngang, không thấy đường nối) và cuộn liên tục khi màn chạy.

**Chi tiết sống động bắt buộc**, ít nhất 4 thứ chuyển động trên mỗi thế giới:
- **DAWN VALLEY:** mây trôi, đàn chim xa vỗ cánh, khói bốc từ ống khói, đèn hải đăng quay quét tia sáng, cối xay gió quay, cờ phấp phới.
- **SUNNY SEA:** sóng lấp lánh, thuyền nhấp nhô, hải âu, bọt trắng, ánh nắng lung linh trên mặt nước.
- **STORM PEAKS:** mưa nghiêng nhiều lớp, chớp loé sáng cả màn hình, mây đen cuộn, lá bay ngang.
- **SUNDOWN:** bụi vàng bay trong nắng, đèn thành phố bật dần, khinh khí cầu trôi, khói nhà máy Citadel.
- **STARLIGHT:** sao băng, cực quang uốn lượn, đom đóm sáng, mây phát sáng dưới chân, Hải Đăng ở xa.

**Thời gian trong màn:** ánh sáng đổi nhẹ theo tiến độ màn (ví dụ DAWN sáng dần lên), dùng lớp phủ màu.

**Cách làm:**
- Vẽ từng lớp bằng Grok image_gen (nền trong suốt hoặc magenta để khoá màu), lưu `tools/art/raw/parallax/<world>/`.
- Xử lý vào `Assets/_Game/Art/parallax/<world>/`.
- Viết `ParallaxWorld.cs` dựng lớp bằng code, đọc file lúc chạy giống `PropArt`, cho scale vừa vùng nhìn x ±7.5, y ±4.22.
- Hạt (mưa, lá, bụi, sao) làm bằng code hoặc sprite nhỏ. Giữ FPS ≥ 60.
- **Dùng chung** cho Flappy, Boss và mọi mini game theo thế giới của màn. Mini game nào cần mặt đất riêng (Landing, Stack, Bolt) thì đặt mặt đất của nó lên trên lớp giữa.

### 3. Màn Bolt và các mini game có mặt đất

- Thay khối hộp phẳng bằng **sprite đá, cỏ, ván gỗ vẽ tay**, có mép và cạnh.
- Hố có chiều sâu (vách đá tối dần, sương dưới đáy).
- Đường băng màn Landing và boong tàu màn Stack cũng thay sprite thật thay vì `Ui.Box`.

### 4. Bản đồ

- Nối các thế giới bằng **đoạn chuyển tiếp vẽ tay** (đám mây, sương) để không còn đường nối cứng.
- Thêm parallax nhẹ khi kéo bản đồ.
- **Nút màn:** đặt lên một đường mòn hoặc tuyến bay vẽ nét đứt, không đè lên đồ vật. Phải đọc rõ.
- Bóng mây trôi qua bản đồ.

### 5. Sửa nhỏ

- Số điểm Flappy: chữ to, viền đậm, nảy khi tăng điểm.

## Quy trình và nghiệm thu

1. Sau mỗi phần, chụp ảnh bằng `python tools/ux.py shot <name>` (xem `tools/ux.py`, `tools/umcp.py`, đã có Unity MCP).
   - Vào Play.
   - `MiniGame.Bot = true; CutscenePlayer.SkipAll = true; LevelFlow.Play(id)`.
   - Ảnh tên `p1_<world>_<n>.png`.
2. **Mỗi thế giới chụp 2 ảnh cách nhau 3 giây** để chứng minh nền có chuyển động. Chụp đủ màn 1, 7, 12, 16, 21 và bản đồ.
3. Quay video 20 giây màn 1 và màn 12 bằng Unity Recorder ra `Builds/trailer/p1_lv1.mp4`, `p1_lv12.mp4`.
4. `python tools/csc_check.py "D:/learning material/Flappy Boeing"` phải ra NO ERRORS. Console không có lỗi đỏ. FPS ≥ 60 (ghi số đo được).
5. **Commit nhỏ** theo từng phần trên `feature/foundation`. KHÔNG push, không force, không reset --hard, không sửa scene bằng tay.
6. Ghi quyết định vào mục E của `docs/HUONG_DAN_GROK_LEVELS.md`.
7. Cuối cùng in báo cáo:
   - danh sách commit
   - ảnh đã chụp
   - FPS đo được
   - phần chưa đạt và vì sao

Nếu một thế giới làm không kịp, **làm trọn vẹn DAWN VALLEY và STORM PEAKS trước**, rồi mới làm các thế giới khác. Đừng làm cả 5 thế giới nửa vời.
