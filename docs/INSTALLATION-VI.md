# Hướng dẫn cài đặt Nova Haven (local development)

Tài liệu này hướng dẫn chạy API .NET, website Next.js, ứng dụng Flutter và PostgreSQL local trên Windows để phát triển/demo. Đây chưa phải quy trình triển khai production. Database SQL Server cũ được giữ nguyên, không tự chuyển đổi dữ liệu.

## 1. Source, database và dữ liệu

| Thành phần | Đường dẫn | Nội dung |
| --- | --- | --- |
| Backend | `backend/` | ASP.NET Core 10 REST API |
| Database | `backend/NovaHaven.Infrastructure/Data/PostgreSqlMigrations/` | Migration baseline cho PostgreSQL (sinh từ model hiện tại) |
| Website/Admin | `apps/web/` | Next.js |
| Mobile | `apps/mobile/` | Flutter reader, tài khoản và hộp thư thông báo |
| Hợp đồng API | `contracts/openapi/wiki-v1.json` | OpenAPI dùng chung cho clients |
| Dữ liệu demo | `scripts/demo-*-content.json`, `scripts/demo-seed.mjs` | Seed nội dung tiếng Việt qua API local |
| ERD | `docs/defense/ERD.md` | Quan hệ bảng |

Repo chứa schema dưới dạng migrations, không chứa file database đã nạp dữ liệu hoặc secret. Mỗi máy tạo PostgreSQL database local riêng rồi áp dụng PostgreSQL migrations. Các file SQL Server migration cũ chỉ được lưu làm lịch sử, không biên dịch/chạy cùng provider hiện tại. Minecraft plugins vẫn giữ quyền xử lý gameplay; source này không ghi vào database Minecraft.

## 2. Yêu cầu

- Windows 10/11, PowerShell.
- .NET SDK 10, EF CLI `dotnet-ef` 10.0.12.
- PostgreSQL local hoặc Docker Desktop + Docker Compose (Compose dùng PostgreSQL 17, cổng loopback `15432`).
- Node.js 22+ và npm.
- Flutter SDK 3.44+ và Android Studio/emulator nếu chạy ứng dụng Android.

Các báo cáo SQL Server/LocalDB ngày 2026-09-29 là lịch sử của provider cũ. Trên checkout này, solution build và toàn bộ PostgreSQL integration đã được chạy trên một PostgreSQL 18.4 cluster tạm chỉ bind loopback; fixture tự tạo/xóa database test ngẫu nhiên. Không có migration, seed hoặc thao tác ghi nào nhắm vào database nội dung local của bạn. Khi tự chạy ứng dụng, cần PostgreSQL local hoặc Docker Compose; kiểm thử trên emulator/thiết bị thật vẫn là bước nghiệm thu riêng.

> **Android Studio không đồng nghĩa với Flutter SDK.** Android Studio cung cấp IDE, Android SDK và emulator; Flutter SDK là bộ công cụ riêng. Trên máy này plugin Flutter/Dart của Android Studio đã có, nhưng Flutter SDK trước đó chưa được cài. Mình đã cài Flutter stable 3.47.3 vào thư mục bị Git ignore `D:\nova-haven-handoff\.local-tools\flutter`. Flutter CLI hoạt động bằng đường dẫn đầy đủ nhưng chưa được thêm vào PATH toàn hệ thống; đặt Flutter SDK path trong Android Studio về thư mục trên nếu IDE chưa tự nhận. Một checkout khác hoặc máy khác cần cài Flutter SDK riêng.

## 3. Tạo database local mới

Tạo database local riêng tên `NovaHaven_InstallDemo`. Không trỏ lệnh migration vào database SQL Server cũ hoặc database PostgreSQL đang chứa dữ liệu cần giữ.

### Cách A — PostgreSQL qua Docker Compose

Tạo `.env` local từ mẫu, đổi `POSTGRES_PASSWORD` thành mật khẩu riêng trên máy này rồi chạy PostgreSQL. Không commit file `.env`.

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d postgres
```

Compose chỉ bind vào loopback cổng `15432`, tránh xung đột với service PostgreSQL đang dùng `5432`. Sau khi container sẵn sàng, nhập mật khẩu local vào biến trong PowerShell (không ghi vào source):

```powershell
$pgPassword = '<mat-khau-local-da-dat-trong-file-env>'
$dbConnection = "Host=127.0.0.1;Port=15432;Database=NovaHaven_InstallDemo;Username=novahaven;Password=$pgPassword"
$env:NOVA_DB_CONNECTION = $dbConnection
$env:ConnectionStrings__NovaDb = $dbConnection
```

### Cách B — PostgreSQL đã cài trên máy

Dùng role local có quyền tạo database và điền thông tin xác thực của chính máy đó. Không cần Docker:

```powershell
$dbConnection = 'Host=127.0.0.1;Port=5432;Database=NovaHaven_InstallDemo;Username=<local-role>;Password=<local-password>'
$env:NOVA_DB_CONNECTION = $dbConnection
$env:ConnectionStrings__NovaDb = $dbConnection
```

Với integration tests, đặt thêm `NOVA_HAVEN_TEST_ADMIN_CONNECTION` trỏ đến database `postgres` bằng role local có quyền `CREATEDB`. Mỗi fixture chỉ tạo/xóa database có tên ngẫu nhiên `NovaHaven_Integration_<32 hex>`; không trỏ biến này vào database đang chứa nội dung.

EF tooling đọc `NOVA_DB_CONNECTION`; API đọc `ConnectionStrings__NovaDb`. Hai biến phải trỏ tới cùng PostgreSQL local. Có thể lưu connection string API trong User Secrets, không đưa mật khẩu vào `launchSettings.json` hoặc Git.

## 4. Tạo schema bằng migrations

Chạy từ thư mục gốc sau khi baseline migration PostgreSQL đã được tạo và review:

```powershell
dotnet --version
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext
dotnet ef migrations list --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext
```

Nếu đã có `dotnet-ef` 10.0.12, bỏ qua lệnh cài tool. EF chỉ nhìn thấy migration PostgreSQL trong `Data/PostgreSqlMigrations`; migrations SQL Server cũ không nằm trong chuỗi chạy hiện hành. App không tự tạo bảng hoặc tự migrate khi khởi động. Không dùng `EnsureCreated` và không sửa schema thủ công.

## 5. Tạo Admin local và chạy API

Admin bootstrap chỉ chạy khi môi trường là `Development` và được bật tường minh. Tự chọn email/mật khẩu riêng; không ghi thông tin đăng nhập vào source.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:SeedAdmin__Enabled = 'true'
$env:SeedAdmin__Email = 'admin@local.test'
$env:SeedAdmin__Password = '<mat-khau-local-rieng-cua-ban>'
dotnet run --project backend/NovaHaven.Api --urls http://127.0.0.1:5080
```

Sau khi tài khoản được tạo, nhấn Ctrl+C, tắt các biến bootstrap rồi khởi động API lại. Giữ hai biến database của bước trước trong cửa sổ PowerShell.

```powershell
Remove-Item Env:SeedAdmin__Enabled, Env:SeedAdmin__Email, Env:SeedAdmin__Password -ErrorAction SilentlyContinue
dotnet run --project backend/NovaHaven.Api --urls http://127.0.0.1:5080
```

Kiểm tra `http://127.0.0.1:5080/health`. Đăng ký người chơi dùng được ngay và không gửi email. API và dữ liệu chạy trên PostgreSQL local.

## 6. Nạp dữ liệu demo

Mở PowerShell khác tại thư mục gốc. Seeder chỉ chấp nhận API loopback, cần đăng nhập Admin, có tính lặp an toàn theo slug và không xóa hàng loạt dữ liệu cũ. Nên chạy trên database demo mới để có bộ dữ liệu nhất quán.

```powershell
$env:NOVA_API_URL = 'http://127.0.0.1:5080'
$env:NOVA_ADMIN_EMAIL = 'admin@local.test'
$env:NOVA_ADMIN_PASSWORD = '<mat-khau-admin-local>'
npm run seed:demo
```

Seed gồm:

- 15 bài Wiki (3 bài nền và 12 bài tiếng Việt), cùng tag phân loại.
- 4 bài Tin tức.
- 5 địa điểm và 4 NPC thuộc lore hư cấu của Nova Haven; chưa xác minh trong Minecraft.
- 12 mục Catalog tham khảo vanilla có nguồn.
- 10 ví dụ Community được đánh dấu là nội dung mẫu.

Seed không tạo Reward, Commerce offer hoặc đơn hàng. Checkout là mô phỏng local: không thu tiền và không cấp vật phẩm game. Muốn thử storefront, Admin phải tự tạo và xuất bản offer, bật mua demo local và đặt giá VND trên database demo.

## 7. Chạy website

Mở PowerShell mới:

```powershell
Set-Location apps/web
Copy-Item .env.example .env.local
```

Trong `.env.local`, giữ `NOVA_API_ORIGIN=http://127.0.0.1:5080`. Có thể thay Discord invite và địa chỉ Minecraft server nếu muốn hiển thị trên giao diện. Không ghi mật khẩu Admin hoặc connection string SQL vào file này.

```powershell
npm install
npm run typecheck
npm run dev -- --hostname 127.0.0.1 --port 3000
```

Mở `http://127.0.0.1:3000/`; Wiki ở `/wiki`, Admin ở `/admin`. Đăng nhập bằng tài khoản ở bước 5.

## 8. Chạy Flutter trên Android

Giữ API chạy rồi mở PowerShell khác:

```powershell
Set-Location apps/mobile
flutter pub get
flutter test
flutter analyze
flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5080/
```

`10.0.2.2` là địa chỉ Android Emulator dùng để truy cập máy host. Điện thoại thật cần API bind vào IP LAN có thể truy cập được, ví dụ dùng `dotnet run --project backend/NovaHaven.Api --urls http://0.0.0.0:5080`; thay URL bằng IP máy phát triển và cho phép kết nối qua firewall của mạng local. Không dùng HTTP này trên Internet.

## 9. Kiểm tra và giới hạn

Từ thư mục gốc:

```powershell
npm test
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj
dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj
```

Integration tests cần một PostgreSQL local role có quyền `CREATEDB` và kết nối qua `NOVA_HAVEN_TEST_ADMIN_CONNECTION`. Fixture chỉ tạo/xóa database tên chính xác `NovaHaven_Integration_<32 hex>`; lần nghiệm thu checkout này đạt 78/78 trên PostgreSQL 18.4 cô lập. EF Core xác nhận không có thay đổi model chưa được migration bao phủ. PostgreSQL database chứa nội dung của bạn không bị áp dụng migration hoặc seed trong lượt kiểm tra. Không dùng `EnsureCreated`; chỉ tự áp migration lên database local sau khi chọn đúng connection string và sao lưu dữ liệu cần giữ.

Trên checkout/máy này, chạy Flutter CLI bằng đường dẫn đầy đủ nếu chưa thêm SDK vào PATH:

```powershell
& 'D:\nova-haven-handoff\.local-tools\flutter\bin\flutter.bat' test --no-pub
& 'D:\nova-haven-handoff\.local-tools\flutter\bin\flutter.bat' analyze --no-pub
```

Kết quả lượt kiểm tra hiện tại: Flutter test 26/26, analyze không có issue. `flutter doctor` xác nhận Android SDK 36.1, Java 21 và licenses Android đã sẵn sàng. Network-check tới pub.dev, Google Maven, GitHub bị sandbox chặn; các lệnh test/analyze không cần tải thêm package do dependency đã cache.

Web typecheck/build chạy trong `apps/web`; Flutter test/analyze chạy trong `apps/mobile`. Xem kết quả và giới hạn của từng gate tại `docs/verification/` và `docs/roadmap/NOVA-HAVEN-ROADMAP.md`.

Đây là local MVP; chưa tuyên bố production deployment, đồng bộ dữ liệu Minecraft, thanh toán thật, cấp vật phẩm, native push cho Flutter hay nghiệm thu đầy đủ browser/mobile E2E. Xem [hướng dẫn demo](defense/DEMO-GUIDE.md), [ERD](defense/ERD.md) và [readiness](defense/READINESS-CHECKLIST.md).
