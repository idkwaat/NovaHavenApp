# Nova Haven Companion (Flutter)

Ứng dụng đọc nội dung đã xuất bản của Nova Haven và truy cập tài khoản người chơi. Wiki, Tin tức, Vật phẩm và Khám phá dùng cùng ASP.NET Core API với website; tài khoản và hộp thư thông báo dùng Identity cookie + CSRF, lưu cookie trong `flutter_secure_storage`.

## Chạy local

1. Khởi động Backend ASP.NET Core kết nối tới SQL Server local theo hướng dẫn trong [README gốc](../../README.md).
2. Cài Flutter SDK rồi chạy:

```powershell
flutter pub get
flutter test
flutter analyze
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/
```

`10.0.2.2` là địa chỉ từ Android Emulator tới máy host. Nếu dùng thiết bị thật, thay bằng địa chỉ HTTPS hoặc IP LAN có thể truy cập được của API. Không đưa SQL connection string hay bí mật email vào ứng dụng.

## Tài khoản và thông báo

- Mở **Khám phá → Tài khoản người chơi** để đăng ký, đăng nhập hoặc đăng xuất. Tài khoản dùng được ngay sau khi tạo; ứng dụng không gửi email xác nhận.
- Mở **Khám phá → Thông báo** hoặc **Tài khoản → Mở thông báo** để xem inbox, đánh dấu một tin hoặc toàn bộ là đã đọc. Inbox lấy từ SQL Server API, không phải dữ liệu giả trên thiết bị.
- Web Push chỉ dành cho trình duyệt. Ứng dụng Flutter chưa có native push/FCM; muốn thêm cần cấu hình provider và thông tin nền tảng riêng.

Tài khoản và thông báo chỉ dùng được khi backend SQL Server đã áp dụng migration `AddUserNotifications`. Migration này là phần việc local; xem mục tài khoản/thông báo trong README gốc và không chạy migration trên DB có dữ liệu nếu chưa kiểm tra đúng database đích.
