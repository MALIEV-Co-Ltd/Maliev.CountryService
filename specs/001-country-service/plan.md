# Implementation Plan: Country WebAPI Service

**Branch**: `001-country-service` | **Date**: 2025-10-31 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-country-service/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Build a high-performance, read-optimized Country WebAPI Service serving as the canonical source for country data across the Maliev microservices ecosystem. The service prioritizes minimal resource usage and sub-50ms read latencies through aggressive multi-layer caching (in-memory LRU + Redis), ETag-based conditional requests, and response compression. Administrative endpoints enable CRUD operations with optimistic concurrency control and comprehensive audit logging. Bulk import functionality supports asynchronous processing of large datasets with atomic validation. The service explicitly does NOT publish events to message bus; downstream consumers poll or request snapshots via the standard list endpoint.

## Technical Context

**Language/Version**: .NET 10.0 (ASP.NET Core WebAPI)
**Primary Dependencies**:
- Entity Framework Core 9.0.10 with Npgsql 9.0.4 (PostgreSQL provider)
- Serilog 8.0.2 (structured logging to stdout)
- FluentValidation 11.3.0 (request validation)
- StackExchange.Redis 9.0.0 (distributed cache)
- Prometheus.AspNetCore 8.2.1 (metrics)
- Scalar 1.2.42 with Microsoft.AspNetCore.OpenApi 9.0.0 (API documentation)
- Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8 (authentication)
- Polly 8.5.0 with Microsoft.Extensions.Http.Resilience 9.0.0 (resilience)
- Asp.Versioning.Http 8.1.0 (API versioning)

**Storage**: PostgreSQL 18 (database: country_service_app_db)
**Testing**: xUnit with FluentAssertions, real PostgreSQL (Docker containers for local/CI)
**Target Platform**: Kubernetes (GKE) with Docker containerization
**Project Type**: Web API microservice (3-project solution: Api, Data, Tests)
**Performance Goals**:
- p95 read latency <50ms for single country lookups
- p95 read latency <100ms for country list retrieval
- 99%+ cache hit rate under normal traffic
- Handle 10,000 concurrent read requests
- Operate on 2 replicas with small VM/container sizes

**Constraints**:
- Minimal resource footprint (<200MB memory per instance)
- Read operations represent 99%+ of traffic
- Small dataset (~250 active countries, <1MB total)
- No event publishing (downstream systems poll snapshots)
- HTTPS-only with JWT authentication for admin endpoints
- Anonymous access for read endpoints

**Scale/Scope**:
- 250 active country records
- 87 functional requirements
- 6 prioritized user stories
- 3 data entities (Country, AuditLog, BulkImportJob)
- 15 API endpoints (7 read, 8 admin/bulk operations)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Principle I: Service Autonomy ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: Service owns dedicated PostgreSQL database (country_service_app_db). No direct database access to other services. Interactions via standard HTTP APIs. Explicit requirement (FR-083) to NOT publish events - downstream systems poll snapshots via GET endpoint.

### Principle II: Explicit Contracts ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: All APIs documented via OpenAPI/Scalar (FR-065, Scalar UI at /countries/v1/scalar/v1). API versioning enforced (FR-080-082: base path /countries/v1, version headers, compatibility guarantees). Backward-compatible migrations ensured through manual migration application.

### Principle III: Test-First Development ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: Tests will be authored immediately after plan approval, before implementation. Project structure includes dedicated Maliev.CountryService.Tests project. Minimum 80% coverage target for business-critical logic (caching, validation, concurrency control). Contract tests for all API endpoints, integration tests for database operations, unit tests for validators and services.

### Principle IV: PostgreSQL-Only Testing ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: All tests MUST use PostgreSQL 18 database (no in-memory databases). Docker Compose configuration for local PostgreSQL test database. GitHub Actions workflows provision PostgreSQL 18 service container. TestDatabaseFixture with real PostgreSQL connection required. EF Core InMemoryDatabase provider explicitly prohibited.

### Principle V: Auditability & Observability ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: Structured JSON logging with Serilog to stdout (FR-070: log all admin operations with user identity, timestamp, resource). Immutable audit logs (FR-084-087: AuditLog entity with 24-month retention, before/after snapshots). Health checks (FR-063-064: liveness and readiness endpoints). Correlation ID middleware for request tracing.

### Principle VI: Security & Compliance ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: JWT authentication with RSA public key validation for admin endpoints (FR-056). Role-based authorization (CountryAdmin, SuperAdmin roles for FR-057-058). HTTPS-only enforcement (FR-054). Rate limiting (FR-061-062: 100 req/min for reads, 20 req/min for admin). Input sanitization (FR-059). No PII in country records (Assumption 9).

### Principle VII: Secrets Management & Configuration Security ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: All secrets injected from Google Secret Manager mounted at /mnt/secrets (connection strings, JWT public key). No secrets in source code. appsettings.Development.json uses localhost only. GitHub Actions workflows use mock service URLs (http://mock-*). README uses placeholders (<password>, <secret-key>).

### Principle VIII: Zero Warnings Policy ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: TreatWarningsAsErrors enabled in all .csproj files. CI/CD workflows fail on warnings. Build must produce zero warnings in Debug and Release configurations.

### Principle IX: Clean Project Artifacts ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: .gitignore excludes bin/, obj/, *.user, TestResults. .dockerignore excludes build outputs, IDE files, specs. Only project-specific files in source control. Unused boilerplate removed.

### Principle X: Simplicity & Maintainability ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: Clean Architecture (Controllers → Services → Data). Stateless design with all state in PostgreSQL. YAGNI applied (no unnecessary abstractions, manual mapping preferred over AutoMapper for simple DTOs). Shared patterns follow MALIEV-Co-Ltd microservices standards.

### Principle XI: Business Metrics & Analytics ✅ PASS
- **Status**: COMPLIANT
- **Evidence**: Prometheus metrics exposed at /metrics endpoint (FR-065-069). Business metrics: cache hit/miss rates, request latency percentiles, request volume by endpoint. Custom metrics for countries created/updated/deleted, active country count. Metrics tagged with service_name, version, environment. No PII exposure in metrics.

**GATE STATUS**: ✅ **ALL GATES PASS** - Proceed to Phase 0

---

### Post-Phase 1 Re-Evaluation (2025-11-01)

**Status**: ✅ **RE-VALIDATED** - All principles remain compliant after detailed design

After completing Phase 0 (research.md) and Phase 1 (data-model.md, contracts/, quickstart.md), all constitution principles have been re-evaluated against concrete implementation decisions:

- **Technology Stack Compliance**: All selected technologies (EF Core 9.0.10, Npgsql 9.0.4, StackExchange.Redis 9.0.0, Polly 8.5.0, etc.) align with Maliev microservices standards
- **Database Design Compliance**: Entity schemas, indexes, and migrations follow PostgreSQL-first approach with proper constraints and audit logging
- **API Design Compliance**: OpenAPI 3.0 specification provides complete contract documentation for all 15 endpoints with examples
- **Testing Strategy Compliance**: Quickstart guide confirms PostgreSQL-only testing with Docker Compose configuration
- **Security Design Compliance**: JWT authentication, role-based authorization, rate limiting, and secrets management patterns documented
- **Observability Compliance**: Prometheus metrics, structured logging, health checks, and audit logs fully specified
- **No New Risks Introduced**: Design decisions enhance constitutional compliance (e.g., optimistic concurrency via ETag, circuit breaker for Redis, atomic bulk import validation)

**Conclusion**: Design artifacts produced in Phase 0 and Phase 1 strengthen constitutional adherence. No violations or risks identified. Implementation may proceed.

---

## Project Structure

### Documentation (this feature)

```text
specs/001-country-service/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (technology decisions, patterns)
├── data-model.md        # Phase 1 output (entities, relationships, migrations)
├── quickstart.md        # Phase 1 output (local dev setup, commands)
├── contracts/           # Phase 1 output (OpenAPI specs, DTOs)
│   ├── openapi.yaml     # Full OpenAPI 3.0 specification
│   ├── dtos/            # Request/Response DTO definitions
│   └── examples/        # Sample requests/responses
├── checklists/          # Quality validation checklists
│   └── requirements.md  # Specification quality checklist (completed)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
Maliev.CountryService/
├── Maliev.CountryService.sln
│
├── Maliev.CountryService.Api/           # WebAPI project
│   ├── Controllers/
│   │   ├── CountriesController.cs       # Main CRUD + lookup endpoints
│   │   └── BulkImportController.cs      # Bulk import/status endpoints
│   ├── Models/
│   │   ├── Countries/
│   │   │   ├── CreateCountryRequest.cs
│   │   │   ├── UpdateCountryRequest.cs
│   │   │   ├── PatchCountryRequest.cs
│   │   │   └── CountryResponse.cs
│   │   ├── BulkImport/
│   │   │   ├── BulkImportRequest.cs
│   │   │   ├── BulkImportStatusResponse.cs
│   │   │   └── ValidationErrorResponse.cs
│   │   └── Common/
│   │       ├── PaginatedResponse.cs
│   │       └── ErrorResponse.cs
│   ├── Services/
│   │   ├── ICountryService.cs
│   │   ├── CountryService.cs            # Business logic for CRUD
│   │   ├── IBulkImportService.cs
│   │   ├── BulkImportService.cs         # Bulk import processing
│   │   ├── ICacheService.cs
│   │   ├── MemoryCacheService.cs        # In-memory LRU cache
│   │   └── RedisCacheService.cs         # Distributed Redis cache
│   ├── BackgroundServices/
│   │   ├── CacheWarmingService.cs       # Pre-load top 50 countries on startup
│   │   └── BulkImportWorkerService.cs   # Process bulk import jobs async
│   ├── Middleware/
│   │   ├── ExceptionHandlingMiddleware.cs
│   │   ├── CorrelationIdMiddleware.cs
│   │   └── SecurityHeadersMiddleware.cs
│   ├── Validators/
│   │   ├── CreateCountryRequestValidator.cs
│   │   ├── UpdateCountryRequestValidator.cs
│   │   ├── PatchCountryRequestValidator.cs
│   │   └── BulkImportRequestValidator.cs
│   ├── HealthChecks/
│   │   ├── DatabaseHealthCheck.cs
│   │   └── RedisHealthCheck.cs
│   ├── Metrics/
│   │   └── BusinessMetrics.cs           # Prometheus custom metrics
│   ├── Configuration/
│   │   └── Top50PopulousCountries.json  # Static list for cache warming
│   ├── Program.cs                       # Application entry point
│   ├── appsettings.json
│   ├── appsettings.Development.json     # Localhost configuration only
│   ├── Dockerfile                       # Multi-stage Docker build
│   └── Maliev.CountryService.Api.csproj
│
├── Maliev.CountryService.Data/          # Data layer project
│   ├── Models/
│   │   ├── Country.cs                   # Main entity with all fields
│   │   ├── AuditLog.cs                  # Audit trail entity
│   │   └── BulkImportJob.cs             # Bulk import job tracking
│   ├── Configurations/
│   │   ├── CountryConfiguration.cs      # FluentAPI for Country entity
│   │   ├── AuditLogConfiguration.cs
│   │   └── BulkImportJobConfiguration.cs
│   ├── Interceptors/
│   │   ├── AuditLogInterceptor.cs       # Auto-create audit logs on mutations
│   │   └── DatabaseMetricsInterceptor.cs # Prometheus DB query metrics
│   ├── CountryServiceDbContext.cs       # Main DbContext
│   ├── CountryServiceDbContextFactory.cs # Design-time factory for migrations
│   ├── Migrations/                      # EF Core migrations (auto-generated)
│   └── Maliev.CountryService.Data.csproj
│
├── Maliev.CountryService.Tests/         # Test project
│   ├── Fixtures/
│   │   ├── TestDatabaseFixture.cs       # PostgreSQL test database setup
│   │   └── TestWebApplicationFactory.cs # Integration test factory
│   ├── Unit/
│   │   ├── Validators/
│   │   │   ├── CreateCountryRequestValidatorTests.cs
│   │   │   ├── UpdateCountryRequestValidatorTests.cs
│   │   │   └── BulkImportRequestValidatorTests.cs
│   │   └── Services/
│   │       ├── CountryServiceTests.cs
│   │       ├── BulkImportServiceTests.cs
│   │       └── CacheServiceTests.cs
│   ├── Integration/
│   │   ├── CountriesControllerTests.cs  # Full endpoint tests with PostgreSQL
│   │   ├── BulkImportControllerTests.cs
│   │   ├── CachingBehaviorTests.cs      # Cache invalidation, warming, stale-while-revalidate
│   │   ├── ConcurrencyControlTests.cs   # Optimistic locking with version conflicts
│   │   └── HealthChecksTests.cs
│   ├── Contract/
│   │   └── OpenApiSchemaTests.cs        # Validate OpenAPI contract compliance
│   └── Maliev.CountryService.Tests.csproj
│
├── .github/
│   └── workflows/
│       ├── ci-develop.yml               # Dev environment CI/CD
│       ├── ci-staging.yml               # Staging environment CI/CD
│       └── ci-main.yml                  # Production environment CI/CD
│
├── .dockerignore                        # Docker build exclusions
├── .gitignore                           # Git exclusions
├── docker-compose.test.yml              # PostgreSQL for local testing
├── README.md                            # Comprehensive service documentation
└── LICENSE
```

**Structure Decision**: Three-project solution following Maliev microservices pattern (Api, Data, Tests). Api project contains all HTTP concerns (controllers, DTOs, middleware, validators), business services, and background workers. Data project isolates EF Core entities, configurations, interceptors, and migrations. Tests project uses real PostgreSQL with dedicated fixtures for unit, integration, and contract testing. This separation enables clean architecture, independent testing of data layer, and clear dependency management (Api → Data, Tests → Api → Data).

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

**No violations detected** - all constitution principles satisfied. Complexity tracking not required.

---

## Phase 0: Research & Technology Decisions

*Status: To be generated in research.md*

Research areas to cover:
1. **Cache warming strategy**: Implementation of static Top 50 populous countries list, startup pre-loading mechanism, performance impact analysis
2. **Optimistic concurrency with ETag**: Combining database version field with HTTP ETag header generation, If-Match header validation patterns, conflict resolution HTTP status codes
3. **Bulk import atomic validation**: Staging table design, all-or-nothing transaction patterns, duplicate detection across batch and database, error reporting with row numbers
4. **Stale-while-revalidate pattern**: Redis TTL configuration with grace period, background refresh triggers, X-Cache-Stale header generation, cache key invalidation strategies
5. **Circuit breaker for Redis**: Polly v8 circuit breaker configuration, failure threshold determination, fallback to in-memory cache, health status communication
6. **Rate limiting partitioning**: Per-user vs per-IP partitioning strategy, authenticated user identification from JWT claims, fallback to Host header, quota design for read vs admin endpoints

---

## Phase 1: Design Artifacts

*Status: To be generated in data-model.md, contracts/, quickstart.md*

### Data Model (data-model.md)
- Country entity with all fields, indexes (unique on iso2/iso3, text search on name), JSONB for timezones/metadata
- AuditLog entity with before/after snapshots, retention policies
- BulkImportJob entity with status machine, error tracking
- Migration strategy for initial schema, seed data for cache warming list

### API Contracts (contracts/)
- OpenAPI 3.0 specification with all 15 endpoints
- Request/Response DTOs with validation annotations
- HTTP status code mappings for all scenarios
- ETag/Last-Modified header specifications
- Pagination parameter schemas
- Error response formats

### Quickstart (quickstart.md)
- Prerequisites (Docker, .NET 9 SDK, kubectl)
- Local PostgreSQL setup with docker-compose.test.yml
- Environment variable configuration for development
- Database migration commands
- Test execution instructions
- Scalar UI access for API exploration

---

## Phase 2: Task Breakdown

*Status: To be generated by /speckit.tasks command (NOT by /speckit.plan)*

Task generation will produce actionable work items organized by:
1. Project setup and infrastructure
2. Data layer implementation (entities, configurations, migrations)
3. Core services implementation (CRUD, caching, validation)
4. API controllers and middleware
5. Background services (cache warming, bulk import worker)
6. Testing infrastructure and test suites
7. Docker and CI/CD pipeline configuration
8. Documentation and deployment artifacts

