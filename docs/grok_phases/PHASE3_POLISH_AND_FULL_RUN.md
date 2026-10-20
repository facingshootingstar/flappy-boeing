# Giai đoạn 3: hoàn thiện và chạy đủ 25 màn

Giai đoạn 2 đã được nghiệm thu (ảnh `Assets/Screenshots/p2_sheet_play.png`, `p2_sheet_story.png`). Đây là vòng cuối trước khi gọi là game hoàn chỉnh. Luật ở mục D của `docs/HUONG_DAN_GROK_LEVELS.md` vẫn giữ nguyên.

## A. Hình còn trượt

1. **Màn 16 (Sundown):** tiền cảnh có các thanh chữ nhật nâu chéo, trông như hộp placeholder (ảnh `p2_lv16`).
   - Nếu là ống khói hoặc dây phơi thì vẽ lại cho ra vật thật.
   - Nếu không thì bỏ đi.
2. **Phần dở bạn đã ghi ở mục E:**
   - `pc01` và `pc02` còn pixel. Vẽ lại.
   - Mũ `face_pip` còn chữ MAIL. Xoá chữ.
   - Vết nhãn trên `pc07`, `pc10`, `pc18`, `pc21`.
   - `w5_climb` và `w5_machine` cùng bố cục. Làm một tấm khác hẳn.
   - Sheet Poe không dùng trong `story/`: dùng cho cutscene nếu hợp, không thì xoá.
   - Boong gỗ lặp còn lệch phối cảnh.
3. **Bản đồ:** chữ tên thế giới (ví dụ "DAWN VALLEY") và các nút màn bị cắt ở mép màn hình khi camera dừng. Giữ camera để nút đang chọn và tên thế giới luôn nằm trọn trong khung.

## B. Cảm giác chơi (juice)

Ở mọi minigame, kiểm tra và bổ sung chỗ còn thiếu:
- Rung màn hình nhẹ khi trúng đòn hoặc khi đáp mạnh.
- Hitstop 2–4 frame khi trúng boss.
- Hạt bụi hoặc tia lửa khi va chạm.
- Số điểm bật lên khi ghi điểm.
- Chữ LEVEL CLEAR có animation, và sao hiện lần lượt.

Giữ hiệu ứng vừa phải, không che gameplay.

## C. Chạy bot đủ 25 màn (bắt buộc)

- Chạy lần lượt màn 1 đến 25 với bot, theo cách đã dùng ở giai đoạn trước: `MiniGame.Bot=true; CutscenePlayer.SkipAll=true; LevelFlow.Play(id)`.
- Ghi bảng vào `docs/grok_phases/PHASE3_RUN.md`, mỗi màn một dòng, gồm:
  - id
  - loại minigame
  - kết quả CLEAR hoặc FAIL
  - số sao
  - thời gian
  - FPS thấp nhất
  - lỗi trong console (nếu có)
- Màn nào FAIL vì bot không qua được thì chỉnh độ khó hoặc chỉnh bot cho hợp lý, nhưng người thật vẫn phải chơi được. Màn nào có exception thì sửa.
- Mỗi màn chụp một ảnh `Assets/Screenshots/p3_lv<id>.png` lúc đang chơi.
- Cuối cùng chạy luồng đầy đủ không SkipAll ít nhất cho màn 1: menu → intro → bản đồ → màn 1 → kết quả → bản đồ. Xác nhận không kẹt.

## D. Nghiệm thu

- `python tools/csc_check.py .` in ra NO ERRORS.
- Bảng `PHASE3_RUN.md` có đủ 25 dòng CLEAR, không có exception.
- Quay video `Builds/trailer/p3_trailer.mp4` dài 60–90 giây, ghép cảnh đẹp từ cả 5 thế giới và một trận boss.
- Commit nhỏ theo từng mục. KHÔNG push.
- Báo cáo trung thực ở mục E.
