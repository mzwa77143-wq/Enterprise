# Enterprise .NET 10 Clean Architecture Solution

A production-ready, enterprise-grade multi-project solution developed in **C# 13** and **.NET 10**. This solution strictly enforces **Clean Architecture** and the **Dependency Inversion Principle**, secures communications with **JWT Authentication & Role-Based Authorization**, integrates **Azure Service Bus** enterprise messaging with peek-lock completion and dead-letter handling, provides interactive **Blazor** user interfaces for all modules, and includes an exhaustive automated test suite.

---

## 🏗️ Architecture & Solution Structure

Dependencies point **strictly inward**:
$$\text{EnterpriseApp.Domain} \longleftarrow \text{EnterpriseApp.Application} \longleftarrow \text{EnterpriseApp.Infrastructure} \longleftarrow \text{EnterpriseApp.Api / BlazorUI}$$

```
C:\dev\Clientele 2
│   EnterpriseSolution.slnx               # Master solution file (.NET 10 SLNX format)
│   PROJECT_CONTEXT.md                    # Structured LLM/Developer architecture context
│   README.md                             # Comprehensive technical manual & quick start
│
├── src
│   ├── EnterpriseApp.Domain              # [Core Layer 1] Entities, Value Objects, Domain Exceptions
│   │   ├── Common                        # BaseEntity, Auditable properties
│   │   ├── Entities                      # Book, Customer, Product, User, FactorialTask, ServiceBusMessage
│   │   └── Exceptions                    # DomainException, EntityNotFoundException, ValidationException
│   │
│   ├── EnterpriseApp.Application         # [Core Layer 2] CQRS/Use-Cases, DTOs, Interfaces, Validators
│   │   ├── DTOs                          # Auth, Customer, Factorial, Library, Messaging, Product, Weather
│   │   ├── Interfaces                    # Repositories, Security, Services, Messaging contracts
│   │   ├── Services                      # BookService, CustomerService, ProductService, AuthService
│   │   └── Validators                    # FluentValidation rules for all request models
│   │
│   ├── EnterpriseApp.Infrastructure      # [Infrastructure Layer] DB, Messaging, External APIs, Security
│   │   ├── Data                          # EnterpriseDbContext (EF Core), DbInitializer (Seeds)
│   │   ├── Messaging                     # AzureServiceBusProducer, ConsumerHostedService, LocalQueue
│   │   ├── Repositories                  # Concrete EF Core repositories (Book, Customer, Product, User)
│   │   ├── Security                      # JwtTokenService (HMAC-SHA256), PasswordHasher (PBKDF2)
│   │   └── Services                      # FactorialCalculator (SemaphoreSlim), WeatherService (HttpClient)
│   │
│   ├── EnterpriseApp.Api                 # [Presentation Layer - Web API]
│   │   ├── Controllers                   # Auth, Books, Customers, Factorial, Messaging, Products, Weather
│   │   ├── Middleware                    # ExceptionHandlingMiddleware (RFC 7807 ProblemDetails)
│   │   ├── appsettings.json              # Connection strings, JWT configuration, Azure Service Bus
│   │   └── Program.cs                    # DI wireup, JWT Bearer middleware, Swagger OpenAPI
│   │
│   └── EnterpriseApp.BlazorUI            # [Presentation Layer - Front-End UI]
│       ├── Layout                        # MainLayout, NavMenu (Interactive Sidebar)
│       ├── Pages                         # Home, Library, Factorial, ServiceBusMonitor, Customers, Products, Weather
│       ├── Services                      # ApiClientService, CustomAuthenticationStateProvider
│       ├── appsettings.json              # Backend API base URL configuration
│       └── Program.cs                    # Blazor Interactive Server services, CascadingAuthState
│
└── tests
    └── EnterpriseApp.UnitTests           # Automated Test Suite (67 tests, 100% passing)
        ├── Domain                        # Book, Customer, Product invariant rules
        ├── Application
        │   ├── Services                  # BookService, CustomerService, ProductService, AuthService
        │   └── Validators                # FluentValidation rule assertions
        ├── MultiThreading                # Factorial concurrency, SemaphoreSlim, BigInteger precision
        ├── Messaging                     # Producer-consumer, Peek-Lock completion, Dead-Letter queue
        └── Security                      # JWT claims generation, PBKDF2 salt uniqueness & verification
```

---

## 🚀 The Six Enterprise Modules

| # | Module | Architectural Implementation & Highlights |
|---|---|---|
| **1** | **Simple Library Management** | Clean Architecture backend with EF Core, ISBN-10/13 validation, publication year restrictions, and an interactive Blazor search and catalog manager. |
| **2** | **Multi-threaded Factorial Calculator** | Thread-safe calculation service with `SemaphoreSlim` concurrency throttling (1-32 threads), arbitrary precision `BigInteger`, telemetry metrics (thread IDs, elapsed ms), and live Blazor streaming. |
| **3** | **Azure Service Bus Pipeline** | Enterprise messaging producer (`ServiceBusSender`) and background consumer (`ServiceBusProcessor`) with peek-lock completion (`CompleteMessageAsync`), dead-letter queue routing (`DeadLetterMessageAsync`), and real-time Blazor monitor. Also features a resilient local peek-lock fallback for zero-configuration local execution. |
| **4** | **Customer CRUD Application** | Full EF Core CRUD application protected with **JWT Bearer Authentication**. Blazor data grid with modal dialogs, phone/email validation, and 1-click test login helpers. |
| **5** | **RESTful Product API & Catalog** | Public read endpoints (`GET /api/products`) with mutations (`POST`, `PUT`, `DELETE`) guarded by `[Authorize(Roles = "Admin,Manager")]`. Blazor catalog with dynamic role-based rendering and instant role elevation switcher. |
| **6** | **Weather API Consumer** | Resilient weather aggregator using `IHttpClientFactory` with automatic fallback to Open-Meteo geocoding and forecast REST endpoints when no OpenWeatherMap key is supplied. Interactive Blazor UI with Celsius/Fahrenheit toggle. |

---

## 🔐 Security & Seeded Test Accounts

The database automatically seeds test accounts on first launch using cryptographic PBKDF2 password hashing (100,000 iterations):

| Username | Password | Role | Permissions |
|---|---|---|---|
| `admin` | `Admin@123456` | **Admin** | Full access to Customers, Products (Create/Update/Delete), Library, Messaging, and Factorial. |
| `manager` | `Manager@123456` | **Manager** | Full access to Customers, Products (Create/Update), Library, Messaging, and Factorial. |
| `user` | `User@123456` | **User** | Read-only access to Products; full access to Customers, Library, Messaging, and Factorial. |

---

## ⚙️ Prerequisites & Setup

1. **.NET 10 SDK** (v10.0.100 or later).
2. *(Optional)* Active Azure Service Bus namespace connection string. If not provided, the solution automatically operates in **Resilient Local Mode** using high-performance `System.Threading.Channels` with identical Peek-Lock and Dead-Letter semantics.
3. *(Optional)* OpenWeatherMap API key. If not provided, the system seamlessly uses the Open-Meteo geocoding and forecast API without requiring an account or key.

### Configuration (`src/EnterpriseApp.Api/appsettings.json`)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=enterprise.db"
  },
  "Jwt": {
    "SecretKey": "EnterpriseSuperSecretSecureKeyForJwtTokens2026_WithMinimumLengthRequirement!",
    "Issuer": "EnterpriseAppApi",
    "Audience": "EnterpriseAppClients",
    "ExpirationHours": 8
  },
  "AzureServiceBus": {
    "ConnectionString": "UseDevelopmentEmulator=true",
    "QueueName": "enterprise-messages"
  },
  "Weather": {
    "ApiKey": ""
  }
}
```

---

## 🔨 Build, Test & Run Instructions

### 1. Build the Entire Solution
```powershell
dotnet build EnterpriseSolution.slnx
```

### 2. Execute the Automated Test Suite
```powershell
dotnet test EnterpriseSolution.slnx
```
> **Result:** 67 tests passing (0 failures, 0 skipped).

### 3. Start the ASP.NET Core Web API
In a terminal window:
```powershell
dotnet run --project src/EnterpriseApp.Api
```
- **Base URL:** `http://localhost:5000` / `https://localhost:5001`
- **Swagger / OpenAPI Documentation:** `http://localhost:5000/swagger`

### 4. Start the Blazor Interactive UI
In a second terminal window:
```powershell
dotnet run --project src/EnterpriseApp.BlazorUI
```
- **Blazor Portal:** `http://localhost:5200`
- Navigate to:
  - `/` - Enterprise Architecture Dashboard & Module Quick-Links
  - `/library` - Library Management System (Search, Add, Delete)
  - `/factorial` - Multi-Threaded Factorial Concurrent Calculator
  - `/servicebus` - Azure Service Bus Producer-Consumer Pipeline Monitor
  - `/customers` - Customer CRUD Application (JWT Protected)
  - `/products` - Product Catalog with Role-Based Access Control
  - `/weather` - Live External Weather Consumer

---

## 🧪 Testing Coverage Overview

The test project in `tests/EnterpriseApp.UnitTests` includes:
- **`Domain/BookTests.cs`**: Invariant tests, validation of ISBN, publication year bounds, and empty title/author guards.
- **`Domain/CustomerAndProductTests.cs`**: Customer email normalization and product non-negative pricing validation.
- **`Application/Validators/ValidatorTests.cs`**: FluentValidation tests for ISBN checksums, telephone regex, character lengths, and authentication credentials.
- **`Application/Services/ServiceTests.cs`**: Mocked repository testing for `BookService`, `CustomerService` (duplicate email detection), `ProductService`, and `AuthService` (PBKDF2 verification and JWT token issuance).
- **`MultiThreading/FactorialCalculatorTests.cs`**: Concurrency execution under `SemaphoreSlim`, thread ID dispersion, cancellation token responsiveness, and `BigInteger` precision.
- **`Messaging/ServiceBusMessagingTests.cs`**: `AzureServiceBusProducerService` message enqueuing, background worker consumption, peek-lock acknowledgment (`CompleteMessageAsync`), and dead-letter routing (`DeadLetterMessageAsync`).
- **`Security/JwtTokenServiceTests.cs`**: Cryptographic PBKDF2 salt uniqueness, password verification, and JWT claims inspection (`sub`, `unique_name`, `email`, `role`).
