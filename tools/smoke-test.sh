#!/usr/bin/env bash
set -euo pipefail

api_base_url="${1:-http://localhost:8080/api/v1}"
frontend_url="${2:-http://localhost:3000}"
skip_frontend="${SKIP_FRONTEND:-false}"

if [[ ! "$api_base_url" =~ ^https?://[A-Za-z0-9._:/-]+$ ]]; then
  echo "Invalid API base URL." >&2
  exit 1
fi

if [[ ! "$frontend_url" =~ ^https?://[A-Za-z0-9._:/-]+$ ]]; then
  echo "Invalid frontend URL." >&2
  exit 1
fi

check_url() {
  local name="$1"
  local url="$2"
  local status

  status="$(curl --silent --show-error --location --output /dev/null --write-out '%{http_code}' --max-time 15 "$url")"
  if [[ "$status" != "200" ]]; then
    echo "$name returned HTTP $status: $url" >&2
    return 1
  fi

  echo "$name OK: $url"
}

api_base_url="${api_base_url%/}"
frontend_url="${frontend_url%/}"

check_url "API liveness" "$api_base_url/health/live"
check_url "API readiness" "$api_base_url/health/ready"
check_url "Project full-text search" "$api_base_url/search/projects?keyword=start&page=2147483647&pageSize=100"
check_url "Member full-text search" "$api_base_url/search/members?keyword=founder&page=1&pageSize=20"
check_url "Search suggestions" "$api_base_url/search/suggestions?keyword=start&limit=20"

if [[ "$skip_frontend" != "true" ]]; then
  check_url "Frontend" "$frontend_url"
fi
