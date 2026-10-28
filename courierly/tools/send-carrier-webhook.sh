#!/usr/bin/env bash
# Signs a carrier webhook body and posts it.
# Usage: tools/send-carrier-webhook.sh -s <secret> -f <body.json> [-u <url>] [-c <carrier>] [-t <unix-ts>] [--tamper]
#   -t      sign with a specific timestamp (use an old value to test the 300 s window)
#   --tamper  sign the original body, then send a modified body
set -euo pipefail

URL="http://localhost:5080/api/webhooks/carrier"
CARRIER="lbc-express"
SECRET=""
FILE=""
TS="$(date +%s)"
TAMPER=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    -s) SECRET="$2"; shift 2 ;;
    -f) FILE="$2"; shift 2 ;;
    -u) URL="$2"; shift 2 ;;
    -c) CARRIER="$2"; shift 2 ;;
    -t) TS="$2"; shift 2 ;;
    --tamper) TAMPER=1; shift ;;
    *) echo "Unknown option $1" >&2; exit 2 ;;
  esac
done

if [[ -z "$SECRET" || -z "$FILE" ]]; then
  echo "Usage: $0 -s <secret> -f <body.json> [-u <url>] [-c <carrier>] [-t <unix-ts>] [--tamper]" >&2
  exit 2
fi

BODY="$(cat "$FILE")"
SIG="$(printf '%s' "${TS}.${BODY}" | openssl dgst -sha256 -hmac "$SECRET" -hex | sed 's/^.*= //')"

if [[ "$TAMPER" -eq 1 ]]; then
  BODY="${BODY/Delivered/Cancelled}"
  BODY="${BODY/InTransit/Delivered}"
fi

echo "POST $URL  t=$TS"
printf '%s' "$BODY" | curl -sS -i -X POST "$URL" \
  -H "Content-Type: application/json" \
  -H "X-Courierly-Carrier: ${CARRIER}" \
  -H "X-Courierly-Signature: t=${TS},v1=${SIG}" \
  --data-binary @-
echo
