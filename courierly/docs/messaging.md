# Messaging

## Queue

Azure Service Bus queue `shipment-status`. Producers: the rider app backend and internal operations tools (both outside this repo; `tools/Courierly.Publisher` stands in for them). Carrier webhooks do not go through this queue: the webhook endpoint applies carrier status changes itself (see `carrier-webhook-contract.md`), using the same allowed transitions. Consumer: the `ShipmentStatusChanged` function in `Courierly.Functions`, which calls `ShipmentStatusHandler`.

## Message body

```json
{
  "shipmentId": 3,
  "tenantId": 1,
  "status": "InTransit",
  "sequence": 4,
  "occurredAt": "2026-11-12T08:15:00Z"
}
```

- `sequence` increases with every status change of one shipment. A higher sequence is a newer status.
- The Service Bus `MessageId` is set by the producer and is unique per status change.

## Merchant callback

Locally, `tools/Courierly.FakeMerchant` plays every merchant: the seeded `CallbackUrl` values point at it (`http://localhost:5099/callbacks/...`). It logs each callback (`GET /received`) and can be told to fail (`POST /admin/fail?count=N`) or slow down (`POST /admin/slow?seconds=N`).

After a status change, the merchant is told by `POST` to `Merchant.CallbackUrl`:

```json
{ "shipmentReference": "SWC-1003", "status": "InTransit", "occurredAt": "2026-11-12T08:15:00Z" }
```

with header `X-Api-Key: <merchant api key>`. Merchants expect at most one callback per status change.

## Allowed status transitions

`Created → PickedUp → InTransit → Delivered`. `Created` or `PickedUp → Cancelled`. `Delivered` and `Cancelled` are final.

## Local and cloud options

- Local (default): `docker compose --profile bus up -d` starts the Service Bus emulator with `shipment-status` and `shipment-status-sessions`, plus Azurite (the storage the Functions host needs for `AzureWebJobsStorage=UseDevelopmentStorage=true`). Emulator connection string: `Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;`. Emulator state is lost when the container restarts.
- Optional: Pluralsight Cloud Sandbox (Azure), with a Standard-tier namespace and the queues created there.
- `tools/Courierly.Publisher` sends sample messages to either.
