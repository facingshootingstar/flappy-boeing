# Giai đoạn 2: đồng bộ tranh truyện, đạo cụ vẽ tay, sạch lỗi hình

Giai đoạn 1 đạt phần lớn: nền 5 lớp, Poe vẽ tay, bản đồ gọn (ảnh `Assets/Screenshots/p1_sheet.png`). Người giám sát (Claude) vẫn chấm theo chuẩn **indie thương mại hạng nặng**. Luật kỹ thuật ở mục D của `docs/HUONG_DAN_GROK_LEVELS.md` vẫn giữ nguyên. Thiết kế Poe đã khoá ở `tools/art/ref/poe_ref.png`.

## A. Lỗi hình còn sót từ giai đoạn 1 (sửa trước)

Ảnh chụp 1920×1080:

1. **Vạch dọc mảnh 1 pixel chạy từ trên xuống.**
   - Thấy ở màn 1 khoảng x≈830 (frame 12 s trong `Builds/trailer/p1_lv1.mp4`).
   - Thấy ở màn 12 khoảng x≈1690 (frame 12 s trong `p1_lv12.mp4`).
   - Thấy ở màn 7 khoảng x≈1440 (`p1_sea_7b`).
   - Tìm nguồn: mép tile, sprite debug, hay tia sáng. Bỏ hẳn.
   - Màn 1 có thêm một dấu chữ thập nhỏ gần x≈790, y≈600. Nếu là đồ debug thì bỏ.
2. **Màn 21 (Starlight):** có các dải ngang sáng hình chữ nhật, nhìn như lỗi, quanh y≈150 và giữa màn hình. Bỏ hoặc vẽ lại thành mây hay dải ngân hà có viền mềm.
3. **Chim nền Flappy:** chim vẫn là pixel thô, lệch phong cách tranh vẽ tay. Vẽ lại cả chim nền lẫn chim chướng ngại theo phong cách viền mực, có animation vỗ cánh ít nhất 4 frame.
4. **Các chấm đen vuông nhỏ rải khắp màn hình** (hiệu ứng bụi phim). Đổi thành hạt tròn mềm, mờ hơn, hoặc bỏ. Hiện trông như pixel chết.

## B. Đạo cụ và nền đất còn là hộp (đã ghi trong báo cáo mục E của bạn)

- **Boong tàu và đường băng:** đang là thanh hộp vàng phẳng trong SupplyDrop, CargoStack và PerfectLanding. Vẽ sprite thật:
  - boong gỗ có đinh tán
  - đường băng có vạch sơn và đèn
  - xe đẩy hàng có bánh
- **Đất màn Bolt:** đang là khối nâu phẳng có viền cỏ mỏng. Cần đất vẽ tay: lớp cỏ dày, đá, rễ cây, mép bờ vực gồ ghề, tile liền không lộ đường nối.
- **Đạo cụ sống động** (vẽ tay, nền trong suốt, có animation):
  - mòng biển bay ngang ở Sunny Sea
  - thuyền nhấp nhô trên mặt biển
  - khinh khí cầu trôi chậm ở Sundown
  - cờ bay phấp phới trên mái nhà hoặc cột buồm
  - đom đóm hoặc sao băng ở Starlight
  - mỗi thế giới ít nhất 2 loại
- **Bản đồ:** chỗ nối giữa hai thế giới đang là dải sương mờ. Thay bằng cụm mây vẽ tay có viền mực đè lên mép.

## C. Tranh truyện (cutscene) phải cùng một Poe

Rà toàn bộ `Assets/_Game/Art/story/`:

1. **Poe:** mọi ảnh có Poe phải đúng `poe_ref.png`: thân ĐỒNG, kính TEAL có mắt, cánh quạt trước, túi thư. Ảnh nào sai màu hoặc sai dáng thì image_edit lại từ tờ ref.
2. **Ảnh trùng:** ảnh nào trùng hoặc gần trùng nhau thì làm lại để mỗi khung kể một nhịp khác.
3. **Chân dung** `face_bubba`, `face_gilt`, `face_pip`: cùng nét mực, cùng khung, nhìn rõ cảm xúc.
4. **Ảnh kết** `end_gold`, `end_rise`: bỏ chữ hoặc ký hiệu lạ do AI vẽ. Bỏ cả watermark nếu có.
5. **Bưu thiếp pc05–pc25:** tranh nào còn thiếu thì vẽ, tranh nào xấu thì vẽ lại. Mỗi tấm gắn với một màn, đúng thế giới của màn đó.
6. **Quay lại intro** bằng Unity Recorder như giai đoạn 1. Lưu `Builds/trailer/p2_intro.mp4`.

## D. Nghiệm thu

- **Biên dịch:** `python tools/csc_check.py .` in ra NO ERRORS.
- **FPS:** ≥ 60 trong Play.
- **Ảnh chụp:** `Assets/Screenshots/p2_<tên>.png`, tối thiểu gồm:
  - màn 1, 5, 7, 12, 16, 21 trong lúc chơi
  - bản đồ ở chỗ nối thế giới
  - 4 khung cutscene có Poe
  - 3 bưu thiếp
- **Video:** `Builds/trailer/p2_lv7.mp4`, `p2_lv12.mp4`, `p2_intro.mp4`.
- **Commit:** commit nhỏ theo từng mục. KHÔNG push.
- **Báo cáo:** cuối việc ghi vào mục E của `docs/HUONG_DAN_GROK_LEVELS.md`, gồm việc đã làm và việc còn thiếu. Ghi trung thực.
