# Thiết kế lại: cốt truyện, intro, cutscene

Tài liệu cho Grok. Mục tiêu: biến chuỗi mini game rời rạc thành **một chuyến phiêu lưu có lý do**. Màn nào cũng phải trả lời được câu hỏi "tại sao mình chơi màn này?".

Phong cách: **phim hoạt hình thập niên 1930 kiểu Cuphead**. Game đã có sẵn bộ lọc phim (`FilmFilter`), hiệu ứng iris, thẻ tên boss và tiếng "card". Cutscene phải dùng đúng các thứ này để đồng bộ.

Các mục:
- **1–5:** bản thiết kế, đọc là hiểu.
- **6:** đặc tả kỹ thuật.
- **7:** danh sách ảnh cần vẽ.
- **8:** tiêu chí nghiệm thu.
- **9:** chỗ để Grok tự sáng tạo.

---

## 1. Ý tưởng một câu

> Một chiếc máy bay đưa thư già sắp bị đem đi bán sắt vụn trở thành hy vọng cuối cùng của quần đảo trên mây. Nó phải giao từng lá thư để thắp lại ngọn Hải Đăng Sao, thứ đã tắt khi hãng hàng không khổng lồ Citadel Air chiếm bầu trời.

**Tên tạm:** `POE: THE LAST MAIL RUN`

Kế hoạch thương mại yêu cầu bỏ chữ "Boeing", nên nhân vật đổi tên từ Poeing thành **Poe**.

---

## 2. Vòng lặp truyện gắn với gameplay (quan trọng nhất)

Mỗi con số trong game phải có nghĩa trong truyện. Dưới đây là cách gắn từng thứ.

| Gameplay | Trong truyện |
|---|---|
| **Sao** (1–3 mỗi màn) | **Thư đã giao.** Mỗi lá thư là một đốm sáng bay về Hải Đăng. |
| Tổng sao | Độ sáng của Hải Đăng. Quyết định **kết thúc thường hay kết thúc vàng**: phải có ≥ 60/75 sao mới xem được kết thúc vàng. Đây là lý do để người chơi cày 3 sao. |
| Coin | Tiền boa của dân làng. |
| Cổng sao giữa các thế giới | "Hải Đăng chưa đủ sáng để soi đường qua bão". Phải giao đủ thư mới đi tiếp được. |
| Rương 10 sao | Gói quà cảm ơn từ dân làng. |
| Bản đồ | Bản đồ tuyến thư của bà Wren, vẽ tay, có vết cà phê. |
| Màn thắng | Dân làng gửi một **bưu thiếp cảm ơn** (1 khung hình, 1 câu). Bưu thiếp được lưu vào bộ sưu tập. |

**Lý do cho từng môn:**

| Môn | Lý do trong truyện |
|---|---|
| Flappy | Vượt hàng rào ống khói mà Citadel dựng chắn các tuyến bay. |
| Xếp thùng (Stack) | Chất hàng cứu trợ lên tàu ở cảng Sunny Sea trước khi thuyền nhổ neo. |
| Né tên lửa | Thoát tên lửa tầm nhiệt của Iron Flock (đàn chim máy của Citadel). |
| Hạ cánh | Đáp xuống những đường băng làng đã bị phá hỏng để giao thư tận tay. |
| Thả hàng (Supply) | Hàng tiếp tế thả dù từ máy bay của Bubba. Poe phải đón trước khi rơi xuống biển. |
| Né sét | Cơn bão sinh ra từ lúc Hải Đăng tắt. Bay qua Storm Peaks. |
| Tiếp dầu | Chặng bay dài tới Sundown, phải nối vòi tiếp dầu với Bubba giữa trời. |
| Boss | Các tướng của Iron Flock canh giữ từng vùng. |

---

## 3. Nhân vật

| Tên | Vai | Tính cách / thiết kế |
|---|---|---|
| **POE** | Người chơi | Máy bay đưa thư cánh quạt, nhỏ, cũ, có mắt trên kính buồng lái kiểu hoạt hình. Không nói, chỉ biểu cảm bằng mắt và tiếng động cơ. Vụng về nhưng không bỏ cuộc. |
| **GRANNY WREN** | Người dẫn truyện, thợ máy | Bà cụ thợ máy, kính bảo hộ, cờ lê trong túi tạp dề. Giọng dẫn truyện của toàn game. Câu cửa miệng: *"STILL GOT SOME SKY IN YOU."* |
| **PIP** | Bồ câu đưa thư, trợ thủ | Mang thư mới, nhắc cách chơi, nói câu đùa ngắn. Sau này sống ở chuồng chim trong Hangar. |
| **BUBBA** | Máy bay tiếp dầu khổng lồ | Hiền, chậm, hay sợ. Xuất hiện từ thế giới 2. Là người thả hàng (Supply) và tiếp dầu (Refuel). |
| **CHAIRMAN GILT** | Phản diện | Chủ tịch Citadel Air: mũ chóp, ria vàng, cười "HA-HA" theo nhịp kèn. Muốn chỉ máy bay của mình được bay. |
| **TEMPEST EAGLE** | Boss 1 | Đại bàng thật bị Citadel đeo vòng cổ điều khiển. Khi bị đánh bại, vòng vỡ và nó được tự do, **trở thành đồng minh**. Đây là khoảnh khắc cảm xúc đầu tiên của game. |
| **IRONWAKE** | Boss 2 | Chiến hạm phong toả cảng Sunny Sea. |
| **SUNDOWN CITADEL** | Boss 3 | Toà tháp pháo đài của Gilt ở Sundown. |
| **THE BLACKOUT** | Boss cuối (mới, Grok thiết kế) | Cỗ máy của Gilt hút sạch ánh sáng khỏi Hải Đăng, nằm ở đỉnh Starlight. |

---

## 4. Cấu trúc truyện theo thế giới

| Thế giới | Chuyện gì xảy ra | Cutscene mở đầu | Sau boss |
|---|---|---|---|
| **1 DAWN VALLEY** | Lá thư đầu tiên: làng cần thuốc. Pip dạy bay. | Pip đáp xuống với lá thư *"PLEASE. WE NEED MEDICINE."* | Vòng cổ của Tempest Eagle vỡ, đại bàng cúi đầu cảm ơn rồi bay đi. Gilt đập bàn qua màn hình theo dõi. |
| **2 SUNNY SEA** | Cảng bị Ironwake phong toả, dân đói. | Bubba trốn dưới mái cảng, run rẩy: *"I... I CAN DROP SUPPLIES. IF YOU CATCH THEM."* | Ironwake chìm. Thuyền cá ra khơi, dân làng vẫy cờ. |
| **3 STORM PEAKS** | Bão dữ vì Hải Đăng mờ dần. | Bà Wren chỉ lên trời: *"THE BEACON IS FADING. THE STORMS ARE WINNING."* | Tempest Eagle quay lại, chở Poe xuyên qua mắt bão (có thể làm thành khung hình bay chung). |
| **4 SUNDOWN** | Đột nhập Citadel. Chặng dài, phải tiếp dầu. | Bubba cùng bay: *"I'LL KEEP YOU FED. YOU KEEP US ALIVE."* | Toà tháp sụp. Gilt trốn lên đỉnh Starlight bằng khinh khí cầu. |
| **5 STARLIGHT** | Leo lên Hải Đăng, đánh bại The Blackout. | Tất cả bạn bè (Pip, Bubba, Eagle) bay hộ tống. | **Kết thúc** (xem bên dưới). |

**Hai kết thúc** (tính theo tổng sao):
- **Thường (< 60 sao):** Hải Đăng sáng lại nhưng chập chờn. Bà Wren: *"IT'S A START. THERE ARE STILL LETTERS OUT THERE."* Hiện gợi ý quay lại cày sao.
- **Vàng (≥ 60 sao):** mọi lá thư bay lên, Hải Đăng bùng sáng vàng, bầu trời đầy máy bay nhỏ cất cánh trở lại. Poe được sơn lại mới tinh. Gilt bị giáng chức làm... nhân viên phân loại thư.
- **Sau credits:** một lá thư lạ rơi xuống, đóng dấu từ một vùng đất chưa có trên bản đồ. Đây là móc cho bản cập nhật sau.

---

## 5. Kịch bản intro (khoảng 75 giây, giữ Space để bỏ qua)

Chỉ phát **lần đầu mở game**. Sau đó có thể xem lại trong menu.

| # | Hình | Chữ trên màn (PixelText) | Âm thanh / chuyển cảnh |
|---|---|---|---|
| 1 | Đen, vệt xước phim | — | Tiếng máy chiếu, iris mở |
| 2 | Toàn cảnh quần đảo trên mây, máy bay nhỏ bay khắp nơi, Hải Đăng sáng trên đỉnh cao nhất | `ONCE THE SKYWARD ISLES WERE JOINED BY A THOUSAND LETTERS A DAY.` | Nhạc dawn, camera trượt chậm |
| 3 | Cận cảnh những lá thư phát sáng bay về Hải Đăng | `EVERY LETTER WAS A LIGHT.` | Tiếng "score" lấp lánh |
| 4 | Toà tháp Citadel lúc hoàng hôn, bóng Gilt trên cửa sổ | `BUT CITADEL AIR WANTED THE SKY FOR ITSELF.` | Nhạc tối lại |
| 5 | Iron Flock xuất xưởng, máy bay thư bị bắn rơi, Hải Đăng mờ dần | `THE IRON FLOCK GROUNDED EVERY LITTLE PLANE.` | thunder, explosion, rung camera |
| 6 | Hangar nhỏ trong mưa. Bà Wren kéo tấm bạt khỏi Poe phủ bụi | `ALL BUT ONE.` | Tiếng mưa, tiếng bạt |
| 7 | Bà Wren nhìn Poe | `YOU'RE ALL WE'VE GOT. OLD BOY.` | — |
| 8 | Pip đáp xuống cánh Poe, ngậm một lá thư | `PLEASE. WE NEED MEDICINE. - DAWN VALLEY` | Tiếng "flap" |
| 9 | Động cơ Poe ho khục khục, nổ lụp bụp, rồi gầm lên. Mắt Poe mở to | — | Cần 3 nhịp: ho, im, gầm (`ready` → `wallop`) |
| 10 | Thẻ tên game `POE: THE LAST MAIL RUN` | — | `card` + `super`, chữ nảy |
| 11 | Iris đóng lại, mở ra bản đồ, máy bay đậu ở màn 1 | — | Nhạc bản đồ |

Ghi chú:
- Câu 7 dùng dấu chấm thay dấu phẩy vì PixelText không có dấu phẩy. Nếu Grok thêm ký tự `,` vào font thì đổi lại.
- **Video intro thật (để đăng store / YouTube):** quay chính cutscene này trong engine bằng **Unity Recorder**, xuất ra `Builds/trailer/intro.mp4` ở 1920×1080, 60fps. Trong game không phát file mp4, vì phát trong engine nhẹ hơn và giữ đúng bộ lọc phim.

**Các cutscene khác:**
- **Mở đầu mỗi thế giới:** 3–4 khung hình, khoảng 20 giây, phát trước màn đầu tiên của thế giới đó.
- **Trước boss:** thẻ tên boss kiểu Cuphead, ví dụ `TEMPEST EAGLE - THE CAGED KING`, kèm 1 câu khiêu khích của Gilt.
- **Sau boss:** 2–3 khung hình như bảng ở mục 4.
- **Kết thúc:** 2 phiên bản như mục 4, cộng cảnh sau credits.
- **Bưu thiếp sau mỗi màn thắng lần đầu:** 1 khung hình nhỏ hiện trên màn hình kết quả. Ví dụ màn 1: `THANK YOU POE! - THE MILLER FAMILY`.

---

## 6. Đặc tả kỹ thuật

Phải tuân thủ luật ở mục D của `docs/HUONG_DAN_GROK_LEVELS.md`: một scene, dựng mọi thứ bằng code, PixelText không chữ thường, không dấu phẩy.

**Dữ liệu: `Levels/CutsceneDB.cs`**
- `class Shot`:
  - `string art`: tên sprite trong `Assets/_Game/Art/story/`
  - `string text`
  - `float dur`
  - `Vector2 panFrom, panTo`
  - `float zoomFrom, zoomTo`
  - `string sfx`, `string music`
  - `Transition inT, outT`, với `Transition` là `Cut`, `Iris`, `Fade` hoặc `PageTurn`
  - `Action custom`: tuỳ chọn, cho cảnh dùng sprite thật đang chuyển động (Poe bay, Eagle vỗ cánh)
- `class Cutscene { string id; Shot[] shots; }`
- Các id: `intro`, `world1`..`world5`, `boss0_in`, `boss0_out`, …, `ending_normal`, `ending_gold`, `postcredits`.

**Trình phát: `Levels/CutscenePlayer.cs`** (MonoBehaviour, tạo giống LevelRuntime)
- Hiện ảnh bằng `Ui.Spr` và chạy hiệu ứng Ken Burns (trượt và zoom chậm).
- Chữ chạy từng ký tự (typewriter) ở dưới, nền là `Ui.Plaque`.
- Chuyển cảnh bằng `FilmFilter.IrisSet`.
- **Giữ Space hoặc giữ chạm 0.6 giây để bỏ qua**, có vòng tròn tiến độ. Bấm một lần thì hiện hết chữ của khung hiện tại hoặc sang khung tiếp.
- `static void Play(string id, System.Action onDone)`.

**Save:** thêm `List<string> seenCutscenes` vào SaveData và tăng `version` lên 3. Migrate phải xử lý trường hợp null.

**Móc vào luồng game (`LevelFlow`):**
- Lần đầu boot, chưa xem `intro` → phát intro rồi vào bản đồ.
- `Play(id)` khi id là màn đầu của thế giới mà chưa xem `worldN` → phát cutscene trước.
- Màn boss: phát `bossN_in` trước khi đánh. Thắng lần đầu → phát `bossN_out` trước màn hình kết quả.
- Thắng màn 25 → phát kết thúc theo tổng sao, rồi `postcredits`.
- Thêm nút `STORY` trên bản đồ để xem lại các cutscene đã mở.

**Bộ sưu tập bưu thiếp:** lưu các id bưu thiếp đã nhận. Hiện thành lưới trong nút `POSTCARDS` trên bản đồ, sau này chuyển lên tường Hangar.

**Không phá test:** `MiniGame.Bot = true` hoặc một cờ `CutscenePlayer.SkipAll` phải bỏ qua hết cutscene.

---

## 7. Danh sách ảnh cần vẽ (Grok tạo)

Yêu cầu chung:
- Pixel-art **480×270** (đúng tỉ lệ màn hình, 32 PPU), cùng bảng màu và nét viền đậm của game hiện tại.
- Phong cách tranh truyện thập niên 30: màu hơi ngả sepia, nhân vật mắt to.
- Lưu vào `Assets/_Game/Art/story/`, import theo `Assets/_Game/Art/UNITY_IMPORT.txt`.

| Nhóm | Số ảnh | Gồm |
|---|---|---|
| Intro | 9 | Quần đảo, lá thư sáng, tháp Citadel, Iron Flock, hangar trong mưa, bà Wren kéo bạt, Pip đáp xuống, Poe tỉnh dậy, logo game |
| Mở đầu thế giới | 5 × 3 | Theo bảng ở mục 4 |
| Sau boss | 4 × 2 | Eagle được tự do, Ironwake chìm, tháp sụp, đánh bại Blackout |
| Kết thúc | 2 × 3 + 1 | Thường, vàng, sau credits |
| Chân dung nhân vật | 5 | Poe (4 biểu cảm: vui, sợ, quyết tâm, mệt), Wren, Pip, Bubba, Gilt, để làm khung thoại |
| Bưu thiếp | 25 | Ảnh nhỏ 160×100 cho mỗi màn |
| Redesign Poe | 1 sheet | Máy bay đưa thư cánh quạt có mắt, xa thiết kế Boeing. Thay sprite người chơi hiện tại. |

---

## 8. Tiêu chí nghiệm thu

- Mở game lần đầu: intro phát hết khoảng 75 giây và bỏ qua được. Lần thứ hai vào thẳng bản đồ.
- Vào màn 6 lần đầu: cutscene thế giới 2 phát. Lần sau không phát.
- Đánh boss 1: có thẻ tên trước trận. Thắng xong thấy cảnh Eagle được tự do.
- Mỗi màn thắng lần đầu có bưu thiếp, và bưu thiếp xuất hiện trong bộ sưu tập.
- Màn 25 với dưới 60 sao ra kết thúc thường. Sửa save thành ≥ 60 sao thì ra kết thúc vàng.
- Bot test chạy được hết các màn mà không kẹt ở cutscene.
- Có file `Builds/trailer/intro.mp4`.
- `csc_check` sạch, Console không lỗi đỏ, có ảnh chụp từng loại cutscene.

---

## 9. Chỗ để Grok tự sáng tạo

Được đổi bất cứ thứ gì ở trên **nếu làm hay hơn**, miễn giữ 3 trụ cột:
1. Sao = thư = ánh sáng của Hải Đăng.
2. Mỗi môn có lý do trong truyện.
3. Có hai kết thúc theo số sao.

Gợi ý mở thêm:
- Thiết kế boss cuối The Blackout: cơ chế mới, ví dụ tắt dần ánh sáng màn hình.
- Thêm câu thoại hài cho Pip trước mỗi màn (1 dòng, tối đa khoảng 40 ký tự).
- Cảnh Poe và Eagle bay chung trong thế giới 3, làm thành một màn chơi mới.
- Nhạc chủ đề riêng cho intro.
- Một cảnh "cú twist" giữa game: Gilt từng là phi công đưa thư bị sa thải. Thêm cảnh này nếu thấy làm phản diện có chiều sâu hơn.

Ghi mọi quyết định vào mục E của `docs/HUONG_DAN_GROK_LEVELS.md`.
