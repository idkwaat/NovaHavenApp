[CmdletBinding()]
param(
    [string]$ServerInstance = '(localdb)\MSSQLLocalDB',
    [string]$SourceDatabase = 'NovaHaven_Local',
    [string]$RestoreDatabase,
    [string]$BackupDirectory
)

$ErrorActionPreference = 'Stop'
$sqlcmd = (Get-Command sqlcmd -ErrorAction Stop).Source
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $BackupDirectory) { $BackupDirectory = Join-Path $repoRoot 'artifacts\localdb-backup' }
New-Item -ItemType Directory -Force -Path $BackupDirectory | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
if (-not $RestoreDatabase) { $RestoreDatabase = "${SourceDatabase}_RestoreCheck_${stamp}" }
$backupPath = Join-Path $BackupDirectory "${SourceDatabase}_${stamp}.bak"

function Quote-SqlIdentifier([string]$value) {
    return '[' + $value.Replace(']', ']]') + ']'
}

function Quote-SqlString([string]$value) {
    return "N'" + $value.Replace("'", "''") + "'"
}

function Invoke-Sql([string]$query) {
    & $sqlcmd -S $ServerInstance -E -b -Q $query
    if ($LASTEXITCODE -ne 0) { throw "sqlcmd failed with exit code $LASTEXITCODE." }
}

$sourceIdentifier = Quote-SqlIdentifier $SourceDatabase
$restoreIdentifier = Quote-SqlIdentifier $RestoreDatabase
$backupLiteral = Quote-SqlString $backupPath

$sourceExists = & $sqlcmd -S $ServerInstance -E -b -h -1 -W -Q "SELECT IIF(DB_ID($(Quote-SqlString $SourceDatabase)) IS NULL, 0, 1);"
if ($LASTEXITCODE -ne 0 -or (($sourceExists | Select-Object -First 1).ToString().Trim() -ne '1')) {
    throw "Source database '$SourceDatabase' was not found on '$ServerInstance'."
}

$restoreExists = & $sqlcmd -S $ServerInstance -E -b -h -1 -W -Q "SELECT IIF(DB_ID($(Quote-SqlString $RestoreDatabase)) IS NULL, 0, 1);"
if ($LASTEXITCODE -ne 0) { throw 'Could not check the restore database name.' }
if (($restoreExists | Select-Object -First 1).ToString().Trim() -eq '1') {
    throw "Restore database '$RestoreDatabase' already exists. Choose a new -RestoreDatabase name."
}

$fileLines = @(& $sqlcmd -S $ServerInstance -E -b -h -1 -W -s '|' -Q "SELECT name, type_desc FROM ${sourceIdentifier}.sys.database_files ORDER BY file_id;")
if ($LASTEXITCODE -ne 0) { throw 'Could not read source database file metadata.' }
$sourceFiles = @(
    foreach ($line in $fileLines) {
        $parts = $line -split '\|'
        if ($parts.Count -eq 2 -and $parts[0].Trim() -and @('ROWS', 'LOG') -contains $parts[1].Trim()) {
            [pscustomobject]@{ LogicalName = $parts[0].Trim(); Type = $parts[1].Trim() }
        }
    }
)
if ($sourceFiles.Count -lt 2) { throw 'Source database file metadata was incomplete; backup/restore was not attempted.' }

Write-Host "Backing up $SourceDatabase to $backupPath"
Invoke-Sql "BACKUP DATABASE $sourceIdentifier TO DISK = $backupLiteral WITH INIT, CHECKSUM, STATS = 5;"
Invoke-Sql "RESTORE VERIFYONLY FROM DISK = $backupLiteral WITH CHECKSUM;"

$moveClauses = @()
$dataIndex = 0
$logIndex = 0
foreach ($file in $sourceFiles) {
    if ($file.Type -eq 'LOG') {
        $targetPath = Join-Path $BackupDirectory "${RestoreDatabase}_log$logIndex.ldf"
        $logIndex++
    } else {
        $targetPath = Join-Path $BackupDirectory "${RestoreDatabase}_data$dataIndex.mdf"
        $dataIndex++
    }
    $moveClauses += "MOVE $(Quote-SqlString $file.LogicalName) TO $(Quote-SqlString $targetPath)"
}

Write-Host "Restoring into new database $RestoreDatabase"
$restoreQuery = "RESTORE DATABASE $restoreIdentifier FROM DISK = $backupLiteral WITH $($moveClauses -join ', '), CHECKSUM, RECOVERY, STATS = 5;"
Invoke-Sql $restoreQuery
Invoke-Sql "DBCC CHECKDB ($(Quote-SqlString $RestoreDatabase)) WITH NO_INFOMSGS, ALL_ERRORMSGS;"
Invoke-Sql "USE $restoreIdentifier; SELECT DB_NAME() AS ConnectedDatabase, COUNT(*) AS TableCount FROM sys.tables; SELECT COUNT(*) AS MigrationCount FROM dbo.__EFMigrationsHistory;"

Write-Host "BACKUP/RESTORE PASS: backup=$backupPath; restoredDatabase=$RestoreDatabase"
Write-Host 'The restored database and backup artifact are retained for inspection; the source database was not changed.'
