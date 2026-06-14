param(
    [string]$ContainerName = "startupconnect-postgres",
    [string]$Database = $env:POSTGRES_DB,
    [string]$Username = $env:POSTGRES_USER,
    [string]$BackupDirectory = $env:BACKUP_DIRECTORY,
    [int]$KeepLatest = 14
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($Database)) {
    $Database = "startupconnect"
}

if ([string]::IsNullOrWhiteSpace($Username)) {
    $Username = "startupconnect"
}

if ([string]::IsNullOrWhiteSpace($BackupDirectory)) {
    $BackupDirectory = Join-Path (Get-Location) "backups"
}

$resolvedBackupDirectory = [System.IO.Path]::GetFullPath($BackupDirectory)
New-Item -ItemType Directory -Force -Path $resolvedBackupDirectory | Out-Null

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupFile = Join-Path $resolvedBackupDirectory "$Database`_$timestamp.dump"

$containerId = docker ps --filter "name=$ContainerName" --format "{{.ID}}" | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($containerId)) {
    throw "PostgreSQL container '$ContainerName' is not running."
}

Write-Host "Creating backup for database '$Database' from container '$ContainerName'..."
$process = Start-Process -FilePath "docker" `
    -ArgumentList @("exec", $ContainerName, "pg_dump", "-U", $Username, "-d", $Database, "-F", "c", "--no-owner", "--no-acl") `
    -RedirectStandardOutput $backupFile `
    -NoNewWindow `
    -Wait `
    -PassThru

if ($process.ExitCode -ne 0) {
    if (Test-Path -LiteralPath $backupFile) {
        Remove-Item -LiteralPath $backupFile -Force
    }

    throw "pg_dump failed with exit code $($process.ExitCode)."
}

if ((Get-Item $backupFile).Length -le 0) {
    Remove-Item -LiteralPath $backupFile -Force
    throw "Backup file was empty."
}

Write-Host "Backup created: $backupFile"

if ($KeepLatest -gt 0) {
    Get-ChildItem -Path $resolvedBackupDirectory -Filter "$Database`_*.dump" |
        Sort-Object LastWriteTime -Descending |
        Select-Object -Skip $KeepLatest |
        Remove-Item -Force
}
