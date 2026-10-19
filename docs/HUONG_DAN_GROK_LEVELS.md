# Bàn giao: Bản đồ màn chơi kiểu Candy Crush + mini game

Tài liệu cho agent tiếp theo (Grok). Phần **A** là việc đã xong, **B** là việc bắt buộc làm tiếp, **C** là chỗ được tự sáng tạo, **D** là luật kỹ thuật bắt buộc tuân thủ.

Branch: `feature/foundation`. Kế hoạch tổng: `docs/KE_HOACH_THUONG_MAI.md`.

---

## A. Đã làm (chưa commit, chưa play-test trong Unity)

Code biên dịch sạch bằng `python tools/csc_check.py "D:/learning material/Flappy Boeing"` → `NO ERRORS`.

| File | Vai trò |
|---|---|
| `Assets/_Game/Scripts/Levels/LevelDB.cs` | 20 màn, 4 thế giới × 5 màn. `enum LevelGame { Flappy, Boss, Missile, Stack, Landing }`. Mỗi màn: `goal`, `stars[3]` (ngưỡng điểm), `hard` (0..1). Màn 5/10/15/20 là boss. |
| `Levels/LevelFlow.cs` | `LevelFlow` (static): tiến độ, mở khoá (màn trước ≥1 sao), `Play(id)`, `Map()`, `Finish(won, score)` → tính sao, trả coin (15/sao mới), lưu save. `LevelRuntime` (GameObject "Levels"): dựng màn, banner intro, màn hình kết quả (RETRY / MAP / NEXT). |
| `Levels/LevelMap.cs` | Bản đồ cuộn ngang: node theo đường sin, 3 sao trên mỗi node, khoá "?", máy bay đánh dấu màn hiện tại, thẻ màn (PLAY). Kéo/lăn chuột/phím trái-phải. |
| `Levels/Ui.cs` | Khối dựng UI world-space (`Ui.Box/Spr/Txt/Btn/Plaque/Disc`) + `UiInput` (chuột/chạm/phím, `InjectedClick` cho test). |
| `Levels/MiniGame.cs` | Lớp nền mini game: READY/GO, HUD trên cùng, `End(won)` → `LevelFlow.Finish`. Cờ `MiniGame.Bot` để game tự chơi khi test. |
| `Levels/MissileDodge.cs` | Máy bay luôn bay, chỉ rẽ trái/phải; tên lửa tầm nhiệt đuổi; dụ 2 tên lửa đâm nhau (+2), ăn sao (+1). Sống đủ `goal` giây là qua. |
| `Levels/CargoStack.cs` | Kiểu "Stack": thùng hàng trượt trên boong tàu, chạm để thả, phần thừa bị cắt rơi. Thả chuẩn = PERFECT (điểm), combo 3 thì thùng nới rộng lại. |
| `Levels/PerfectLanding.cs` | Lượn hạ cánh: giữ để nâng, chạm đường băng nhẹ + gần cờ = tối đa 100 điểm/lần. Lệch đường băng hoặc rơi mạnh = nổ. |
| Sửa: `SaveSystem.cs` (save v2, `levels` list), `GameState.cs` (`OnlyBoss`), `BossDirector.cs` (chơi 1 boss), `HudController.cs` (nhường kết quả cho LevelRuntime) | |

Một thay đổi nhỏ cuối cùng: `LevelFlow.Play` đặt `Last = null` (chưa được Unity biên dịch lại).

---

## B. Việc bắt buộc tiếp theo (theo thứ tự)

1. **Biên dịch trong Unity**: focus Unity, Ctrl+R, xem Console không có lỗi đỏ.
2. **Play-test từng phần** (script mẫu: `levels_test.py` trong scratchpad cũ, hoặc tự viết bằng `tools/umcp.py` / `tools/ux.py`):
   - Vào Play → phải thấy **bản đồ**, không phải Flappy. Chụp `lvl_map`.
   - `LevelFlow.Play(1)` (Flappy), `Play(2)` (Stack), `Play(4)` (Missile), `Play(5)` (Boss), `Play(6)` (Landing). Bật `MiniGame.Bot = true` để tự chơi.
   - Kiểm tra màn hình kết quả: sao hiện đúng, coin cộng, nút NEXT mở màn kế, `save.json` có `levels`.
3. **Sửa lỗi phát hiện được**. Chỗ nghi ngờ nhất:
   - Hướng sprite máy bay trong MissileDodge (lật `localScale.y` khi bay sang trái).
   - `LevelRuntime.Rocket/Torpedo` có thể null → tên lửa thành hình tròn.
   - Cân bằng độ khó: ngưỡng sao trong `LevelDB.cs` là đoán, cần chơi thử rồi chỉnh.
   - Text bị tràn màn hình (vùng nhìn x ±7.5, y ±4.22).
4. **Commit** trên `feature/foundation`, mỗi bước một commit nhỏ.

---

## C. Không gian sáng tạo cho Grok 🎨

Mục tiêu: game **gây nghiện kiểu Candy Crush**, muốn chơi "thêm một màn nữa". Được tự quyết, miễn giữ luật ở phần D.

Gợi ý (chọn, đổi, hoặc nghĩ cái hay hơn):

- **Thể loại mini game mới** (mỗi cái là 1 class kế thừa `MiniGame`, thêm vào `enum LevelGame` + `LevelRuntime.Awake`):
  - Thả hàng tiếp tế trúng đích từ máy bay đang bay.
  - Bay đội hình: chạm đúng nhịp để giữ đội hình (rhythm).
  - Tiếp nhiên liệu trên không: giữ máy bay khớp vòi của máy bay tiếp dầu.
  - Điều khiển không lưu: vẽ đường cho nhiều máy bay hạ cánh không đụng nhau.
  - Né mây giông / sét theo làn (lane runner).
- **Biến thể trong màn** để 20 màn không lặp: gió mạnh, ban đêm (tầm nhìn hẹp), mục tiêu phụ ("ăn 5 sao"), giới hạn số lần chạm.
- **Bản đồ sống động hơn**: hiệu ứng mở khoá màn mới, máy bay bay từ node cũ sang node mới, rương thưởng mỗi 10 sao, cổng sao ("cần 12 sao để vào thế giới 3").
- **Thêm thế giới** sau màn 20 (dữ liệu chỉ là thêm dòng `L(...)` trong `LevelDB.cs`).
- **Cảm giác (juice)**: rung camera, chữ nảy, âm thanh combo tăng cao độ, sao bay vào thanh đếm.
- **Đồ hoạ**: được dùng Grok tạo ảnh pixel-art (giữ phong cách và bảng màu hiện có), đặt trong `Assets/_Game/Art/`.

Ghi lại quyết định thiết kế của mình ở cuối file này (mục E) để agent sau hiểu.

---

## D. Luật kỹ thuật bắt buộc

- **Unity 6 (6000.6.0f1), 2D, một scene** `Assets/_Game/Scenes/Main.unity`, dựng bởi menu `Flappy/Build All`. Không sửa scene bằng tay.
- Mọi màn dùng chung scene, bị reload. Mini game **tự tạo hết bằng code** trong `Build()`, không cần prefab.
- Camera ortho, 32 PPU, vùng nhìn x ±7.5, y ±4.22.
- **PixelText chỉ có**: `A-Z 0-9 ! ? . : - + / ' %` và dấu cách. **Không có chữ thường, không có dấu phẩy, không dấu tiếng Việt.** Mỗi ký tự rộng 0.25 đơn vị ở scale 1.
- Mỗi mini game gọi `End(won)` **đúng một lần**; `score` là số dùng để tính sao.
- Sao: Flappy qua ở `stars[0]` ống; game khác qua khi đạt `goal`, sao 2/3 theo `score ≥ stars[1]/stars[2]`.
- API có sẵn: `GameKit.Burst/Pop`, `CameraFx.Shake`, `AudioManager.Sfx(name, vol, pitchVar, minGap, pitch)`, `TapInput.Pressed()`, `CombatInput.Move()/Fire()`, `Announcer.Yellow/Red/Teal/Cream/Pink`.
- Tên SFX hợp lệ: explosion, roar, card, iris, swoop, dash, wallop, thunder, super, shoot, hurt, victory, score, ready, parry, knockout, hit, flap, ex, death.
- Kiểm tra biên dịch offline: `python tools/csc_check.py "D:/learning material/Flappy Boeing"`.
- An toàn: không chạy agy với `--dangerously-skip-permissions`, không in file cấu hình chứa khoá, không `git push --force` / `reset --hard`.

---

## E. Nhật ký quyết định (Grok ghi tiếp vào đây)

- …
