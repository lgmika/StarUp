param(
    [string]$ApiBaseUrl = "http://localhost:8080/api/v1",
    [string]$FrontendUrl = "http://localhost:3000",
    [int]$TimeoutSeconds = 15,
    [switch]$SkipFrontend
)

$ErrorActionPreference = "Stop"

function Assert-HttpEndpoint {
    param(
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Name
    )

    $response = Invoke-WebRequest -UseBasicParsing -Uri $Url -TimeoutSec $TimeoutSeconds
    if ($response.StatusCode -lt 200 -or $response.StatusCode -ge 300) {
        throw "$Name returned HTTP $($response.StatusCode)."
    }

    Write-Host "[OK] $Name ($($response.StatusCode))"
}

$api = $ApiBaseUrl.TrimEnd("/")
Assert-HttpEndpoint -Url "$api/health/live" -Name "API liveness"
Assert-HttpEndpoint -Url "$api/health/ready" -Name "API readiness"
Assert-HttpEndpoint -Url "$api/search/projects?keyword=start&page=2147483647&pageSize=100" -Name "Project full-text search"
Assert-HttpEndpoint -Url "$api/search/members?keyword=founder&page=1&pageSize=20" -Name "Member full-text search"
Assert-HttpEndpoint -Url "$api/search/suggestions?keyword=start&limit=20" -Name "Search suggestions"

if (-not $SkipFrontend) {
    Assert-HttpEndpoint -Url $FrontendUrl -Name "Frontend"
}

Write-Host "Smoke test completed successfully."
