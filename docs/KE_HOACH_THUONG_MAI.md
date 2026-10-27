# Kế hoạch thương mại hoá — Flappy Poeing → "Sky Arcade" (tên tạm)

Ngày lập: 2026-10-16 · Engine: Unity 6000.6 · Trạng thái hiện tại: commit `c73dad0`

---

## 0. Hiện trạng dự án (đã đọc code)

| Hạng mục | Hiện có |
|---|---|
| Luồng chơi | 1 scene `Main`. Flappy (một chạm) tới 30 điểm → iris đóng → Boss Rush WASD với 3 boss × 3 phase (Citadel, Tempest Eagle, Ironwake) → VICTORY |
| Cảm giác chơi (juice) | Rất tốt: hit-stop, rung màn hình, film filter, iris, phát thanh READY?/WALLOP!/KNOCKOUT, parry, EX/Super cards, dash |
| Hình ảnh | Pixel art 16-bit + filter phim cũ kiểu Cuphead, 4 biome (dawn/sea/storm/dusk) |
| Code | ~4.700 dòng, sạch, tách nhỏ (BossBase, BulletPool, Routine, Telegraph…). Có bot AutoPilot + `combat_test.py` để test tự động |
| Thiếu để bán | Menu, lobby, save game (chỉ có `PlayerPrefs best`), tiền tệ, tiến trình meta, settings, điều khiển cảm ứng cho boss, localization, analytics, ads/IAP, nhạc thật |
| Rủi ro pháp lý | **"Boeing"** là nhãn hiệu đã đăng ký. **"WALLOP!"** là câu thoại đặc trưng của Cuphead. Ảnh AI (Grok) phải khai báo trên Steam và kiểm tra điều khoản thương mại |

**Kết luận:** phần lõi (game feel) đã đạt chất lượng thương mại. Thứ còn thiếu là **lý do để quay lại chơi**: lobby, tiến trình, sưu tầm, nhiệm vụ hằng ngày và nhiều mini game.

---

## 1. Định vị sản phẩm

**Một câu:** *"Một sân bay pixel ấm cúng, nơi mỗi chiếc máy bay là một mini game arcade 1–3 phút, và mọi thứ bạn kiếm được đều làm sân bay của bạn đẹp và mạnh hơn."*

- **Thể loại:** tuyển tập arcade (arcade collection) + hub meta kiểu Soul Knight.
- **Nền tảng đề xuất:** **Mobile trước** (Android → iOS), màn ngang, F2P. Sau đó lên **Steam** dạng premium giá $4.99, bỏ quảng cáo.
  - Lý do: mini game ngắn hợp với mobile; Flappy vốn là game một chạm; Soul Knight cũng thành công nhờ mobile.
- **Đối tượng:** 13–30 tuổi, thích game pixel, chơi lúc rảnh 5–15 phút.
- **Đổi tên (bắt buộc):** bỏ "Boeing" và "Flappy Bird". Gợi ý: *Sky Arcade*, *Hangar Heroes*, *Pocket Airfield*, *Propeller Party*. Nhân vật máy bay có thể vẫn tên "Poeing" nhưng đổi thiết kế cho xa Boeing (nên đổi luôn).
- **Đổi call-out:** thay WALLOP! bằng câu riêng, ví dụ "TAKE OFF!" / "KNOCKOUT" → "SHOT DOWN!".

### 3 trụ cột thiết kế
1. **Vào chơi trong 5 giây.** Từ lobby bước tới máy bay là bay, không qua menu.
2. **Mỗi lần chơi đều tiến lên.** Thua vẫn có xu, vật liệu và tiến độ nhiệm vụ.
3. **Lobby là đồ chơi.** Mọi hệ thống là một đồ vật đi tới tương tác được, không phải nút UI.

---

## 2. Học từ lobby Soul Knight

### Soul Knight làm gì hay
| Thành phần lobby Soul Knight | Vì sao hiệu quả |
|---|---|
| Nhà là một màn chơi điều khiển được (cùng cần gạt, cùng nút) | Không dạy lại điều khiển, lobby cũng là chỗ thử súng |
| Các hero **ngồi rải rác trong nhà**; đi tới để chọn, hero khoá hiện giá | Bày nội dung khoá ra trước mắt, kích thích muốn mở |
| **Vườn**: trồng hạt, thu hoạch theo giờ thực | Lý do mở game nhiều lần trong ngày |
| **Bàn rèn**: chế vũ khí từ vật liệu nhặt trong run | Biến vật phẩm rơi thành mục tiêu dài hạn |
| **Tượng** trả gem để nhận buff đầu run | Chỗ tiêu tiền mềm, tăng sức mạnh nhẹ |
| **Máy gacha**, bể cá, thú cưng, đồ trang trí, bí mật ẩn | Sưu tầm, cá nhân hoá, khám phá |
| **Cổng/cửa vào run** ở giữa nhà | Bắt đầu chơi cực nhanh |
| Ghế sofa co-op, bảng sự kiện, trang trí theo mùa | Xã hội + live ops dễ làm (chỉ cần đổi decor) |

### Áp dụng thành **Hangar (Sân bay)**
Người chơi điều khiển một **phi công chibi** đi lại trong hangar, dùng cùng cần gạt với Boss Rush.

```
 ┌───────────────────────────── HANGAR ─────────────────────────────┐
 │  [Tháp radar]        [Bảng nhiệm vụ]        [Máy gacha capsule]   │
 │   = Daily/Event        = 3 daily + weekly      = skin, pet, decor │
 │                                                                   │
 │  [Xưởng]       ✈ Poeing   ✈ Gunship   ✈ Glider(khoá)  ✈ ???      │
 │   = nâng cấp      ── dãy máy bay đỗ = chọn máy bay ──             │
 │     & chế đồ                                                      │
 │                                                                   │
 │  [Chuồng chim]   ════════ ĐƯỜNG BĂNG / CÁC CỔNG MINI GAME ═══════ │
 │   = vườn: ấp       (1) Flappy  (2) Boss Rush  (3) Missile  (4)... │
 │     trứng theo giờ                                                │
 │  [Tủ cúp]  [Tượng phi công = buff]  [Sofa co-op]  [Thùng thư]     │
 └───────────────────────────────────────────────────────────────────┘
```

| Đồ vật trong Hangar | Tương ứng Soul Knight | Chức năng |
|---|---|---|
| **Dãy máy bay đỗ** | Hero ngồi trong nhà | Đi tới để chọn máy bay. Mỗi máy bay có **1 kỹ năng bị động dùng chung mọi mini game** (vd. +1 HP, khiên 1 lần, nam châm xu) |
| **Đường băng có cổng số** | Cổng vào run | Mỗi cổng là một mini game; cổng khoá hiện điều kiện mở |
| **Xưởng (Workshop)** | Bàn rèn | Dùng vật liệu chế đạn/bộ phận cho Boss Rush, nâng cấp máy bay |
| **Chuồng chim** | Vườn | Ấp trứng chim theo giờ thực → nở ra thú cưng bay theo hoặc vật liệu |
| **Tượng phi công** | Tượng buff | Trả xu để có buff một lần cho lượt chơi tiếp |
| **Máy gacha capsule** | Máy gacha | Chỉ cosmetic: skin, vệt khói, thú cưng, đồ trang trí |
| **Tủ cúp** | Bảng thành tựu | Cúp vật lý cho mỗi boss/grade S, mỗi kỷ lục |
| **Tháp radar** | Bảng sự kiện | Thử thách hằng ngày (seed cố định), sự kiện tuần |
| **Thùng thư** | — | Quà đăng nhập, tin cập nhật |
| **Sofa co-op** | Sofa multiplayer | Giai đoạn sau: co-op 2 người cùng máy (Boss Rush) |
| **Trang trí** | Decor theo mùa | Tết, Halloween, Noel: chỉ đổi sprite, chi phí thấp |

**Quy tắc vàng:** lần đầu mở game, **thả thẳng vào Flappy** (không menu). Sau lần chết đầu tiên, iris mở ra Hangar với một mũi tên chỉ cổng Boss Rush. Hệ thống mới mở dần theo cấp phi công, mỗi lần chỉ một hệ thống.

---

## 3. Danh mục mini game

Tiêu chí chọn: game gốc **đã được chứng minh thành công**, **chơi 1–3 phút**, **điều khiển 1 chạm hoặc 1 cần gạt**, **dùng lại tối đa code/asset hiện có** (BulletPool, Telegraph, Routine, biome, AutoPilot).

### Giữ lại và nâng cấp

#### (1) Flappy — *giữ* (cảm hứng: Flappy Bird)
- Tách thành mode **endless** độc lập, không còn bị khoá ở mốc 30 điểm.
- Thêm: **xu trong khe** (rủi ro / phần thưởng), **near-miss bonus** (sát chim → x2 điểm), power-up hiếm (khiên, nam châm, slow-mo), đổi biome mỗi 25 điểm, **seed hằng ngày** cho bảng xếp hạng.
- Kỹ năng máy bay áp vào: Gunship được bắn 3 phát để phá đàn chim, v.v.

#### (2) Boss Rush — *giữ* (cảm hứng: Cuphead)
- Chọn boss từng con (mở khoá dần), thêm 3 độ khó **Simple / Regular / Expert**. Expert cho cúp và skin.
- **Loadout:** 1 vũ khí chính + 1 EX + 1 "charm" chế ở Xưởng (học Cuphead: bán vũ khí/charm trong shop).
- Điểm grade (đã có hệ grade A+/B…) → phần thưởng theo grade.
- **Điều khiển cảm ứng:** cần gạt trái, nút Bắn (giữ, có auto-fire tuỳ chọn), Dash, EX bên phải.
- Live ops: **thêm 1 boss mỗi mùa** (6–8 tuần).

### Mini game mới (xếp theo ưu tiên)

| # | Tên | Cảm hứng (thành công) | Lõi gameplay | Điều khiển | Dùng lại | Công sức |
|---|---|---|---|---|---|---|
| 3 | **Missile Dodge** | *Go Plane / Missiles!* (Ketchapp) | Máy bay lượn tròn, tên lửa tầm nhiệt đuổi theo; dụ chúng đâm nhau để ghi điểm, nhặt sao | Giữ trái/phải để rẽ | PlayerShot, nổ, CameraFx, biome | **S** (1–1,5 tuần) |
| 4 | **Sky Survivors** | *Vampire Survivors / Brotato* | Tự động bắn, sóng quái bay tới, nhặt XP, lên cấp chọn 1 trong 3 nâng cấp, sống sót 5 phút, boss cuối | Cần gạt di chuyển | BulletPool, Minion, Health, boss phase | **M** (2–3 tuần). Tiềm năng giữ chân cao nhất |
| 5 | **Cargo Stack** | *Stack / Tower Bloxx* | Thả thùng hàng từ trực thăng lên boong tàu đang lắc lư, chồng càng cao càng nhiều xu; đặt trúng hoàn hảo thì combo | 1 chạm | Biome biển Ironwake | **S** (1 tuần) |
| 6 | **Perfect Landing** | *Desert Golfing / Alto / "Landing" games* | Hạ cánh bằng điều chỉnh góc và ga trong gió; chấm điểm êm và chính xác; 3 sao mỗi màn | Giữ để kéo mũi lên, thả để hạ | Physics, biome storm (gió) | **M** (2 tuần), có màn chơi thủ công |
| 7 | **Micro Mix** | *WarioWare* | Chuỗi micro game 5 giây ngẫu nhiên (tránh 1 viên đạn, chạm đúng 1 chim, parry 1 viên hồng…) nhanh dần, 4 mạng | 1 chạm / vuốt | Mảnh nhỏ cắt từ mọi game khác | **M**, nhưng mỗi micro game rẻ. Là mode "tổng hợp" làm thương hiệu |
| 8 | **Sky Race** (sau launch) | *Tiny Wings / Alto's Odyssey* | Lướt theo sườn đồi mây, tích đà, so ghost với bạn bè | Giữ để lao xuống | Biome dawn | **M** |
| 9 | **Rhythm Flight** (sau launch) | *Rhythm Heaven / Geometry Dash* | Bay theo nhịp nhạc, chướng ngại theo beat | 1 chạm | Đội nhạc cần nhạc thật | **L** |

**Launch với 5 game:** Flappy, Boss Rush, Missile Dodge, Cargo Stack, Sky Survivors. Micro Mix ra ở bản cập nhật lớn đầu tiên (~1 tháng sau launch), sau đó 1 mini game hoặc 1 boss mỗi mùa.

### Nguyên tắc để mỗi mini game "hay"
1. **3 giây đầu đã vui:** không có tutorial dạng chữ; nhân vật làm mẫu bằng một bóng ma (ghost) 1 lần.
2. **Thua vì lỗi mình:** hitbox nhỏ hơn sprite, có cảnh báo (Telegraph/WarnLane đã có) trước mọi đòn.
3. **"Một lần nữa":** nút chơi lại to, load < 1 giây, hiện ngay "còn 3 điểm nữa là kỷ lục".
4. **Độ sâu ẩn:** mỗi game có 1 kỹ năng cao cấp (near-miss, perfect stack, parry chain, missile combo) cho người chơi giỏi.
5. **Chung một nền:** cùng hệ juice, cùng tiền tệ, cùng nhiệm vụ, cùng máy bay. Đây là thứ phân biệt chúng ta với một app gom game rời rạc.

---

## 4. Vòng lặp meta và kinh tế

```
 Chơi mini game ──► Xu + Vật liệu + XP phi công + tiến độ nhiệm vụ
        ▲                               │
        │                               ▼
 Máy bay/charm mới ◄── Hangar: Xưởng, gacha, tượng, chuồng chim, mở cổng mới
```

| Tiền / tài nguyên | Kiếm từ | Tiêu vào |
|---|---|---|
| **Xu** (mềm) | Mọi mini game, nhiệm vụ | Nâng cấp máy bay, tượng buff, lượt gacha |
| **Vật liệu** (bánh răng, pin, lông chim…) | Mỗi game rơi một loại riêng → buộc chơi đa dạng | Chế charm/vũ khí ở Xưởng |
| **Kim cương** (cứng) | Nhiệm vụ tuần, thành tựu, IAP | Mở máy bay sớm, skin cao cấp, tăng tốc ấp trứng |
| **XP phi công** | Mọi game | Mở cổng mini game và hệ thống mới theo cấp |

- **Nhiệm vụ ngày:** 3 nhiệm vụ trải trên các game khác nhau ("Parry 5 lần", "Stack 15 thùng", "Né 20 tên lửa").
- **Thử thách radar:** seed cố định toàn cầu mỗi ngày, có bảng xếp hạng.
- **Không có hệ năng lượng (energy).** Người chơi game arcade ghét bị chặn.

### Kiếm tiền (công bằng, kiểu Soul Knight)
| Nguồn | Chi tiết |
|---|---|
| Rewarded ads (chủ động xem) | Hồi sinh 1 lần/lượt, x2 xu cuối lượt, thêm 1 lượt gacha/ngày |
| Interstitial | Tối đa 1 lần / 3 phút, **không bao giờ** sau lần chơi đầu trong phiên. Mua "No Ads" là tắt |
| IAP | No Ads $2.99 · Starter Pack $1.99 (máy bay + kim cương) · gói máy bay · skin |
| Season Pass (từ mùa 2) | $4.99, chỉ cosmetic + vật liệu; track miễn phí vẫn đáng chơi |
| Steam | Premium $4.99, không ads, toàn bộ cosmetic mở bằng chơi |

Công cụ: Unity LevelPlay (ads), Unity IAP, Unity Analytics hoặc GameAnalytics, Remote Config để chỉnh giá và độ khó không cần cập nhật app.

---

## 5. Thay đổi kỹ thuật

### Kiến trúc mới
```
Boot ──► Hangar (lobby) ──► MiniGame scene (additive hoặc LoadScene)
                 ▲                 │
                 └── ResultScreen ◄┘   (RunResult: điểm, xu, vật liệu, nhiệm vụ)
```
- **`IMiniGame`**: `Id`, `Begin(Loadout)`, sự kiện `Finished(RunResult)`. Mỗi game là một scene riêng + 1 `MiniGameDef` (ScriptableObject: tên, icon, cấp mở, loại vật liệu rơi).
- **Tách `Main` hiện tại:** `GameState` đang gộp Flappy và Combat (`CombatScore = 30`). Tách thành `FlappyMode` và `BossRushMode`. Giữ mode **"Classic"** (Flappy → Boss) như một cổng riêng cho người thích luồng cũ.
- **`SaveSystem`**: JSON có version + migration, ghi atomic, chuẩn bị cloud save (Unity Cloud Save / Google Play Games).
- **`Economy`**, **`MissionService`**, **`ProgressionService`**: thuần C#, có unit test.
- **Input:** chuyển sang Input System, thêm lớp touch (cần gạt ảo + nút), giữ bàn phím và gamepad cho Steam.
- **Bot:** mở rộng AutoPilot để mỗi mini game có bot riêng → test smoke tự động + đo độ khó (tỉ lệ thắng, thời gian sống) trước mỗi bản build.
- **Hiệu năng mobile:** đo FilmFilter trên máy Android tầm thấp; có tuỳ chọn tắt grain/CRT. Mục tiêu 60 FPS trên máy 3 năm tuổi.
- **Localization:** Tiếng Việt + Tiếng Anh từ đầu (PixelText cần bảng glyph có dấu tiếng Việt).
- **Settings:** âm lượng, rung, tắt filter, chế độ trợ năng (giảm nháy, đạn tương phản cao), tay trái/phải.

### Nội dung và pháp lý
- **Nhạc:** nhạc tự tổng hợp hiện tại đủ cho SFX. Nhạc nền nên mua license hoặc thuê (ngân sách $300–1.500 cho 6–8 bài).
- **Art AI:** lưu lại prompt/nguồn của từng asset; Steam yêu cầu khai báo nội dung AI; với asset chủ chốt (máy bay, boss, icon app) nên cho artist vẽ lại hoặc tinh chỉnh tay.
- **Tên + logo + icon:** tra nhãn hiệu (USPTO/WIPO, Cục SHTT VN) trước khi chốt.

---

## 6. Lộ trình (1 dev + AI, ước tính)

| Giai đoạn | Thời lượng | Nội dung | Tiêu chí xong |
|---|---|---|---|
| **P0 Nền móng** | 2 tuần | Đổi tên; tách scene; `IMiniGame`, SaveSystem, Economy; Input System + touch; Settings | Flappy và Boss Rush chạy độc lập từ menu tạm; save/load qua khởi động lại |
| **P1 Hangar vertical slice** | 3 tuần | Scene Hangar, phi công đi lại, dãy máy bay chọn được, 2 cổng, màn kết quả, xu | Vòng lặp chơi → xu → mua máy bay thứ 2 chạy trọn vẹn; quay video 30 giây cho người lạ xem |
| **P2 Meta** | 3 tuần | Xưởng + charm, nhiệm vụ ngày, chuồng chim, tượng, gacha cosmetic, tủ cúp, FTUE | Người test mới hiểu được Hangar trong 2 phút mà không cần giải thích |
| **P3 Mini game mới** | 5 tuần | Missile Dodge (1,5) → Cargo Stack (1) → Sky Survivors (2,5) | Mỗi game có bot test, 3 nhiệm vụ, 1 loại vật liệu, bảng điểm |
| **P4 Thương mại** | 2 tuần | Ads, IAP, analytics + funnel, Remote Config, localization VI/EN, icon/store page | Build release ký số, qua checklist chính sách Google Play |
| **P5 Soft launch** | 4–6 tuần | Phát hành 1–2 nước (VN/Philippines); chỉnh theo số liệu | Đạt KPI bên dưới, hoặc biết rõ vì sao chưa đạt |
| **P6 Global + Live ops** | liên tục | iOS, Steam; mỗi 6–8 tuần một mùa: 1 boss hoặc 1 mini game + decor + pass | — |

**Tổng đến soft launch: ~15 tuần.**

### KPI soft launch (mục tiêu cho casual/arcade)
| Chỉ số | Mục tiêu |
|---|---|
| Retention D1 / D7 / D30 | ≥ 35% / ≥ 12% / ≥ 4% |
| Thời gian chơi mỗi ngày | ≥ 12 phút |
| Số mini game khác nhau / người / ngày | ≥ 2,5 (đo xem collection có phát huy không) |
| Tỉ lệ xem rewarded ad | ≥ 30% DAU |
| Crash-free | ≥ 99,5% |
| CPI thử nghiệm (VN) | < $0,30 |

---

## 7. Rủi ro và cách giảm

| Rủi ro | Giảm thiểu |
|---|---|
| Tuyển tập mini game bị xem là "app rác gom game" | Hangar + máy bay dùng chung + tiền tệ chung = một thế giới thống nhất; art và juice đồng bộ |
| Phạm vi quá lớn cho 1 người | Launch 5 game, cắt Perfect Landing/Rhythm sang sau; dùng lại hệ thống tối đa |
| Boss Rush khó trên cảm ứng | Auto-fire, hitbox rộng hơn ở Simple, test với 5 người thật trên điện thoại ở P1 |
| Nhãn hiệu (Boeing, Flappy Bird, Cuphead) | Đổi tên, call-out và thiết kế máy bay ngay ở P0 |
| Filter phim nặng trên máy yếu | Tuỳ chọn chất lượng, tự hạ khi FPS < 50 |
| Kiếm tiền quá đà làm mất người chơi | Không energy, ads chủ động là chính, cosmetic-first |

---

## 8. Việc làm ngay tuần này
1. Chốt **tên mới** và **nền tảng** (mobile-first hay Steam-first).
2. Tạo nhánh `feature/foundation`: tách `GameState` thành Flappy/BossRush, dựng `IMiniGame` + `SaveSystem`.
3. Phác Hangar bằng grey-box (hình khối xám) để thử cảm giác đi lại trước khi vẽ art.
4. Prototype **Missile Dodge** trong 2 ngày để kiểm chứng pipeline thêm mini game mới.
