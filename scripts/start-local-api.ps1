# Script khởi động Backend API Nova Haven cho local & mobile
param(
    [string]$Port = "5080",
    [string]$Database = "NovaHaven_Local"
)

Write-Host "=== Nova Haven Local API Runner ===" -ForegroundColor Cyan

# 1. Đảm bảo LocalDB đang chạy
Write-Host "[1/4] Kiểm tra SQL Server LocalDB..." -ForegroundColor Yellow
$sqlinfo = sqllocaldb info MSSQLLocalDB 2>$null
if ($sqlinfo -notmatch "State:\s+Running") {
    Write-Host "Khởi động MSSQLLocalDB..." -ForegroundColor Yellow
    sqllocaldb start MSSQLLocalDB
}
Write-Host "MSSQLLocalDB đã sẵn sàng." -ForegroundColor Green

# 2. Cấu hình biến môi trường kết nối database & Development
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__NovaDb = "Server=(localdb)\MSSQLLocalDB;Database=$Database;Integrated Security=true"
$env:NOVA_DB_CONNECTION = $env:ConnectionStrings__NovaDb
$env:SeedAdmin__Enabled = "true"
$env:SeedAdmin__Email = "admin@novahaven.vn"
$env:SeedAdmin__Password = "Admin@12345678"

# 3. Lấy IP LAN của máy tính
$lanIp = (Get-NetIPAddress -AddressFamily IPv4 | Where-Object { $_.InterfaceAlias -notlike '*Loopback*' -and $_.IPAddress -notlike '169.254*' } | Select-Object -First 1).IPAddress
Write-Host "[2/4] Thông tin mạng:" -ForegroundColor Yellow
Write-Host "  - Localhost: http://127.0.0.1:$Port" -ForegroundColor Gray
Write-Host "  - LAN IP:    http://${lanIp}:$Port" -ForegroundColor Gray

# 4. Tự động reverse port ADB nếu có thiết bị Android kết nối
$adbPath = "$env:LOCALAPPDATA\Android\Sdk\platform-tools\adb.exe"
if (Test-Path $adbPath) {
    $devices = & $adbPath devices | Where-Object { $_ -match '\tdevice$' }
    if ($devices) {
        Write-Host "[3/4] Tìm thấy thiết bị Android, đang cấu hình adb reverse port $Port..." -ForegroundColor Yellow
        & $adbPath reverse tcp:$Port tcp:$Port
        Write-Host "  -> Thiết bị Android có thể dùng URL: http://localhost:$Port/ hoặc http://127.0.0.1:$Port/" -ForegroundColor Green
    } else {
        Write-Host "[3/4] Không có thiết bị Android nào kết nối qua ADB." -ForegroundColor Gray
    }
} else {
    Write-Host "[3/4] Không tìm thấy ADB." -ForegroundColor Gray
}

Write-Host "[4/4] Khởi động API trên 0.0.0.0:$Port..." -ForegroundColor Cyan
Write-Host ""
Write-Host "CÁC URL CHO MOBILE:" -ForegroundColor Green
Write-Host "  1. Thiết bị Android thật (kết nối ADB): --dart-define=API_BASE_URL=http://localhost:$Port/" -ForegroundColor White
Write-Host "  2. Thiết bị thật (cùng mạng Wi-Fi):     --dart-define=API_BASE_URL=http://${lanIp}:$Port/" -ForegroundColor White
Write-Host "  3. Giả lập Android Emulator:            --dart-define=API_BASE_URL=http://10.0.2.2:$Port/" -ForegroundColor White
Write-Host ""

dotnet run --project backend/NovaHaven.Api --urls "http://0.0.0.0:$Port"
