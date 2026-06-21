#!/usr/bin/env bash
set -euo pipefail

environment_file="${1:-.env.production}"
compose_file="${2:-docker-compose.production.yml}"
api_base_url="${3:-http://localhost:8080/api/v1}"
frontend_url="${4:-http://localhost:3000}"
ready_timeout_seconds="${5:-180}"

if [[ ! "$api_base_url" =~ ^https?://[A-Za-z0-9._:/-]+$ ]]; then
  echo "Invalid API base URL." >&2
  exit 1
fi

if [[ ! "$frontend_url" =~ ^https?://[A-Za-z0-9._:/-]+$ ]]; then
  echo "Invalid frontend URL." >&2
  exit 1
fi

if [[ ! "$ready_timeout_seconds" =~ ^[1-9][0-9]*$ ]]; then
  echo "Ready timeout must be a positive integer." >&2
  exit 1
fi

if [[ ! -f "$environment_file" ]]; then
  echo "Environment file not found: $environment_file" >&2
  exit 1
fi

if [[ ! -f "$compose_file" ]]; then
  echo "Compose file not found: $compose_file" >&2
  exit 1
fi

if grep --extended-regexp --quiet 'CHANGE_ME|ghcr\.io/owner/' -- "$environment_file"; then
  echo "Production environment file still contains placeholder values." >&2
  exit 1
fi

docker compose --env-file "$environment_file" -f "$compose_file" config --quiet
docker compose --env-file "$environment_file" -f "$compose_file" pull
docker compose --env-file "$environment_file" -f "$compose_file" up -d

deadline=$((SECONDS + ready_timeout_seconds))
ready_url="${api_base_url%/}/health/ready"

until curl --silent --show-error --fail --max-time 5 "$ready_url" >/dev/null; do
  if (( SECONDS >= deadline )); then
    docker compose --env-file "$environment_file" -f "$compose_file" logs --tail 100 api
    echo "API did not become ready before timeout." >&2
    exit 1
  fi

  sleep 3
done

bash "$(dirname "$0")/smoke-test.sh" "$api_base_url" "$frontend_url"
