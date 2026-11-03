# Kế hoạch: đăng nhập Firebase bắt buộc + menu động

## A. Đăng nhập Firebase (bắt buộc trước menu)

- Dùng **Firebase Auth REST API** (identitytoolkit.googleapis.com) qua `UnityWebRequest`. Không cần Firebase Unity SDK, không tải package nào.
- Tài khoản = tên + mật khẩu như hiện tại. Tên được đổi thành email ảo `ten@flappyboeing.app` để gửi lên Firebase (Email/Password provider).
  - Đăng ký: `accounts:signUp?key=API_KEY`
  - Đăng nhập: `accounts:signInWithPassword?key=API_KEY`
  - Giữ phiên: lưu `refreshToken` (không lưu mật khẩu), làm mới bằng `securetoken.googleapis.com/v1/token?key=API_KEY` khi mở game.
- Cấu hình: `Assets/Resources/firebase.json` → `{ "apiKey": "..." }`. File này nằm trong `.gitignore`. Repo chỉ có `firebase.example.json`.
- Luồng mới: mở game → **màn LOGIN** (thay cho FrontMenu khi chưa đăng nhập) → đăng nhập/đăng ký thành công → FrontMenu. Nút trên menu chỉ còn `HI TEN` / LOG OUT; đăng xuất thì quay về màn LOGIN.
- Lỗi mạng hoặc lỗi Firebase hiện bằng chữ đỏ (EMAIL_EXISTS → NAME ALREADY TAKEN, INVALID_LOGIN_CREDENTIALS → WRONG NAME OR PASSWORD, mất mạng → NO CONNECTION).
- `Accounts.cs` chuyển thành lớp bọc Firebase: bỏ hash PBKDF2 cục bộ, `accounts.json` chỉ còn `{current, refreshToken, uid}`.

### Bạn cần chuẩn bị (trước khi test thật)
1. Vào https://console.firebase.google.com → tạo project (ví dụ `flappy-boeing`).
2. Build → Authentication → Get started → bật **Email/Password**.
3. Project settings → General → Add app → **Web** → chép **Web API Key** (`AIza...`).
4. Dán vào chat hoặc tự tạo `Assets/Resources/firebase.json`.

Web API Key của Firebase không phải bí mật tuyệt đối (client nào cũng có), nhưng vẫn để ngoài git.

## B. Menu động
- Tiêu đề rơi xuống + nảy, nút trượt vào lần lượt (stagger), nút phồng nhẹ khi rê chuột/nhấn.
- Nền: mây trôi parallax, máy bay Poe bay lượn qua lại theo đường sin.
- Panel mở/đóng bằng scale-pop thay vì hiện tức thì.
- Áp dụng cho cả màn LOGIN.
- Tất cả bằng code (Update + easing), không cần asset mới.
