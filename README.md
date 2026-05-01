# Transactions Webhook API

A minimal .NET 8 service that ingests third-party transaction webhooks, ensures idempotency, and stores derived financial records in PostgreSQL.

---

## Endpoint

```
POST /webhooks/transactions
```

**Request Body:**
```json
{
  "transactionId": "txn_123",
  "amount": 100.00,
  "currency": "USD",
  "merchant": "Acme Corp",
  "timestamp": "2024-01-01T12:00:00Z"
}
```

**Responses:**
- `201 Created` — New transaction stored with derived fields
- `200 OK` — Duplicate, already processed
- `400 Bad Request` — Invalid payload

---

## Database Schema

```sql
CREATE TABLE "Transactions" (
    "Id"                     UUID         PRIMARY KEY DEFAULT gen_random_uuid(),
    "ExternalTransactionId"  TEXT         NOT NULL UNIQUE,
    "Amount"                 NUMERIC      NOT NULL,
    "Currency"               TEXT         NOT NULL,
    "Merchant"               TEXT         NOT NULL,
    "Timestamp"              TIMESTAMPTZ  NOT NULL,
    "CalculatedFee"          NUMERIC      NOT NULL,
    "NetAmount"              NUMERIC      NOT NULL,
    "CreatedAt"              TIMESTAMPTZ  NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX idx_transactions_external_id ON "Transactions" ("ExternalTransactionId");
```

---

## Explanation *(~250 words)*

This service exposes a single `POST /webhooks/transactions` endpoint. When a payload arrives, it first validates the request using FluentValidation, rejecting any malformed data with a `400` before any business logic executes.

If valid, the service performs an idempotency pre-check by querying whether a transaction with the same `ExternalTransactionId` already exists. If so, it immediately returns `200 OK` without touching the database further. This covers the common case of webhook retries.

A 1.5% processing fee is then derived from the incoming amount (`CalculatedFee = Amount × FeeRate`), and the net amount is computed as `NetAmount = Amount - CalculatedFee`. The fee rate is externalised to `appsettings.json` via the Options pattern, allowing it to change per environment without recompilation.

The record is persisted to PostgreSQL via Entity Framework Core. A unique database index on `ExternalTransactionId` provides a second layer of protection against race conditions (two concurrent identical webhooks arriving simultaneously). If this constraint fires, EF Core raises a `DbUpdateException` which is translated into a domain `DuplicateTransactionException` and handled gracefully, returning `200 OK`.

On success, a `201 Created` response is returned with the structured output: `ExternalTransactionId`, `Amount`, `CalculatedFee`, and `NetAmount`.

---

## Assumptions

1. The external provider guarantees that `TransactionId` is a stable, globally unique string identifier — it is used as the idempotency key.
2. A flat 1.5% processing fee applies to all transactions regardless of currency or merchant.
3. Webhook payloads always contain valid UTC timestamps; no timezone conversion is applied server-side.

---

## Decision Justification

**1. Unique database index for idempotency over application-only checks**
A pre-check query stops known duplicates cheaply, but race conditions (two identical webhooks arriving within milliseconds) can slip past application-level guards. Adding a unique database index on `ExternalTransactionId` makes the guarantee atomic. The cost is a tiny index overhead that is negligible at any realistic webhook volume.

**2. Repository pattern for data access abstraction**
The service layer depends on `ITransactionRepository`, not EF Core directly. This means the integration tests can swap in a Moq double, keeping tests deterministic and fast without standing up a real database. It also makes a future persistence change (e.g., to a different ORM or event store) a one-class change.

---

## Rejected Alternative

**In-memory distributed cache (e.g., IMemoryCache) for idempotency**
Using a cache to track seen `TransactionId` values was considered because it would avoid an extra database query. It was rejected because the cache is ephemeral — a pod restart or scale-out event would clear it, allowing duplicates to slip through. Database-backed idempotency is the only reliable guarantee.

---

## Failure Scenario

**Database unavailable during webhook ingestion**
If PostgreSQL is down when a webhook arrives, `SaveChangesAsync` throws an exception. The external provider will receive a `500 Internal Server Error` and retry. Because idempotency is enforced at the database level, the retry is safe — when the database recovers, the first successful write will persist and all subsequent retries will return `200 OK`. No data is lost and no duplicates are created.

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- PostgreSQL
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef`

### 1. Configure
Update `appsettings.json` with your connection string and fee rate:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=transactions_db;Username=postgres;Password=postgres"
  },
  "TransactionSettings": {
    "FeeRate": 0.015
  }
}
```

### 2. Apply Migrations
Run the EF Core migration to create the database schema:
```bash
dotnet ef database update --project Transactions.Data --startup-project Transactions.API
```
This will create the `Transactions` table with the unique index on `ExternalTransactionId`.

### 3. Run
```bash
dotnet run --project Transactions.API
```
Swagger UI available at `/swagger`.

### 4. Test
```bash
dotnet test
```
