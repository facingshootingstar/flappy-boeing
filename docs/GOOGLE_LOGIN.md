# Đăng nhập Google

Mục đích duy nhất là hiện tên người chơi ở menu ("HI TEN"). Không lưu lên đám mây, không có bảng xếp hạng.

## Đã có trong code

- `Assets/_Game/Scripts/GoogleLogin.cs`
  - Trên Android: đăng nhập qua Google Play Games (bản v2).
  - Trong Unity Editor: giả lập một tên ("EDITOR PILOT") để thử giao diện.
  - Trên nền tảng khác: nút bị ẩn.
- **Làm sạch tên:** bỏ dấu tiếng Việt, viết hoa, cắt còn 14 ký tự vì font pixel chỉ có A–Z, 0–9 và vài dấu câu. Ví dụ "Nguyễn Văn Đức" thành "NGUYEN VAN DUC".
- `FrontMenu.cs`
  - Góc trên bên trái có nút SIGN IN WITH GOOGLE.
  - Đăng nhập xong, nút đổi thành HI TEN.
  - Khi vào game, máy tự đăng nhập lặng lẽ nếu người chơi từng đăng nhập.

## Việc bạn phải tự làm (cần tài khoản của bạn)

1. **Tài khoản Google Play Console:** đăng ký tại play.google.com/console, phí 25 USD một lần.
2. **Tạo app** trong Play Console, rồi vào **Play Games Services → Setup**. Bước này tạo một project trên Google Cloud.
3. **Tạo OAuth client kiểu Android**, dùng đúng:
   - package name, ví dụ `com.tencongty.flappyboeing`, phải khớp với Player Settings → Android → Package Name;
   - SHA-1 của keystore dùng để ký bản build.
4. **Cài plugin** Google Play Games for Unity: bản `.unitypackage` mới nhất ở github.com/playgameservices/play-games-plugin-for-unity → `current-build`. Mở bằng Assets → Import Package.
5. **Dán cấu hình:** trong Unity chọn Window → Google Play Games → Setup → Android setup, rồi dán đoạn XML "resources" lấy từ Play Console.
6. **Bật code:** Project Settings → Player → Android → Scripting Define Symbols, thêm `GPGS`. Chưa thêm thì phần Play Games không được biên dịch, game vẫn chạy bình thường.
7. **Thêm tester:** trong Play Console → Play Games Services → Testers, thêm email của bạn để đăng nhập được khi app chưa phát hành.
8. **Chuyển build target sang Android** (File → Build Profiles) và build thử lên điện thoại.

## Lưu ý

- Trước khi build Android, đổi `Company Name` và `Package Name`. Hiện chúng vẫn là `DefaultCompany`.
- Muốn bản PC (Steam) cũng đăng nhập Google thì phải đổi cách làm sang Unity Authentication. Bản này chưa hỗ trợ.
