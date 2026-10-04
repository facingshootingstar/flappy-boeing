# Kế hoạch dùng Antigravity CLI (agy) để tiết kiệm token Claude

**Nguyên tắc:** `agy` (Gemini 3.8 Flash) làm "mắt", đi đọc và thu thập thông tin. Claude làm "tay", chỉ đọc bản tóm tắt rồi sửa code, chạy Unity và kiểm chứng.

## Cách gọi
```
python tools/agy_ask.py "<câu hỏi>" [-n số_dòng_tối_đa] [-m model] [-t giây]
python tools/agy_ask.py -f prompt.txt
```
- Script tự gắn bối cảnh project và luật chỉ đọc, giới hạn số dòng trả lời, bỏ link `file:///`. Câu trả lời gốc được lưu ở `tools/.agy_logs/`.
- Model mặc định là `gemini-3.8-flash-medium`. Việc khó thì dùng `-m gemini-3.1-pro-high`.
- Quyền nằm trong `~/.gemini/antigravity-cli/settings.json` (bản gốc ở `settings.json.bak`):
  - Cho phép: đọc file, đọc web, các lệnh chỉ đọc (`ls`, `rg`, `grep`, `Get-Content`, `git log`…).
  - Cấm: ghi file, xoá, di chuyển, `>`/`Set-Content`, `npm`/`pip`/`curl`, `git commit`/`push`.
  - Đã thử: agy không tạo được file nào.

## Việc giao cho agy (Claude không tự đọc)
| Loại | Ví dụ câu hỏi |
|---|---|
| Tìm code | "Hàm nào xử lý parry, ở path:line nào, gọi từ đâu?" |
| Tóm tắt file dài | "Tóm tắt luồng phase 2 của CitadelBoss.cs, liệt kê các coroutine và thời lượng" |
| Đọc log | Editor.log, output của combat_test.py: "Tổng hợp các lần player bị trúng theo `by=`, đếm theo boss/phase" |
| Xem ảnh chụp | "Mở Assets/Screenshots/x.png: HUD có đè nhau không, chữ có đọc được không, có gì vỡ hình?" |
| Tra web/docs | "Đọc docs Unity về X, trả lời 5 gạch đầu dòng" |
| Soát chữ/prompt | Soát chữ UI, viết nháp prompt cho Grok gen ảnh |
| Bản đồ code | `tools/CODEMAP.md`: Claude đọc file này thay vì quét cả thư mục |

## Việc Claude tự làm
- Sửa code. Trước khi sửa chỉ đọc đúng đoạn cần sửa (Read có offset/limit theo path:line agy đưa).
- Chạy Unity qua MCP, chạy bot test, build.
- Quyết định thiết kế và kiểm chứng cuối cùng.

## Kiểm soát chất lượng
- agy hay đoán sai (lần soát chữ đầu chỉ 3/6 gợi ý đúng; từng nhầm thẻ super là tim). Trước khi sửa code dựa trên nó, Claude luôn đọc lại đúng dòng nó dẫn.
- Hỏi cụ thể, có giới hạn dòng (`-n`), yêu cầu path:line.
- Mỗi lần gọi mất khoảng 20 giây đến 3 phút. Các câu hỏi độc lập chạy nền song song.
- CODEMAP.md cũ thì tạo lại bằng agy.
