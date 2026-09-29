$ErrorActionPreference = 'Stop'

$previewApi = 'http://127.0.0.1:5081'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$health = Invoke-RestMethod -Uri "$previewApi/health" -TimeoutSec 5
if ($health.status -ne 'ok') {
  throw "Preview API is not healthy at $previewApi. No content was seeded."
}

$adminEmail = Read-Host 'Local preview Admin email'
if ([string]::IsNullOrWhiteSpace($adminEmail)) {
  throw 'Admin email is required. No content was seeded.'
}

$securePassword = Read-Host 'Local preview Admin password' -AsSecureString
if ($securePassword.Length -lt 12) {
  $securePassword.Dispose()
  throw 'The password did not meet the minimum length. No content was seeded.'
}

$bstr = [IntPtr]::Zero
$plainPassword = $null
$previousApiUrl = $env:NOVA_API_URL
$previousAdminEmail = $env:NOVA_ADMIN_EMAIL
$previousAdminPassword = $env:NOVA_ADMIN_PASSWORD
$pushedLocation = $false

try {
  $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePassword)
  $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
  $env:NOVA_API_URL = $previewApi
  $env:NOVA_ADMIN_EMAIL = $adminEmail.Trim()
  $env:NOVA_ADMIN_PASSWORD = $plainPassword

  Push-Location $repositoryRoot
  $pushedLocation = $true
  npm run seed:demo
  if ($LASTEXITCODE -ne 0) {
    throw "Demo seed failed with exit code $LASTEXITCODE. No database rows were removed."
  }
}
finally {
  if ($pushedLocation) { Pop-Location }
  if ($null -eq $previousApiUrl) { Remove-Item Env:NOVA_API_URL -ErrorAction SilentlyContinue } else { $env:NOVA_API_URL = $previousApiUrl }
  if ($null -eq $previousAdminEmail) { Remove-Item Env:NOVA_ADMIN_EMAIL -ErrorAction SilentlyContinue } else { $env:NOVA_ADMIN_EMAIL = $previousAdminEmail }
  if ($null -eq $previousAdminPassword) { Remove-Item Env:NOVA_ADMIN_PASSWORD -ErrorAction SilentlyContinue } else { $env:NOVA_ADMIN_PASSWORD = $previousAdminPassword }
  if ($bstr -ne [IntPtr]::Zero) { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) }
  if ($securePassword) { $securePassword.Dispose() }
  $plainPassword = $null
  $adminEmail = $null
}
