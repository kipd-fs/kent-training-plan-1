# Courierly

Multi-tenant last-mile delivery platform. Tenants are courier businesses (for example *Swift Cebu*, *Metro Haulers*). Merchants are the tenants' customers: shops that book deliveries and receive status callbacks.

## Projects

| Path | What it is |
|---|---|
| `src/Courierly.Core` | Domain model, EF Core `CourierlyDbContext`, `DeliveryFeeCalculator`, `ShipmentStatusHandler` (Service Bus message handling) |
| `src/Courierly.Api` | ASP.NET Core Web API: shipments, quotes, carrier webhook |
| `src/Courierly.Functions` | Azure Functions (isolated worker): `ShipmentStatusChanged`, triggered by the `shipment-status` queue |
| `tests/Courierly.Core.Tests` | xUnit test project |
| `tools/` | Webhook signing scripts, sample payloads, `Courierly.Publisher` (sends sample Service Bus messages), `Courierly.FakeMerchant` (plays the merchants' callback endpoints on port 5099) |
| `docs/` | Business rules and contracts: fee rules, API consumers, carrier webhook contract, messaging |

## Prerequisites

- .NET 10 SDK (`global.json` pins the 10.0 band)
- Docker (Docker Desktop on Windows, or a Pluralsight Cloud Server / Instant Terminal with Docker)
- Optional for the Functions project: Azure Functions Core Tools v4

## Run it

```bash
docker compose up -d sql          # SQL Server 2022 on localhost,1433
dotnet build
dotnet test
dotnet run --project src/Courierly.Api   # http://localhost:5080
```

In Development the API creates the `Courierly` database and seeds two tenants, three merchants, fee rules and seven shipments on first start.

The SA password in `docker-compose.yml` and `appsettings.json` is a throwaway value for a local container only. It is not a pattern to copy.

Try it:

```bash
curl -s http://localhost:5080/api/shipments -H "X-Tenant-Id: 1"
curl -s http://localhost:5080/api/shipments/3 -H "X-Tenant-Id: 1"
curl -s -X POST http://localhost:5080/api/quotes -H "Content-Type: application/json" \
  -d '{"tenantId":1,"zone":"A","weightKg":1.2,"promoCode":"SUKI10"}'
```

## Service Bus

See `docs/messaging.md`. Local emulator: `docker compose --profile bus up -d`. Send sample messages:

```bash
dotnet run --project tools/Courierly.Publisher -- --connection "<connection string>" --mode duplicate
```

## Functions locally

1. Storage for the Functions host: `docker compose --profile bus up -d azurite` (or the whole `bus` profile if you also want the local Service Bus emulator).
2. The merchants: `dotnet run --project tools/Courierly.FakeMerchant` (listens on http://localhost:5099; `GET /received` lists callbacks, `POST /admin/fail?count=N` makes the next N fail, `POST /admin/slow?seconds=N` slows every callback, `POST /admin/reset` clears it).
3. Make sure the `Courierly` database exists with the current schema (run the API once in Development, or apply your migrations).
4. Copy `src/Courierly.Functions/local.settings.json.example` to `local.settings.json` (git-ignored), point `ServiceBusConnection` at the emulator or your Azure namespace, then run `func start` from `src/Courierly.Functions`.
5. Send messages with `tools/Courierly.Publisher`.
