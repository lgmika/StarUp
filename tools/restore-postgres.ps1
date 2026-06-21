param(
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [Parameter(Mandatory = $true)][string]$TargetDatabase,
    [string]$ContainerName = "startupconnect-postgres",
    [string]$Username = "startupconnect",
    [switch]$ConfirmDatabaseReplacement
)

$ErrorActionPreference = "Stop"

if ($TargetDatabase -notmatch '^[A-Za-z0-9_]+$') {
    throw "TargetDatabase may contain only letters, numbers, and underscores."
}

if ($Username -notmatch '^[A-Za-z0-9_]+$') {
    throw "Username may contain only letters, numbers, and underscores."
}

if ($TargetDatabase -in @("postgres", "template0", "template1")) {
    throw "Restoring into a PostgreSQL system database is not allowed."
}

$resolvedBackup = (Resolve-Path -LiteralPath $BackupFile).Path
$containerId = docker ps --filter "name=$ContainerName" --format "{{.ID}}" | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($containerId)) {
    throw "PostgreSQL container '$ContainerName' is not running."
}

$databaseExists = docker exec $ContainerName psql -U $Username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$TargetDatabase'"
if ($databaseExists -eq "1" -and -not $ConfirmDatabaseReplacement) {
    throw "Database '$TargetDatabase' already exists. Pass -ConfirmDatabaseReplacement to replace its contents."
}

if ($databaseExists -ne "1") {
    docker exec $ContainerName createdb -U $Username $TargetDatabase
    if ($LASTEXITCODE -ne 0) {
        throw "Could not create database '$TargetDatabase'."
    }
}

$containerBackup = "/tmp/startupconnect_restore_$([Guid]::NewGuid().ToString('N')).dump"
try {
    docker cp $resolvedBackup "${ContainerName}:$containerBackup"
    if ($LASTEXITCODE -ne 0) {
        throw "Could not copy backup into PostgreSQL container."
    }

    $restoreArgs = @("exec", $ContainerName, "pg_restore", "-U", $Username, "-d", $TargetDatabase, "--no-owner", "--no-acl")
    if ($ConfirmDatabaseReplacement) {
        $restoreArgs += @("--clean", "--if-exists")
    }
    $restoreArgs += $containerBackup

    & docker @restoreArgs
    if ($LASTEXITCODE -ne 0) {
        throw "pg_restore failed with exit code $LASTEXITCODE."
    }
}
finally {
    docker exec $ContainerName rm -f $containerBackup | Out-Null
}

Write-Host "Backup restored successfully into database '$TargetDatabase'."
