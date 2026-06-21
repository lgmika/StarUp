param(
    [Parameter(Mandatory = $true)][string]$PreviousApiImage,
    [Parameter(Mandatory = $true)][string]$PreviousFrontendImage,
    [string]$EnvironmentFile = ".env.production",
    [string]$ComposeFile = "docker-compose.production.yml",
    [string]$ApiBaseUrl = "https://api.example.com/api/v1",
    [string]$FrontendUrl = "https://app.example.com"
)

$ErrorActionPreference = "Stop"

if ($PreviousApiImage -notmatch '^[A-Za-z0-9._:/-]+$' -or $PreviousFrontendImage -notmatch '^[A-Za-z0-9._:/-]+$') {
    throw "Image names contain unsupported characters."
}

$resolvedEnvironmentFile = (Resolve-Path -LiteralPath $EnvironmentFile).Path
$resolvedComposeFile = (Resolve-Path -LiteralPath $ComposeFile).Path
$env:API_IMAGE = $PreviousApiImage
$env:FRONTEND_IMAGE = $PreviousFrontendImage

docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile pull api frontend
if ($LASTEXITCODE -ne 0) { throw "Could not pull rollback images." }

# Database migrations remain forward-only; application images must be backward-compatible.
docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile up -d --no-deps api frontend
if ($LASTEXITCODE -ne 0) { throw "Rollback deployment failed." }

& (Join-Path $PSScriptRoot "smoke-test.ps1") -ApiBaseUrl $ApiBaseUrl -FrontendUrl $FrontendUrl
