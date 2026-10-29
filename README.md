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
2. *(Optional)* Active Azure Service Bus namespace connection string. If not provided or set to `UseDevelopmentEmulator=true`, the solution automatically falls back to **Resilient Local Mode** using high-performance `System.Threading.Channels` with identical Peek-Lock and Dead-Letter semantics.
3. *(Optional)* OpenWeatherMap API key. If omitted, the system seamlessly falls back to Open-Meteo geocoding and forecast REST endpoints without requiring an account or key.

---

## 🔒 Local Secrets & Connection Strings Setup (.NET User Secrets)

To protect sensitive keys and prevent accidental commits of credentials into Git, this solution uses **.NET Secret Manager (`dotnet user-secrets`)** for local development. In production or cloud deployments, use environment variables or Azure Key Vault.

All placeholders in `appsettings.json` are overridden at runtime by your local User Secrets store (`%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`).

### 1. Configure Secrets for Web API (`EnterpriseApp.Api`)

Navigate to the solution root or run commands directly targeting the project:

#### A. Azure Service Bus Connection String
To connect the messaging pipeline to an actual Azure Service Bus namespace queue:
1. In the [Azure Portal](https://portal.azure.com), navigate to your **Service Bus Namespace**.
2. Select **Shared access policies** (or go into your queue's shared access policies).
3. Select or create a policy with **Send** and **Listen** permissions (e.g. `RootManageSharedAccessKey` or a custom policy).
4. Copy the **Primary Connection String**.
5. Save it into User Secrets:

```powershell
dotnet user-secrets set "AzureServiceBus:ConnectionString" "Endpoint=sb://<your-namespace>.servicebus.windows.net/;SharedAccessKeyName=<policy-name>;SharedAccessKey=<your-key>" --project src/EnterpriseApp.Api
```

> [!TIP]
> If you do not have an Azure Service Bus namespace yet, you can leave the default fallback or explicitly configure local in-memory emulation:
> ```powershell
> dotnet user-secrets set "AzureServiceBus:ConnectionString" "UseDevelopmentEmulator=true" --project src/EnterpriseApp.Api
> ```

#### B. JWT Secret Key
Set a cryptographically strong 256-bit (minimum 32-character) secret key used for signing HMAC-SHA256 tokens:

```powershell
dotnet user-secrets set "Jwt:SecretKey" "EnterpriseSuperSecretSecureKeyForJwtTokens2026_WithMinimumLengthRequirement!" --project src/EnterpriseApp.Api
```

#### C. Database Connection String (Optional / SQLite)
By default, the SQLite database `EnterpriseApp.db` is used. If you wish to customize its filename or path:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Data Source=EnterpriseApp.db" --project src/EnterpriseApp.Api
```

#### D. Weather API Key (Optional)
If you have an OpenWeatherMap API key:

```powershell
dotnet user-secrets set "WeatherApi:ApiKey" "<your-openweathermap-api-key>" --project src/EnterpriseApp.Api
```

---

### 2. Verify Configured Secrets

You can inspect the active secrets configured in your local development store at any time:

```powershell
dotnet user-secrets list --project src/EnterpriseApp.Api
```

---

### Base Configuration Template (`src/EnterpriseApp.Api/appsettings.json`)

`appsettings.json` contains safe defaults and non-sensitive structure:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=EnterpriseApp.db"
  },
  "Jwt": {
    "SecretKey": "YOUR_JWT_SECRET_KEY_PLACEHOLDER_STORED_IN_USER_SECRETS_STORE",
    "Issuer": "EnterpriseAppApi",
    "Audience": "EnterpriseAppClients",
    "ExpirationHours": 8
  },
  "AzureServiceBus": {
    "ConnectionString": "UseDevelopmentEmulator=true",
    "QueueName": "enterprise-messages"
  },
  "WeatherApi": {
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
> **Result:** 68 tests passing (0 failures, 0 skipped).

### 3. Start the ASP.NET Core Web API
In a terminal window:
```powershell
dotnet run --project src/EnterpriseApp.Api --launch-profile https
```
- **Base URLs:** `https://localhost:7002` / `http://localhost:5075`
- **Swagger / OpenAPI Documentation:** `https://localhost:7002/swagger`

### 4. Start the Blazor Interactive UI
In a second terminal window:
```powershell
dotnet run --project src/EnterpriseApp.BlazorUI --launch-profile https
```
- **Blazor Portal:** `https://localhost:7017` (or `http://localhost:5268`)
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
