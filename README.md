# Event-Driven Order Management API

Built with **.NET 8**, **Wolverine**, and **Marten EventStore**.

## 🚀 Overview

This project implements an **event‑driven, fully event‑sourced Order
Management System** with **enterprise-grade security** using:

-   **Marten** as the event store + projections engine
-   **Wolverine** as the command bus & HTTP endpoint framework
-   **CQRS architecture** with separate command and query pipelines
-   **Inline & async projections** for materialized views
-   **Domain events & aggregates** for strong consistency
-   **Multi‑stream projections** for product sales tracking
-   **Wolverine HTTP Endpoints** for minimal APIs with automatic validation and messaging
-   **JWT Authentication** for secure token-based authentication
-   **Role-Based Authorization** with fine-grained access control
-   **Rate Limiting** to prevent API abuse
-   **API Versioning** for backward compatibility
-   **Security Headers** for defense against common web vulnerabilities

------------------------------------------------------------------------

## 🧩 Architecture

### 1. **Domain Layer**

Contains: - Aggregates (Order, etc.) - Domain Events (`OrderCreated`,
`OrderItemAdded`, `OrderShipped`, `OrderCancelled`) - Commands
(`CreateOrderCommand`, `AddOrderItemCommand`, ...)

All business logic is encapsulated inside the **Order aggregate**.

------------------------------------------------------------------------

### 2. **Application Layer**

Contains Wolverine message handlers:

-   `OrderCommandHandlers`
    -   Applies commands to aggregates
    -   Loads Marten event streams
    -   Appends events
    -   Returns results or validation errors

This layer ensures **write-side consistency**.

------------------------------------------------------------------------

### 3. **Infrastructure Layer**

Includes:

#### 📌 **Marten Configuration**

-   Event store
-   StoreOptions
-   Inline projections
-   Async daemon
-   Document schemas for read models

#### 📌 **Projections**

1.  **OrderSummaryProjection** (Single Stream Aggregation)
    -   Maintains current order state
    -   Tracks totals, shipped/cancelled status
2.  **ProductSalesProjection** (Multi-Stream Projection)
    -   Aggregates product sales across multiple orders
    -   Uses slicing by `ItemId`

#### 📌 **Read Models**

-   OrderSummary
-   ProductSales

------------------------------------------------------------------------

### 4. **WebApi Layer**

#### ✔ Wolverine HTTP Endpoints

Minimal endpoints such as:

    POST /api/orders
    POST /api/orders/{orderId}/items
    POST /api/orders/{orderId}/ship
    POST /api/orders/{orderId}/cancel

Each endpoint: - Accepts command DTO - Auto-generates `OrderId` on
creation - Injects Wolverine `IMessageBus` - Dispatches commands with
updated timestamps

#### ✔ Query Endpoints

    GET /api/orders
    GET /api/orders/{id}
    GET /api/products/sales

------------------------------------------------------------------------

## 🧬 Event Sourcing Flow (Write Side)

### Example: Adding an order item

1.  `POST /api/orders/{orderId}/items`
2.  Wolverine invokes handler:
3.  Handler loads event stream → Order aggregate reconstructs from
    events
4.  Aggregate applies business rules
5.  Produces `OrderItemAdded`
6.  Marten appends event to event store
7.  Inline projections update read models
8.  Async projections update long-running read models

------------------------------------------------------------------------

## 📦 Projections

### OrderSummaryProjection

Single stream projection for each order:

-   Description
-   Total items
-   Shipping status
-   Cancellation status

### ProductSalesProjection

Multi‑stream projection for product sales:

-   Sliced by `ItemId`
-   Updates aggregated sales numbers
-   Tracks total sold quantity
-   Updates last sale timestamp

------------------------------------------------------------------------

## 🔍 Request/Response Logging Middleware

A custom middleware logs: - Complete request body - Complete response
body - Timestamps - Route, method, headers

Useful for debugging projection behavior and endpoint flows.

------------------------------------------------------------------------

## 🔧 Tools Integration

### Marten

-   Event Store\
-   Inline projections\
-   Async daemon\
-   Lightweight sessions

### Wolverine

-   Command buses
-   Durable queues
-   Minimal API routing
-   Automatic model binding

### Swagger

-   Custom configuration
-   Hide schemas
-   Clean API interface

------------------------------------------------------------------------

## ▶ Running the Application

### 1. Configure Database Connection (User Secrets)

**IMPORTANT**: Never hardcode credentials. Use user secrets:

```bash
cd WebApi
dotnet user-secrets set "ConnectionStrings:MartenDb" "Host=localhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=yourpass"
```

### 2. Configure JWT Secret Key (REQUIRED)

```bash
cd WebApi
dotnet user-secrets set "JwtSettings:SecretKey" "YourSecretKeyAtLeast32CharactersLong!"
```

### 3. Ensure PostgreSQL is Running

Marten requires PostgreSQL.

### 4. Restore Packages & Start the API

```bash
dotnet restore
dotnet run --project WebApi
```

### 5. Open Swagger

Navigate to: `https://localhost:7257/swagger`

### 6. Authenticate in Swagger

1. Click the **"Authorize"** 🔒 button
2. Login at `/api/auth/login` using demo credentials:
   - **admin** / admin123 (full access)
   - **manager** / manager123 (order management)
   - **viewer** / viewer123 (read-only)
3. Copy the JWT token from the response
4. Paste it in the Authorize dialog: `Bearer YOUR_TOKEN`
5. Click "Authorize"

For detailed authentication docs, see [AUTHENTICATION_GUIDE.md](./AUTHENTICATION_GUIDE.md)

------------------------------------------------------------------------

## 🧪 Testing Endpoints

### 1. Get Authentication Token

```bash
POST /api/auth/login
Content-Type: application/json

{
  "username": "admin",
  "password": "admin123"
}
```

### 2. Create Order (Requires Authentication)

```bash
POST /api/orders
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "description": "Test Order"
}
```

### 3. Add Item (Requires Authentication)

```bash
POST /api/orders/{orderId}/items
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "itemId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "itemName": "Product A",
  "quantity": 10
}
```

### 4. Get Orders (Requires Authentication)

```bash
GET /api/orders
Authorization: Bearer YOUR_JWT_TOKEN
```

------------------------------------------------------------------------

## 🔒 Security Features

This project implements **production-ready security**:

### Authentication & Authorization ✅
- **JWT Bearer Token** authentication
- **Role-based authorization** with policies (Admin, OrderManager, OrderViewer)
- **Secure token validation** with configurable expiration
- **Demo authentication endpoint** for testing (replace for production)

### API Protection ✅
- **Rate Limiting**: 100 req/min, 1000 req/hour per IP
- **Input Validation**: All commands validated with data annotations
- **HTTPS Enforcement**: Redirects HTTP to HTTPS in production
- **Security Headers**: X-Frame-Options, CSP, X-Content-Type-Options, etc.

### Data Protection ✅
- **Sensitive Data Sanitization**: Logs automatically redact passwords, tokens, etc.
- **No Hardcoded Credentials**: User secrets for development, Key Vault for production
- **Proper Error Handling**: No information disclosure in error messages

### Monitoring ✅
- **Health Checks**: `/health` and `/health/ready` endpoints
- **Structured Logging**: With sanitization of sensitive fields
- **API Versioning**: Version 1.0 with backward compatibility support

For complete security documentation, see:
- [SECURITY_SETUP.md](./SECURITY_SETUP.md) - Security configuration guide
- [AUTHENTICATION_GUIDE.md](./AUTHENTICATION_GUIDE.md) - Authentication usage guide

------------------------------------------------------------------------

## 📚 Summary

This project demonstrates:
- Clean CQRS architecture
- True event sourcing with Marten
- Multi-stream & single-stream projections
- Wolverine command dispatching
- Full request/response observability
- Clean API surface using Wolverine HTTP
- **Enterprise-grade security** with JWT auth, rate limiting, and comprehensive protection
