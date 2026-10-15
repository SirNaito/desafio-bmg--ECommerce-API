# Big E-Commerce API

A portfolio project built with ASP.NET Core, Entity Framework Core and SQLite. It demonstrates authentication, authorization, product management, transactional order creation, stock reservation and simulated payments.

## Features

- .NET 10 LTS / ASP.NET Core Web API.
- JWT bearer authentication with issuer, audience, lifetime and signing-key validation.
- Role-based access control for administrators and customers.
- Salted PBKDF2-SHA256 password hashing with constant-time comparison.
- SQLite persistence with versioned EF Core migrations.
- Server-side pricing and transactional stock reservation.
- DTOs with request validation and consistent HTTP error responses.
- Unit tests, HTTP integration tests and GitHub Actions CI.
- Multi-stage Docker image and Docker Compose for a repeatable local demo.

## Run locally

Requirements: .NET 10 SDK.

```bash
dotnet restore BigEcommerce.sln
dotnet run --project Ecommerce.Api
```

With the included launch profile, the API is available at `http://localhost:5162` and Swagger at `http://localhost:5162/swagger`.

The application applies EF Core migrations at startup. In the Development environment only, it seeds demo users and sample products when their tables are empty.

### Development accounts

| Role | Username | Password |
| --- | --- | --- |
| Administrator | `Admin` | `Pass123@` |
| Customer | `Gabriel` | `Pass123@` |
| Customer | `Sayo` | `Pass@123` |

These credentials and the development JWT secret are for local demonstrations only. Outside Development, configure `Jwt__Secret`, `Jwt__Issuer` and `Jwt__Audience` using environment variables or a secret manager. Use a unique secret with at least 32 bytes.

## Run with Docker

Requirements: Docker Desktop or Docker Engine with Compose.

```bash
docker compose up --build
```

Open `http://localhost:8080/swagger`. The SQLite database is stored in the named `ecommerce-data` volume and survives container restarts. The Compose file uses Development mode and demo credentials only to make local exploration easy; do not use those settings for public deployment.

To override the development JWT secret in Bash:

```bash
JWT_SECRET="replace-with-a-unique-secret-at-least-32-bytes" docker compose up --build
```

## Run tests

```bash
dotnet test BigEcommerce.sln --configuration Release
```

The unit tests use an isolated SQLite in-memory database. Integration tests boot the real ASP.NET Core request pipeline with a separate in-memory SQLite database. Neither suite changes your local `ecommerce.db` file.

## API overview

| Method | Endpoint | Access | Description |
| --- | --- | --- | --- |
| `POST` | `/auth/login` | Public | Returns a JWT for valid credentials. |
| `GET` | `/products` | Public | Lists the catalog. |
| `GET` | `/products/{id}` | Public | Gets one product. |
| `POST` | `/products` | Admin | Creates a product. |
| `PUT` | `/products/{id}` | Admin | Updates a product. |
| `DELETE` | `/products/{id}` | Admin | Deletes a product if no order references it. |
| `POST` | `/orders` | Authenticated | Creates an order and reserves stock. |
| `GET` | `/orders/my` | Authenticated | Lists the current user's orders. |
| `GET` | `/orders/{id}` | Owner or Admin | Gets an order with ownership checks. |
| `POST` | `/payments/pix/{orderId}` | Owner or Admin | Simulates Pix payment. |
| `POST` | `/payments/card/{orderId}` | Owner or Admin | Simulates card payment. |

For protected endpoints, send `Authorization: Bearer <token>` using the token returned by login.

## Business rules

- The server calculates the price from the current product records; client input never includes a trusted price.
- Empty carts, invalid quantities, missing products and insufficient inventory are rejected.
- Duplicate product IDs are merged before validation and total calculation.
- Inventory and order creation are saved in a single database transaction.
- Only the order owner or an administrator can access or pay for an order.
- Payment can only be confirmed while the order is awaiting payment. A repeated attempt returns `409 Conflict`.
- Responses expose DTOs rather than database entities.

**Payments are simulated.** There is no integration with a bank, Pix provider or card processor, and the API does not collect or store card data.

## Architecture

```text
HTTP request
    |
    v
Controllers (authentication, authorization, input validation)
    |
    v
OrderService (business rules and transactions)
    |
    v
AppDbContext / Entity Framework Core
    |
    v
SQLite database (managed by migrations)
```

## Known limitations

This is an educational portfolio project, not a production payment platform. A production deployment should add rate limiting, account provisioning and recovery, operational monitoring, a real payment-provider integration and a reviewed database migration/deployment process. Development demo accounts are seeded only when the app runs in Development.
