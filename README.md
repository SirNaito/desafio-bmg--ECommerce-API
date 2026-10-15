# Big E-Commerce API

A portfolio-focused REST API built with ASP.NET Core, Entity Framework Core and SQLite. It demonstrates JWT authentication, role-based authorization, product management, order creation, inventory reservation and simulated payments.

## Highlights

- **.NET 10 LTS** and ASP.NET Core Web API.
- JWT bearer authentication with issuer, audience, lifetime and signing-key validation.
- Role-based access control for administrators and customers.
- PBKDF2-SHA256 password hashing with a unique cryptographic salt and constant-time comparison.
- SQLite persistence and database transactions for order creation.
- Server-side price calculation; clients send product IDs and quantities, never trusted prices.
- Automated tests and GitHub Actions CI.

## Requirements

- .NET 10 SDK.

## Run locally

```bash
dotnet restore BigEcommerce.sln
dotnet run --project Ecommerce.Api
```

The API listens on `http://localhost:5162` with the included launch profile. Open `http://localhost:5162/swagger` for interactive API documentation.

SQLite creates `ecommerce.db` automatically. Demo users and products are seeded only in Development.

### Development demo accounts

| Role | Username | Password |
| --- | --- | --- |
| Administrator | `Admin` | `Pass123@` |
| Customer | `Gabriel` | `Pass123@` |
| Customer | `Sayo` | `Pass@123` |

These accounts and the development JWT secret are for local demos only. Outside Development, configure `Jwt__Secret`, `Jwt__Issuer` and `Jwt__Audience` through environment variables or a secret manager. The app refuses to start if the signing secret is missing or shorter than 32 bytes.

## Run tests

```bash
dotnet test BigEcommerce.sln --configuration Release
```

Tests use an isolated SQLite in-memory database and do not modify the local database file.

## API overview

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/auth/login` | Public | Returns a JWT for valid credentials. |
| `GET` | `/products` | Public | Lists the catalog. |
| `POST` | `/products` | Admin | Creates a product. |
| `PUT` | `/products/{id}` | Admin | Updates product details. |
| `DELETE` | `/products/{id}` | Admin | Deletes a product. |
| `POST` | `/orders` | Authenticated | Creates an order and reserves inventory. |
| `GET` | `/orders/my` | Authenticated | Lists the current user's orders. |
| `POST` | `/payments/pix/{orderId}` | Owner or Admin | Simulates Pix payment. |
| `POST` | `/payments/card/{orderId}` | Owner or Admin | Simulates card payment. |

For protected endpoints, send `Authorization: Bearer <token>` using the token returned by login.

## Business rules

The server calculates totals from current database prices and validates stock before changing inventory. Repeated product IDs are merged, and order plus stock changes are saved inside a database transaction. Failed validation does not decrement inventory. Only the order owner or an administrator can pay for an order, and payment confirmation is allowed only while an order is `PendingPayment`.

**Payments are simulated**: there is no bank, Pix provider or card processor integration, and no card details are collected or stored.

## Scope and next steps

This is an educational portfolio project, not a production payment platform. A production deployment should add database migrations, rate limiting, account provisioning, monitoring and integration with a real payment provider. `EnsureCreated` is used to keep local setup simple.
