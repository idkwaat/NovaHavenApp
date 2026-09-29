# Hướng dẫn cài đặt Nova Haven (local development)

Tài liệu này hướng dẫn chạy API .NET, website Next.js, ứng dụng Flutter và SQL Server trên Windows để phát triển/demo. Đây chưa phải quy trình triển khai production.

## 1. Source, database và dữ liệu

| Thành phần | Đường dẫn | Nội dung |
| --- | --- | --- |
| Backend | `backend/` | ASP.NET Core 10 REST API |
| Database | `backend/NovaHaven.Infrastructure/Data/Migrations/` | 12 EF Core migrations cho SQL Server |
| Website/Admin | `apps/web/` | Next.js |
| Mobile | `apps/mobile/` | Flutter reader, tài khoản và hộp thư thông báo |
| Hợp đồng API | `contracts/openapi/wiki-v1.json` | OpenAPI dùng chung cho clients |
| Dữ liệu demo | `scripts/demo-*-content.json`, `scripts/demo-seed.mjs` | Seed nội dung tiếng Việt qua API local |
| ERD | `docs/defense/ERD.md` | Quan hệ bảng |

Repo chứa schema dưới dạng migrations, không chứa file database đã nạp dữ liệu hoặc bản sao lưu `.bak`. Mỗi máy tạo SQL Server database local riêng rồi áp dụng migrations. Minecraft plugins vẫn giữ quyền xử lý gameplay; source này không ghi vào database Minecraft.

## 2. Yêu cầu

- Windows 10/11, PowerShell.
- .NET SDK 10, EF CLI `dotnet-ef` 10.0.12.
- SQL Server 2022 LocalDB (`MSSQLLocalDB`) hoặc Docker Desktop + Docker Compose.
- Node.js 22+ và npm.
- Flutter SDK 3.44+ và Android Studio/emulator nếu chạy ứng dụng Android.

Phiên bản đã xác minh tại checkout ngày 2026-09-29: .NET SDK 10.0.302, Node 24.16.0/npm 11.13.0, Flutter 3.44.1/Dart 3.12.1 và SQL Server LocalDB 17.0.4025.3. Docker không có trong môi trường này. Kết quả test mới nhất được ghi ở [báo cáo hardening local](verification/2026-09-29-local-platform-hardening.md); thiết bị Android thật/emulator chưa được nghiệm thu trong lượt đó.

## 3. Tạo database local mới

Ví dụ dùng database riêng `NovaHaven_InstallDemo`. Đổi tên nếu database này đã tồn tại. Chỉ áp dụng migrations lên DB local mới hoặc DB đã sao lưu/kiểm tra.

### Cách A — SQL Server LocalDB

Mở PowerShell ở thư mục gốc repo:

```powershell
$dbConnection = 'Server=(localdb)\MSSQLLocalDB;Database=NovaHaven_InstallDemo;Integrated Security=true;TrustServerCertificate=True'
$env:NOVA_DB_CONNECTION = $dbConnection
$env:ConnectionStrings__NovaDb = $dbConnection
```

LocalDB thuộc tài khoản Windows đã tạo instance. Chạy EF và API dưới cùng tài khoản đó.

### Cách B — SQL Server qua Docker

Tạo file `.env` local từ mẫu, sửa `MSSQL_SA_PASSWORD` thành mật khẩu mạnh chỉ dùng trên máy này rồi chạy SQL Server. Không commit file `.env`.

```powershell
Copy-Item .env.example .env
notepad .env
docker compose up -d sqlserver
```

Khi SQL Server sẵn sàng, nhập lại cùng mật khẩu trong connection string. Tránh dùng dấu chấm phẩy trong mật khẩu.

```powershell
$saPassword = '<mat-khau-local-da-dat-trong-file-env>'
$dbConnection = "Server=localhost,14333;Database=NovaHaven_InstallDemo;User Id=sa;Password=$saPassword;TrustServerCertificate=True"
$env:NOVA_DB_CONNECTION = $dbConnection
$env:ConnectionStrings__NovaDb = $dbConnection
```

EF đọc `NOVA_DB_CONNECTION`; API đọc `ConnectionStrings__NovaDb`. Hai biến phải trỏ tới cùng database. Docker Compose chỉ chạy SQL Server; API và web chạy bằng SDK tương ứng.

## 4. Tạo schema bằng migrations

Chạy từ thư mục gốc:

```powershell
dotnet --version
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext
dotnet ef migrations list --project backend/NovaHaven.Infrastructure --startup-project backend/NovaHaven.Api --context NovaDbContext
```

Nếu đã có `dotnet-ef` 10.0.12, bỏ qua lệnh cài tool. Hiện có 12 migrations, từ `InitialWiki` đến `AddUserNotifications`. App không tự tạo bảng hoặc tự migrate khi khởi động. Không dùng `EnsureCreated` và không sửa schema thủ công.

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

Kiểm tra `http://127.0.0.1:5080/health`. Trong Development không cấu hình SMTP, email xác nhận người chơi được ghi vào `.local/mail-outbox`; không gửi email ra ngoài.

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

Integration tests cần SQL Server local và tự dùng database `NovaHaven_Integration_<GUID>` riêng. Lần chạy ngày 2026-09-29 đạt 58/58 và không để lại database test. `NovaHaven_Local` hiện có 11 migrations trong khi source có 12; migration `AddUserNotifications` đã được kiểm thử trên fixture disposable nhưng **chưa áp dụng** lên DB này. Nếu muốn dùng account/notification trên DB có sẵn, hãy sao lưu và đọc kỹ migration trước khi chủ động chạy `dotnet ef database update`; không dùng `EnsureCreated`.

Web typecheck/build chạy trong `apps/web`; Flutter test/analyze chạy trong `apps/mobile`. Xem kết quả và giới hạn của từng gate tại `docs/verification/` và `docs/roadmap/NOVA-HAVEN-ROADMAP.md`.

Đây là local MVP; chưa tuyên bố production deployment, đồng bộ dữ liệu Minecraft, thanh toán thật, cấp vật phẩm, native push cho Flutter hay nghiệm thu đầy đủ browser/mobile E2E. Xem [hướng dẫn demo](defense/DEMO-GUIDE.md), [ERD](defense/ERD.md) và [readiness](defense/READINESS-CHECKLIST.md).
