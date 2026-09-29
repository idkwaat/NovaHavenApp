# Nghiệm thu hardening local — Nova Haven

Ngày: 2026-09-29
Checkout: `delivery/source-install-guide` trong `.incoming-v2/remote-repo` (checkout Git có lịch sử thật).
Phạm vi: API/CMS, website Next.js, Flutter và SQL Server local. Không dùng SQLite; không deploy và không push.

## Kết quả sửa

SQL integration suite ban đầu đạt 50/57. Nhiều mutation qua MVC controller trả HTTP 500 vì API chỉ đăng ký `AddControllers()`, trong khi action hiện có dùng `[ValidateAntiForgeryToken]` và MVC chưa đăng ký filter phụ thuộc ViewFeatures.

Đã thêm regression test gửi mutation thiếu CSRF token. Test tái hiện RED: mong HTTP 400 nhưng nhận HTTP 500. Đổi đăng ký thành `AddControllersWithViews()` để framework đăng ký antiforgery filter chuẩn; không đổi route, OpenAPI contract hoặc database schema. Test missing-CSRF và category/ETag liên quan đạt 2/2; sau đó toàn bộ integration đạt 58/58.

## Kết quả xác minh

| Hạng mục | Kết quả |
| --- | --- |
| Node tests | 154 đạt, 4 skipped, 0 lỗi (158 tổng) |
| .NET API build | Đạt, 0 warning, 0 error |
| .NET Domain tests | 78/78 đạt |
| SQL Server LocalDB integration | 58/58 đạt trên database disposable riêng |
| EF Core model check | Không có thay đổi model chưa được migration |
| Next.js typecheck | Đạt |
| Next.js production build | Đạt; dùng thư mục `.next-acceptance` riêng để không ghi đè metadata được track |
| Web route smoke | 9/9 route trả HTTP 200 trên `127.0.0.1:3010` |
| Flutter tests | 30/30 đạt |
| Flutter analyze | Không có issue |
| Flutter debug APK | Build đạt; có 3 warning Gradle về Java source/target 8 đã lỗi thời |
| SMTP/Web Push thật | Chưa kiểm thử; không có credential/provider ngoài |
| Authenticated browser E2E | Chưa chạy; web route smoke không có API backend đang chạy |
| Android emulator/device install/launch | Chưa kiểm thử trong lượt này |
| Production deploy | Không thực hiện |

Routes được smoke trên production preview Next.js: `/`, `/wiki`, `/admin`, `/account`, `/notifications`, `/catalog`, `/community`, `/map`, `/commerce`. HTTP 200 xác nhận trang được render/serve; không chứng minh dữ liệu API, đăng nhập, CMS mutation hoặc thanh toán hoạt động đầu-cuối.

Preview đang chạy: [http://127.0.0.1:3010](http://127.0.0.1:3010). API chưa được khởi động cùng preview, để tránh kết nối nhầm database có migration cũ.

## SQL Server và dữ liệu local

- Source có 12 migration, mới nhất `20260929074952_AddUserNotifications`.
- Truy vấn SQL chỉ đọc xác nhận `NovaHaven_Local` tồn tại và có 11 migration, mới nhất `20260928164302_AddLocalDemoCommerce`.
- Migration hiện tại được áp dụng khi integration fixture tạo database `NovaHaven_Integration_<GUID>`; sau test không còn fixture database nào.
- `NovaHaven_Local` không bị migrate, seed, xóa hay sửa dữ liệu trong lượt này. Sao lưu và áp dụng migration một cách chủ động trước khi dùng account/notification API trên database đó.
- Không thêm hoặc dùng SQLite.

## Lệnh tái chạy

Chạy từ repo root; cần Node 22+, .NET SDK 10, Flutter 3.44+ và SQL Server LocalDB dưới cùng tài khoản Windows.

```powershell
npm test
dotnet build backend/NovaHaven.Api/NovaHaven.Api.csproj --no-restore --configuration FeatureTests
dotnet test tests/backend/NovaHaven.Domain.Tests/NovaHaven.Domain.Tests.csproj --no-restore --configuration FeatureTests
dotnet test tests/backend/NovaHaven.Integration.Tests/NovaHaven.Integration.Tests.csproj --no-build --no-restore --configuration FeatureTests
```

Web:

```powershell
Set-Location apps/web
npm run typecheck
$env:NOVA_NEXT_DIST_DIR = '.next-acceptance'
npm run build
```

Mobile:

```powershell
Set-Location apps/mobile
flutter pub get --offline
flutter test --no-pub
flutter analyze --no-pub
flutter build apk --debug --no-pub
```

EF model check (khởi tạo model, không kết nối/cập nhật database):

```powershell
$env:NOVA_DB_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=NovaHaven_ModelCheck;Trusted_Connection=True;TrustServerCertificate=True'
dotnet ef migrations has-pending-model-changes --project backend/NovaHaven.Infrastructure/NovaHaven.Infrastructure.csproj --startup-project backend/NovaHaven.Api/NovaHaven.Api.csproj --configuration FeatureTests --no-build
```

## Việc còn lại

1. Sao lưu `NovaHaven_Local`, xem lại migration rồi mới chủ động nâng từ 11 lên 12 migration nếu muốn bật account/notification trên DB này.
2. Chạy API với database local đã cập nhật, sau đó nghiệm thu đăng ký → xác nhận email local → đăng nhập → inbox; kiểm thử SMTP và Web Push thật chỉ khi cấu hình provider/credential.
3. Chạy authenticated browser E2E qua các luồng public/Admin và kiểm tra same-revision giữa web/mobile.
4. Cài/chạy APK trên emulator hoặc thiết bị; đánh giá trải nghiệm thực tế và hoàn thiện kiểm tra accessibility thủ công.
5. Native push cho Flutter, production deployment, backup/restore vận hành và giám sát vẫn là hạng mục riêng, chưa được tuyên bố hoàn tất.
