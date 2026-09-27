#!/usr/bin/env bash
# Fires two booking requests at the same instant with one idempotency key.
# It supports Step-16 verification without embedding a bearer token or creating persistent evidence files.
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "usage: $0 <bearer-token> [event-id] [idempotency-key]" >&2
  exit 64
fi

token="$1"
event_id="${2:-2}"
request_key="${3:-step16-parallel-$(date +%s)}"
result_dir="$(mktemp -d)"
trap 'rm -rf "$result_dir"' EXIT

request() {
  local output_file="$1"
  curl --silent --show-error \
    --output "$output_file" \
    --write-out '%{http_code}' \
    --request POST 'http://localhost:5100/booking/bookings' \
    --header "Authorization: Bearer $token" \
    --header "Idempotency-Key: $request_key" \
    --header 'Content-Type: application/json' \
    --data "{\"eventId\":$event_id,\"quantity\":1,\"simulatePaymentFailure\":false}"
}

request "$result_dir/first.json" > "$result_dir/first.status" &
first_pid=$!
request "$result_dir/second.json" > "$result_dir/second.status" &
second_pid=$!
wait "$first_pid"
wait "$second_pid"

echo "idempotency-key: $request_key"
echo "first:  HTTP $(<"$result_dir/first.status") $(<"$result_dir/first.json")"
echo "second: HTTP $(<"$result_dir/second.status") $(<"$result_dir/second.json")"
