# Carrier webhook contract

Carriers push shipment status changes to `POST /api/webhooks/carrier`.

## Request

`Content-Type: application/json`

```json
{
  "eventId": "evt_01JC8Q0W5Z3T",
  "shipmentReference": "SWC-1003",
  "status": "Delivered",
  "occurredAt": "2026-11-12T09:41:07Z"
}
```

- `eventId` is unique per event. Carriers retry the same event (same `eventId`) on any non-2xx response or timeout, for up to 24 hours.
- `status` is one of `PickedUp`, `InTransit`, `Delivered`.

## Signature

Each carrier has its own shared secret, agreed out of band. Every request carries:

```
X-Courierly-Signature: t=1762940467,v1=5f2b9c...e1
```

- `t` is the Unix time (seconds) when the carrier signed the request.
- `v1` is the lowercase hex HMAC-SHA256 of the string `"{t}.{rawBody}"` using the carrier's secret, where `rawBody` is the exact bytes of the request body as sent.
- Courierly must reject a request whose signature does not match, or whose `t` is more than 300 seconds away from Courierly's clock.
- Carriers identify themselves with the `X-Courierly-Carrier` header (for example `lbc-express`), which selects the secret. Each carrier account is contracted by exactly one tenant, so the carrier also determines the tenant.

## Response

Respond `2xx` quickly (within 5 seconds). Respond `401` for a missing or invalid signature. A `2xx` for an `eventId` already processed is correct: the carrier is just retrying.

## Current state

The endpoint exists but does not implement the signature check, the carrier header or the `eventId` handling yet.

## Tooling

`tools/send-carrier-webhook.ps1` (Windows) and `tools/send-carrier-webhook.sh` (bash) sign a JSON file with a secret and post it.
