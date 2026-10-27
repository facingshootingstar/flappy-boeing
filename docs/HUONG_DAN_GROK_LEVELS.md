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

- Giữ nguyên loại màn 1, 2, 4, 5, 6. Màn 4 (né tên lửa) được làm dịu: tên lửa dễ chậm hơn máy bay, 0.55 giây đầu không nổ, ít tên lửa hơn. Bot sống đủ 20 giây nhưng 0 điểm vì chỉ né, không nhặt sao. Ngưỡng 3/6 điểm giữ nguyên: sống sót là 1 sao, điểm là phần thưởng. Màn xếp thùng: thùng không tràn khỏi màn, bot thả khi thùng đi qua đúng tâm.
- Ngưỡng sao hạ cánh lấy từ lượt bot 167 điểm / 3 lần đáp. Màn 6 là 110 và 165. Màn 11 là 100 và 145. Màn 17 là 120 và 170. Boss vẫn 6/9 (4 phát trúng / 1 phát). Xếp thùng màn 2 vẫn 2/4 vì bot hoàn hảo không phải người chơi.
- Ba mini game mới, cùng khuôn `MiniGame`, bot tự chơi được: `SupplyDrop` (thả hàng, 3 lần trượt thì thua, bắt giữa +10), `BoltLane` (3 làn, sét rơi, điểm là lần né sát), `SkyRefuel` (khớp độ cao máy bay tiếp dầu, mỗi lần tuột trừ 6 điểm, đầy bình thì thắng). Kết quả bot: màn 7 bắt 60 điểm / 3 sao, màn 12 né 9/10 / 3 sao, màn 16 tiếp dầu 100 điểm / 3 sao.
- Biến thể `LevelTwist`: `Wind` đẩy ngang, `Night` phủ một lớp đêm (Flappy/Boss ở order 13, mini game ở trên sprite và dưới HUD), `Quota` cộng một lần phần thưởng bằng khoảng cách sao 2 và sao 3. Quota không làm thua.
- Thế giới 5 `STARLIGHT` (màn 21–25), dùng lại bầu trời dusk. Cổng sao mỗi thế giới: 0, 6, 15, 26, 40. Thế giới 2 chỉ cần 6 sao để hai màn 3 sao đầu mở được, không khoá màn 6. `Play()` vẫn không kiểm tra khoá (để bot nhảy màn). Nút NEXT và bản đồ thì kiểm tra `Unlocked` sau khi sao đã lưu.
- Rương mỗi 10 sao, +25 coin, bit `chestMask` trên save. Save cũ thiếu field thì mặc định 0, không tăng version. Máy bay trên bản đồ bay từ màn vừa thắng tới màn biên giới.
- Juice dùng lại `CameraFx.Shake` và `AudioManager.Sfx` (cao độ tăng theo combo). Chữ nảy qua `PopText`. Chỉ dùng tên SFX có trong danh sách.
- Ảnh mới vẽ bằng pixel cứng, đúng bảng màu, nằm ở `Assets/_Game/Art/props/` (rương, thùng dù, cổng, máy bay tiếp dầu). Sét dùng `Art/fx/lightning_bolt.png`. Game đọc file lúc chạy, lọc point, không sửa scene.
- Save dev đã bị các lượt chơi thử ghi đè: `C:/Users/ADMIN/AppData/LocalLow/DefaultCompany/Flappy Boeing/save.json`. Màn 3 chưa có sao nên bản đồ khoá màn 4 dù màn 4 đã được bot chơi thẳng. Đó là luật "màn trước phải có sao", không phải lỗi tính sao.
- Cốt truyện: save v3 thêm `seenCutscenes` và `postcards`. Migrate luôn tạo list rỗng khi field null, kể cả save đã ghi version 3. Intro chỉ tự chạy khi chưa thấy. Bot hoặc `CutscenePlayer.SkipAll` bỏ qua và không đánh dấu đã xem. Một khung hình khựng không được nhảy quá 0.05 giây trong cutscene.
- Thẻ boss không đổi cơ chế đánh. Màn 5 đại bàng, màn 10 Ironwake, màn 15 vẫn là súng Sundown nhưng cảnh sau trận là đại bàng cõng Poe (một khung hình, không phải màn mới). Màn 20 là bầy cuối, tháp đổ. Màn 25 vẫn goal 2, chỉ đổi tên hiển thị thành THE BLACKOUT và phủ một lớp tối không chặn input. Vàng khi tổng sao ≥ 60.
- Gilt từng lái thư nằm trong một khung của world4. Câu Pip hiện trên bảng giới thiệu màn. Lật trang là trượt ngang. Vòng giữ để bỏ qua là một đĩa vàng.
- Ảnh truyện vẽ bằng code, nearest-neighbor, trong `Assets/_Game/Art/story/`. Tên file máy bay cũ giữ nguyên, pixel bên trong đổi thành Poe. Object scene vẫn tên Poeing. `productName` vẫn Flappy Boeing để thư mục save không đổi chỗ. Logo trong game là `POE: THE LAST MAIL RUN`.
- Unity Recorder 5.1.7 quay intro ra `Builds/trailer/intro.mp4`: 1920x1080, 60fps, 75.48 giây, có tiếng. Thư mục Builds bị gitignore nên file không vào commit. Trong game không phát mp4.
- Cutscene vẽ trên HUD của bản đồ. Nút STORY mở lại cảnh đã xem. Nút POSTCARDS là lưới 5x5. Bưu thiếp lưu trước chuỗi cảnh thắng, hiện 1.5 giây rồi mới tới bảng kết quả. Chưa chụp được nhịp bưu thiếp trên bảng kết quả vì không có màn mới nào được bot thắng trong lượt kiểm tra.
- Phase 1: Poe vẽ tay cao 1.4, `PropArt.PoeSet` gán vào `SpriteAnimator.Play` ngay từ frame chờ tap. Animator scene vẫn giữ sheet pixel cũ và ghi đè sprite mỗi frame nếu không `Play`. Bot thì `Begin` luôn để đàn chim (ống Flappy) đã nằm trong khung, sorting 16. Đĩa `Ui.Disc` cỡ 1 đơn vị khi thiếu sprite trông như vết bẩn trên ống kính, nên mote chỉ còn khoảng 0.045 đơn vị, alpha tối đa 0.22, không xoay. Lớp front co còn 0.34 chiều cao khung và alpha 0.2, đặt cao. Nút bản đồ nâng lên y khoảng 2.35, tên thế giới lên sát mép trên, đường nối là hai dải sương chứ không phải hàng đĩa. Cỏ và hố đọc `parallax/props/grass.png` và `pit.png`. Đường băng phối cảnh không dùng làm tile. Sundown và Starlight có đủ 5 lớp; đồi sundown lần đầu bị khóa vì cùng họ màu magenta, phải vẽ lại bằng đất ochre và đá xám.
- Đồ họa mới làm bằng Imagine, không vẽ pixel bằng code. Sprite khóa nền nằm ở `Assets/_Game/Art/imagen/`. Bản đồ dùng bầu trời Imagine. Một số khung địa điểm và cảnh hangar trong `Art/story/` được thay. Menu đầu game: PLAY, RANKING, OPTIONS, EXIT. RANKING đọc sao và điểm đã có trong save, không đổi công thức sao. OPTIONS bật nhạc và ba mức SFX. Phi công chạy màn Bolt và đứng trên boong màn Stack. Màn Bolt là chạy thư vì bão không cho Poe cất cánh. Máy bay các màn bay vẫn là Poe. `productName` và object scene Poeing giữ nguyên.
- Màn Bolt (12, 19, 22) là chạy ngang: A/D hoặc mũi tên, nhảy bằng Space. Qua đúng `goal` hố thì thắng, rơi hoặc dính sét thì thua, mỗi lượt `End` một lần. Điểm là thư nhặt được. Ngưỡng sao và số hố giữ nguyên. Chữ trên thẻ là `CROSS N GAPS` và `MAIL`. Bot đã qua màn 12 (thư 5, 2 sao). Bầu trời Imagine của Flappy và boss nằm trên mặt phẳng chơi, sorting order 5, dưới chim và Poe. Tấm dán lên camera phải đẩy ra z của mặt phẳng, nếu không camera cắt mất. Khung truyện 480 đã thay bằng Imagine. `b1_sink` phải vẽ lại vì lần đầu có tên tàu. Bưu thiếp pc03 và pc04 đã thay. pc01 và pc02 vẽ lại bị chữ nên giữ ảnh cũ. pc05–pc25 vẫn ảnh cũ. `end_gold` và `end_rise` vẫn còn vết nhỏ trên đuôi.
- Sửa bốn lỗi giám sát phase 1. Đàn chim đầu màn 1 không còn ở x=4.6: `spawnX` ép 11 dù cảnh vẫn ghi 9, khe mở 4.6 trên y=0, chim trái nhất ngoài mép 7.5. Bot gắn `AutoPilot` khi `MiniGame.Bot` và vỗ một nhịp mỗi lần chìm, không giữ nút. Play thật còn sống ở 10 cột. Mép tile: cột 0 của tấm trời copy cột cuối, lớp có khóa bleed màu vào viền trong suốt, tile chồng 3 texel hoặc 18% nếu có fade. Ảnh chơi màn 16 và 21 không còn vạch sáng suốt chiều cao. Tuyến bản đồ hạ xuống khoảng y -0.85 đến 1.25, tên thế giới ở y=-2.65 nên không đè STORY/MENU, LAND (màn 11) nằm trọn khi căn màn 9. `sceneLoaded` có lúc thấy `LevelRuntime` sắp bị hủy nên `Play` tải scene thô; `Ensure` chỉ nhận runtime của scene đang chạy và `GameState.Start` gọi lại. Video `Builds/trailer/p1_lv1.mp4` và `p1_lv12.mp4`: 1920x1080, 60fps, 20.02 giây, hai track, không vào git. FPS parallax trong Play này: 77, 99, 104, 112, 138. Console không lỗi đỏ. `csc_check` NO ERRORS.
- Phase 2: vạch dọc 1 pixel là vết xước `FilmFilter` (4 px màn hình), đã bỏ vòng scratch; bụi vuông đổi thành đĩa mềm hiếm. Cánh quạt dấu cộng đổi thành cánh vải chữ X. Aurora màn 21 là đĩa mềm, sao băng là vệt nhọn. Chim nền và chim chướng ngại dùng chung 4 frame mòng biển viền mực. Boong, đường băng, xe đẩy, đất Bolt (cỏ, đá, rễ), thuyền, khinh khí cầu, cờ, đom đóm, lá, khói và cụm mây nối bản đồ đã vẽ; thiếu file thì không vẽ hộp thay. Trên ảnh chơi, vệt sáng rộng 4 px dài nhất là 62 px (màn 7), không còn vạch suốt màn. FPS cửa sổ 240 frame đầu: 96 đến 109 trên các màn vừa chơi; cửa sổ frame 240–480 lên vài trăm vì editor không khoá vsync. `csc_check` NO ERRORS. Ảnh `Assets/Screenshots/p2_lv1`, `p2_lv5`, `p2_lv7`, `p2_lv12`, `p2_lv16`, `p2_lv21`, `p2_map` (nối bình minh/biển), `p2_cs_wren`, `p2_cs_pip`, `p2_cs_gold`, `p2_cs_carry`, `p2_pc05`, `p2_pc14`, `p2_pc25`. Video `Builds/trailer` (gitignore): `p2_lv7.mp4` và `p2_lv12.mp4` 1920x1080, 60fps, 20.02 giây, có tiếng; `p2_intro.mp4` 76.02 giây, có tiếng, không `SkipAll`. Bot xong màn 7 trước giây 12 của video nên nửa sau là bảng LEVEL CLEAR. Còn dở: `pc01`/`pc02` giữ pixel cũ vì vẽ lại bị chữ; mũ `face_pip` còn chữ MAIL; `pc07`, `pc10`, `pc18`, `pc21` có vết nhãn nhỏ; `w5_climb` và `w5_machine` cùng bố cục hải đăng nhưng một bên là sao, một bên là máy đen; `poe_happy`/`scared`/`set`/`tired` và sheet pixel trong `story/` không được code đọc. Boong gỗ còn phối cảnh nhẹ khi lặp. Không push.
