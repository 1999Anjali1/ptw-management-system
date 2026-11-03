# PTW Management System

A full-stack **Permit-to-Work (PTW)** application for oil & gas facilities, built with an **ASP.NET Core 10 Web API** backend and an **Angular 22** frontend.

It digitises the paper / ePTW process used on LNG and process plants: a contractor raises a permit, the **Area Authority** approves it, the **Permit Issuer** verifies gas tests and energy isolations before issuing it, and every step is recorded in an audit trail.

![.NET](https://img.shields.io/badge/.NET-10-512BD4) ![Angular](https://img.shields.io/badge/Angular-22-DD0031) ![EF Core](https://img.shields.io/badge/EF%20Core-10-6DB33F) ![SQL Server](https://img.shields.io/badge/SQL%20Server%20%7C%20SQLite-CC2927)

---

## Features

| Area | What it does |
|---|---|
| **Permit workflow** | Draft → Submitted → Approved → Active ⇄ Suspended → Closed, plus Reject / Cancel. Implemented as a tested state machine. |
| **Role-based access** | Requester, Area Authority, Permit Issuer, Admin - JWT authentication with role claims. |
| **Segregation of duties** | You cannot approve or issue a permit you raised yourself. |
| **Gas testing** | O₂, LEL, H₂S, CO readings checked against limits. Hot work needs 0% LEL. Hot work and confined space entry cannot be issued without a passed test taken in the last 2 hours. |
| **Auto-suspension** | A failed gas test on an active permit suspends it immediately. |
| **Energy isolation (LOTO)** | Isolation register with lock numbers. Electrical work and confined space entry require an applied isolation before issue; a permit cannot be closed while isolations are still applied. |
| **SIMOPS check** | Warns when other live permits overlap in the same area and time window. |
| **Audit trail** | Every status change, gas test and isolation is logged with user, time and comment. |
| **Dashboard** | Live work on site, pending approvals, permits ready to issue, overdue permits, open isolations. |
| **Validation** | Max 24-hour validity, data annotations on DTOs, RFC 7807 ProblemDetails errors. |

## Tech stack

**Backend** - ASP.NET Core 10 Web API · Entity Framework Core 10 (SQLite for zero-setup dev, SQL Server for Docker/production) · JWT Bearer authentication · ASP.NET Core Identity password hashing · Swagger / OpenAPI · xUnit unit + integration tests (`WebApplicationFactory`)

**Frontend** - Angular 22 standalone components · Signals · Reactive Forms · functional HTTP interceptor and route guards · lazy-loaded routes · Vitest

**DevOps** - Dockerfiles for API and UI (nginx) · docker-compose with SQL Server 2022 · GitHub Actions CI building and testing both apps

## Project structure

```
ptw-management-system/
├── backend/
│   ├── PtwSystem.sln
│   ├── src/PtwSystem.Api/
│   │   ├── Controllers/        # HTTP endpoints (thin - they call services)
│   │   ├── Services/           # Business logic: PermitService, AuthService, TokenService…
│   │   ├── Domain/
│   │   │   ├── Entities/       # Permit, GasTest, Isolation, PermitHistory, User
│   │   │   ├── Enums/          # PermitStatus, PermitType, UserRole…
│   │   │   └── Rules/          # PermitWorkflow (state machine), GasTestEvaluator
│   │   ├── Data/               # AppDbContext (EF Core) + DbSeeder (demo data)
│   │   ├── Dtos/               # Request/response models sent over the API
│   │   ├── Common/             # Exceptions, global error handler, current user, JWT options
│   │   ├── Extensions/         # Service registration (DI, auth, Swagger, database)
│   │   └── Program.cs          # App startup and HTTP pipeline
│   └── tests/PtwSystem.Tests/  # xUnit: workflow rules, gas limits, API integration tests
├── frontend/
│   └── src/app/
│       ├── core/               # models, AuthService, API services, interceptor, guards
│       ├── layout/             # sidebar shell
│       ├── pages/              # login, dashboard, permit-list, permit-form, permit-detail
│       └── shared/             # status badge, labels
├── docker-compose.yml
└── .github/workflows/ci.yml
```

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org)
- (optional) Docker Desktop for the SQL Server setup

### 1. Run the API

```bash
cd backend/src/PtwSystem.Api
dotnet run
```

- API: http://localhost:5089
- Swagger UI: http://localhost:5089/swagger
- A SQLite database `ptw.db` is created and filled with demo data on first run. Delete the file to reset.

### 2. Run the Angular app

```bash
cd frontend
npm install
npm start
```

Open http://localhost:4200 and click one of the demo accounts.

### Demo accounts

All passwords: `Password@123`

| Role | Email | Try this |
|---|---|---|
| Requester | requester@ptwdemo.com | Raise a hot work permit and submit it |
| Area Authority | areaauthority@ptwdemo.com | Approve or reject submitted permits |
| Permit Issuer | issuer@ptwdemo.com | Record a gas test, apply isolations, issue / suspend / close |
| Admin | admin@ptwdemo.com | Can do everything, manage users via API |

**Try the safety rules:** log in as the Issuer, open **PTW-…-0002 (confined space entry)** and click *Issue permit* - it is blocked because the gas test is older than 2 hours. Record a new passing gas test and issue again.

### Run with Docker (SQL Server)

```bash
docker compose up --build
```

Open http://localhost:8080. The API runs against SQL Server 2022 in a container.

### Run the tests

```bash
cd backend && dotnet test        # 25+ xUnit tests incl. full API workflow
cd frontend && npx ng test --watch=false
```

## API overview

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/auth/login` | Get a JWT token |
| GET | `/api/auth/me` | Current user |
| GET | `/api/permits?status=&type=&search=&mine=&page=` | Search permits (paged) |
| GET | `/api/permits/{id}` | Permit details, safety records, history, allowed actions |
| POST | `/api/permits` | Raise a permit (Draft) |
| PUT | `/api/permits/{id}` | Edit a draft |
| POST | `/api/permits/{id}/actions` | `{ "action": "Approve", "comment": "…" }` - workflow transition |
| POST | `/api/permits/{id}/gas-tests` | Record a gas test |
| POST | `/api/permits/{id}/isolations` | Apply an isolation |
| POST | `/api/permits/{id}/isolations/{isolationId}/remove` | Remove an isolation |
| GET | `/api/permits/{id}/conflicts` | SIMOPS conflicts |
| GET | `/api/dashboard/summary` | Dashboard KPIs |
| GET/POST | `/api/users` | User management (Admin) |

Blocked actions return **422** with the reasons, e.g.

```json
{
  "title": "Request blocked by PTW rules",
  "status": 422,
  "errors": ["HotWork requires a gas test before work can start."]
}
```

## How a request flows through the backend

```
Angular  ──HTTP + JWT──►  PermitsController  ──►  PermitService  ──►  PermitWorkflow (rules)
                                                        │
                                                        └──►  AppDbContext (EF Core)  ──►  SQLite / SQL Server
```

1. **`Program.cs`** registers services in the dependency-injection container and builds the middleware pipeline (exception handler → CORS → authentication → authorization → controllers).
2. **Controllers** only handle HTTP: they receive a DTO, call a service and return the result. `[ApiController]` validates the DTO automatically.
3. **Services** contain the business logic. `PermitService` loads the permit with EF Core, asks `PermitWorkflow` whether the action is allowed, updates the entity, writes a history row and saves.
4. **`PermitWorkflow`** is plain C# with no database, so the PTW rules are easy to unit test.
5. Business rule failures throw `BusinessRuleException`; **`GlobalExceptionHandler`** turns them into a 422 ProblemDetails response.

## Configuration

`backend/src/PtwSystem.Api/appsettings.json`

```json
"Database": { "Provider": "Sqlite" },          // or "SqlServer"
"ConnectionStrings": { "Sqlite": "...", "SqlServer": "..." },
"Jwt": { "Key": "...", "Issuer": "...", "Audience": "...", "ExpiryHours": 8 }
```

Never commit a real JWT key - use `dotnet user-secrets` locally or environment variables (`Jwt__Key`) in deployment.

## Possible next steps
- EF Core migrations instead of `EnsureCreated`
- Permit PDF print-out with QR code
- Email / Teams notifications on approval requests
- Attachments (JSA, isolation certificates, drawings)
- Refresh tokens and Azure AD sign-in
