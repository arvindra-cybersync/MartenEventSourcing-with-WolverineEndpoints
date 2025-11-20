# Event-Driven Order Management API

Built with **.NET 8**, **Wolverine**, and **Marten EventStore**.

## 🚀 Overview

This project implements an **event‑driven, fully event‑sourced Order
Management System** using:

-   **Marten** as the event store + projections engine\
-   **Wolverine** as the command bus & HTTP endpoint framework\
-   **CQRS architecture** with separate command and query pipelines\
-   **Inline & async projections** for materialized views\
-   **Domain events & aggregates** for strong consistency\
-   **Multi‑stream projections** for product sales tracking\
-   **Wolverine HTTP Endpoints** for minimal APIs with automatic
    validation and messaging

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

### 1. Update connection string

In `appsettings.json`:

``` json
"ConnectionStrings": {
  "MartenDb": "Host=localhost;Port=5432;Database=MartenEventStore;Username=postgres;Password=yourpass"
}
```

### 2. Run PostgreSQL

Marten requires PostgreSQL.

### 3. Start the API

    dotnet run

### 4. Open Swagger

    https://localhost:7257/swagger

------------------------------------------------------------------------

## 🧪 Testing Endpoints

### Create Order

    POST /api/orders
    {
      "customerId": "guid",
      "description": "test"
    }

### Add Item

    POST /api/orders/{orderId}/items
    {
      "itemId": "guid",
      "itemName": "Product A",
      "quantity": 10
    }

------------------------------------------------------------------------

## 📚 Summary

This project demonstrates: - Clean CQRS architecture - True event
sourcing with Marten - Multi-stream & single-stream projections -
Wolverine command dispatching - Full request/response observability -
Clean API surface using Wolverine HTTP
