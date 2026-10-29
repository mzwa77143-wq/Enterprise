# Enterprise Solution - Project Context

This document is engineered to provide instant context for LLM agents, architects, and engineering teams interacting with this multi-project solution.

---

## 1. Project Overview
EnterpriseSolution is a production-ready, enterprise-grade distributed system written in C# and .NET 10 consisting of six cohesive modules unified under strict Clean Architecture and Dependency Inversion principles. The system delivers end-to-end capabilities spanning library cataloging, high-throughput asynchronous multi-threaded calculations, resilient Azure Service Bus enterprise messaging pipelines, JWT-secured customer management, role-authorized product inventory, and external live weather aggregation. All modules are seamlessly exposed through secure ASP.NET Core Web APIs and an interactive, real-time Blazor user interface with centralized telemetry and role-based access control.

---

## 2. Tech Stack
- **Languages & Frameworks:**
  - **C# 13 / .NET 10** (`net10.0`) across all backend, domain, infrastructure, and presentation assemblies.
  - **ASP.NET Core Web API:** RESTful endpoint controllers with OpenAPI/Swagger integration and ProblemDetails middleware.
  - **Blazor Interactive Server:** Component-driven, reactive web user interface with cascading authentication state, form validations, and asynchronous HTTP client services.
- **Data & Persistence:**
  - **Entity Framework Core 10.0.14:** Code-first relational ORM with automatic schema migrations and seed initializers.
  - **SQLite:** Cross-platform local database storage (`enterprise.db`), fully swappable for SQL Server / Azure SQL.
- **Enterprise Messaging:**
  - **Azure.Messaging.ServiceBus (v7.18.4):** High-reliability enterprise cloud message queuing with peek-lock message lifecycle (`CompleteMessageAsync`), dead-letter queue routing (`DeadLetterMessageAsync`), and exponential retry policies.
  - **Azure Bicep IaC:** Automated infrastructure-as-code templates in `infrastructure/bicep/` targeting Subscription `8419b0f2-ff8e-4fe0-924e-47223fdea478` ("Subscription 1").
  - **Resilient Dual-Mode In-Memory Fallback:** High-performance `System.Threading.Channels` peek-lock emulator enabling zero-configuration offline execution and unit testing.
- **Security & Identity:**
  - **JSON Web Tokens (JWT):** RFC 7519 compliant tokens signed via HMAC-SHA256 with role-based claims (`Admin`, `Manager`, `User`).
  - **Cryptography:** PBKDF2 password hashing utilizing RFC 2898 with cryptographically secure 128-bit salts and 100,000 iterations.
- **Concurrency & Compute:**
  - **`System.Numerics.BigInteger`:** Arbitrary-precision integer arithmetic for massive factorial calculations.
  - **`System.Threading.SemaphoreSlim`:** Thread-safe resource throttling and task parallelization with real-time thread ID tracking and cancellation tokens.
- **Validation & Testing:**
  - **FluentValidation (v11.11.0):** Strongly-typed business validation rules for all DTOs and command requests.
  - **xUnit (v2.9.3), FluentAssertions (v8.11.0), Moq (v4.21.0), EF Core In-Memory:** Automated unit and integration test suite.

---

## 3. Architectural Patterns
- **Clean Architecture & Dependency Inversion Principle (DIP):**
  Dependencies strictly point inward:
  $$\text{Domain} \longleftarrow \text{Application} \longleftarrow \text{Infrastructure} \longleftarrow \text{Presentation (API \& Blazor UI)}$$
  - `EnterpriseApp.Domain`: Zero external NuGet dependencies; contains domain entities, value objects, domain exceptions, and audit tracking.
  - `EnterpriseApp.Application`: Houses use cases, DTOs, service interfaces, repository contracts, and FluentValidation validators.
  - `EnterpriseApp.Infrastructure`: Implements data persistence (`EnterpriseDbContext`), Azure Service Bus integration, HTTP clients, cryptography, and threading services.
  - `EnterpriseApp.Api` & `EnterpriseApp.BlazorUI`: Outermost layer handling HTTP transport, JWT middleware, Swagger documentation, and client-side rendering.
- **Repository & Unit of Work Pattern:** Decouples business logic from persistence mechanisms, facilitating comprehensive test mocking.
- **Service Layer & DTO Mapping:** Enforces explicit data encapsulation and prevents entity leakage across API boundaries.
- **Options & Strategy Pattern:** Configurable provider fallback strategies for messaging (Azure Service Bus vs. local peek-lock emulator) and weather data (OpenWeatherMap vs. Open-Meteo geocoding fallback).
- **SOLID Principles:** Single-responsibility domain services, open-for-extension messaging pipelines, interface segregation across repositories, and strict dependency inversion throughout the DI container.

---

## 4. Key Workflows
1. **JWT Authentication & Authorization Flow:**
   - Client sends credentials via `/api/auth/login`.
   - `AuthService` verifies PBKDF2 hash using fixed-time cryptographic equality comparison.
   - `JwtTokenService` issues a signed bearer token containing `sub`, `unique_name`, `email`, and `role` claims.
   - `CustomAuthenticationStateProvider` stores token in local storage / session state, unpacks claims, and notifies Blazor cascades to dynamically re-render authorized navigation, buttons, and views.
2. **Simple Library Management Flow:**
   - User inputs book details; `CreateBookRequestValidator` validates ISBN-10/13 checksum patterns and publication years.
   - `BookService` creates a validated `Book` entity and persists it via `IBookRepository`.
   - Blazor dashboard provides live client-side and server-side search across Title, Author, and ISBN, along with modal-based additions and instant deletion.
3. **Multi-Threaded Factorial Calculation Flow:**
   - User submits a batch of integers and chooses a concurrency ceiling (1 to 32 threads).
   - `FactorialCalculatorService` coordinates tasks with `SemaphoreSlim`, dispatching workloads across the .NET thread pool.
   - As each `BigInteger` factorial resolves, elapsed execution time and managed thread IDs (`Environment.CurrentManagedThreadId`) are recorded and streamed live to the UI.
4. **Azure Service Bus Producer-Consumer Pipeline:**
   - Producer publishes payload through `AzureServiceBusProducerService` (using `ServiceBusSender` or resilient fallback queue).
   - `AzureServiceBusConsumerHostedService` receives message under Peek-Lock semantics.
   - If payload flags `SimulateDeadLetter`, message is routed to Dead-Letter Queue via `DeadLetterMessageAsync`. Otherwise, simulated enterprise work completes and message is acknowledged via `CompleteMessageAsync`.
   - Telemetry metrics (Enqueued, Completed, Dead-Lettered, Active Transport) stream to the Blazor Monitor.
5. **Customer CRUD Application:**
   - Fully protected under JWT Bearer authentication (`[Authorize]`).
   - Blazor UI verifies authentication status before rendering grid; includes 1-click test credential login helpers and full create, edit, and delete modals.
6. **Role-Restricted Product Catalog:**
   - Public read access (`GET /api/products`).
   - Mutations (`POST`, `PUT`, `DELETE`) are guarded by `[Authorize(Roles = "Admin,Manager")]`.
   - Blazor catalog automatically adjusts controls based on active user role, offering a convenient 1-click role switcher for immediate validation.
7. **Weather Consumer API Flow:**
   - Consumes live forecasts via `IHttpClientFactory`.
   - Dual-engine fallback: detects if an OpenWeatherMap API key is configured; if missing, automatically defaults to Open-Meteo geocoding and forecast REST endpoints, guaranteeing 100% functionality out of the box with Celsius/Fahrenheit conversion.

---

## 5. Current Status & Recent Accomplishments
- **Phase 1: Domain Core & Application Layer** — Completed
  - Created domain entities with invariant encapsulation (`Book`, `Customer`, `Product`, `User`, `WeatherReport`, `FactorialTask`, `ServiceBusMessageRecord`).
  - Implemented FluentValidation rule sets for all commands and requests.
  - Implemented application services (`BookService`, `CustomerService`, `ProductService`, `AuthService`).
- **Phase 2: Infrastructure & Persistence** — Completed
  - Configured EF Core `EnterpriseDbContext` with SQLite and automated database migration/seeding on startup.
  - Implemented resilient dual-mode `AzureServiceBusProducerService` and `AzureServiceBusConsumerHostedService`.
  - Built PBKDF2 cryptographic `PasswordHasher` and HMAC-SHA256 `JwtTokenService`.
  - Implemented `FactorialCalculatorService` using `SemaphoreSlim` and `BigInteger`.
  - Implemented resilient `WeatherService` with OpenWeatherMap and Open-Meteo fallback.
- **Phase 3: Presentation API & OpenAPI** — Completed
  - Built 7 ASP.NET Core API controllers with standardized ProblemDetails error handling.
  - Configured Swagger / OpenAPI with interactive JWT Bearer authorization support.
- **Phase 4: Interactive Blazor UI** — Completed
  - Built interactive Blazor Server user interface matching all 6 project specifications.
  - Implemented custom `CustomAuthenticationStateProvider` with live role elevation and token injection into HTTP clients.
  - Added clean Bootstrap 5 enterprise styling with cards, telemetry badges, modals, and responsive data grids.
- **Phase 5: Unit Testing & Verification** — Completed
  - Developed 67 unit and integration tests across 6 test suites covering Domain invariants, FluentValidation rules, Application services, Multi-threading, Azure Service Bus messaging, and JWT security.
  - Verified 100% test pass rate with zero warnings and zero build errors.

---

## 6. Known Limitations / Technical Debt
- **Local Azure Service Bus Connection:** In local development environments without an active Azure subscription, the system operates seamlessly via its built-in `LocalServiceBusQueue` peek-lock emulator. To bind to live Azure Service Bus, update `AzureServiceBus:ConnectionString` in `appsettings.json` or User Secrets.
- **SQLite Concurrency in High-Volume Integration Tests:** SQLite uses file-level locking; while ideal for local development, production deployment to distributed container clusters should switch the EF Core provider to Azure SQL or PostgreSQL.
- **Client-Side Blazor Token Storage:** In the current Blazor Interactive Server demo configuration, the JWT token is maintained in an in-memory session state for instant interactive evaluation without external JavaScript interop round-trips. For production WebAssembly deployment, transition to secure HTTP-only cookies or encrypted browser storage.

---

## 7. Glossary
- **Clean Architecture:** A software design philosophy pioneered by Robert C. Martin where business rules reside in the innermost layers, and technical frameworks (databases, UI, network protocols) reside in external rings with inward-pointing dependencies.
- **Dependency Inversion Principle (DIP):** High-level modules do not depend on low-level modules; both depend upon abstractions.
- **Peek-Lock Message Completion:** An enterprise messaging pattern where a consumer locks a message upon reading, preventing other consumers from processing it. The message remains on the queue until the worker explicitly calls `CompleteMessageAsync`, or gets abandoned / dead-lettered if processing fails.
- **Dead-Letter Queue (DLQ):** A secondary queue holding messages that cannot be processed successfully, protecting systems from poison messages.
- **SemaphoreSlim:** A lightweight synchronization primitive in .NET that limits the number of threads that can access a pool of resources concurrently.
- **BigInteger:** An arbitrarily large signed integer in `System.Numerics` whose value has no theoretical upper or lower bound.
- **JWT (JSON Web Token):** A compact, URL-safe means of representing claims between two parties, digitally signed using HMAC or RSA.
- **PBKDF2:** Password-Based Key Derivation Function 2, a key stretching algorithm that applies pseudorandom functions (e.g., HMAC-SHA256) along with a salt value across many iterations to resist brute-force attacks.
