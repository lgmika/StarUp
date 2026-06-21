param(
    [string]$EnvironmentFile = ".env.production",
    [string]$ComposeFile = "docker-compose.production.yml",
    [string]$ApiBaseUrl = "http://localhost:8080/api/v1",
    [string]$FrontendUrl = "http://localhost:3000",
    [int]$ReadyTimeoutSeconds = 180
)

$ErrorActionPreference = "Stop"

$resolvedEnvironmentFile = (Resolve-Path -LiteralPath $EnvironmentFile).Path
$resolvedComposeFile = (Resolve-Path -LiteralPath $ComposeFile).Path
$environmentText = Get-Content -LiteralPath $resolvedEnvironmentFile -Raw
if ($environmentText -match 'CHANGE_ME|ghcr\.io/owner/') {
    throw "Production environment file still contains placeholder values."
}

docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile config --quiet
if ($LASTEXITCODE -ne 0) { throw "Production compose validation failed." }

docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile pull
if ($LASTEXITCODE -ne 0) { throw "Could not pull production images." }

docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile up -d
if ($LASTEXITCODE -ne 0) { throw "Production deployment failed." }

$deadline = (Get-Date).AddSeconds($ReadyTimeoutSeconds)
do {
    try {
        $response = Invoke-WebRequest -UseBasicParsing -Uri "$($ApiBaseUrl.TrimEnd('/'))/health/ready" -TimeoutSec 5
        if ($response.StatusCode -eq 200) { break }
    }
    catch {
        Start-Sleep -Seconds 3
    }
} while ((Get-Date) -lt $deadline)

if ($response.StatusCode -ne 200) {
    docker compose --env-file $resolvedEnvironmentFile -f $resolvedComposeFile logs --tail 100 api
    throw "API did not become ready before timeout."
}

& (Join-Path $PSScriptRoot "smoke-test.ps1") -ApiBaseUrl $ApiBaseUrl -FrontendUrl $FrontendUrl
