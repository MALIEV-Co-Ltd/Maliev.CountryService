# Implementation Tasks: Country WebAPI Service

**Spec**: [specs/001-country-service/spec.md](specs/001-country-service/spec.md)
**Plan**: [specs/001-country-service/plan.md](specs/001-country-service/plan.md)
**Status**: Draft

## Dependencies

1. **Phase 1: Setup** (Project & Infra)
2. **Phase 2: Foundation** (DB, ServiceDefaults, Base Tests)
3. **Phase 3: User Story 1** (Fast Lookup)
4. **Phase 4: User Story 2** (List & Pagination)
5. **Phase 5: User Story 3** (Admin Management & Audit)
6. **Phase 6: User Story 4** (Optimistic Concurrency)
7. **Phase 7: User Story 6** (Resilience - *Implemented out of order due to logical affinity with Read ops*)
8. **Phase 8: User Story 5** (Bulk Import)
9. **Phase 9: Polish**

## Implementation Strategy

- **Strict Test-First**: All features start with an integration test using Testcontainers.
- **Incremental**: We build the read path first (US1/US2) to satisfy the primary high-traffic use case, then the write path (US3/US4), then advanced features (US5/US6).
- **Resilience**: Caching and fallback logic are integrated early in the Service layer.
- **Compliance**: STRICT adherence to Maliev Constitution (No FluentValidation, No AutoMapper, ServiceDefaults First).

---

## Phase 1: Project Setup & Infrastructure

**Goal**: Initialize the solution, projects, and infrastructure configuration strictly adhering to Maliev guidelines.

- [x] T001 Initialize solution `Maliev.CountryService.sln` and projects (`Api`, `Data`, `Tests`)
- [x] T002 [P] Configure `Maliev.CountryService.Api/Maliev.CountryService.Api.csproj` (Add `Maliev.Aspire.ServiceDefaults`, `Npgsql`, `Redis`, `MassTransit`, set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- [x] T003 [P] Configure `Maliev.CountryService.Data/Maliev.CountryService.Data.csproj` (Add EF Core, Npgsql, set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- [x] T004 [P] Configure `Maliev.CountryService.Tests/Maliev.CountryService.Tests.csproj` (Add `Testcontainers`, `xUnit`, `Microsoft.AspNetCore.Mvc.Testing`, set `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`)
- [x] T005 Setup `Maliev.CountryService.Api/Program.cs` with strictly ordered `AddServiceDefaults()` and middleware pipeline
- [x] T006 [P] Create `Maliev.CountryService.Api/Dockerfile` using multi-stage build and `app` user
- [x] T007 [P] Create `docker-compose.test.yml` for local development support
- [x] T008 [P] Create `Maliev.CountryService.Api/appsettings.json` and `appsettings.Development.json` with connection strings placeholders
- [x] T009 [P] Setup `nuget.config` in solution root for GitHub Packages authentication

## Phase 2: Foundation (Data & Tests)

**Goal**: Establish the database context, migrations, and the reusable integration test fixture.

- [x] T010 Define `Country` entity skeleton in `Maliev.CountryService.Data/Entities/Country.cs`
- [x] T011 Setup `Maliev.CountryService.Data/CountryServiceDbContext.cs` with entity configuration
- [x] T012 Create initial migration in `Maliev.CountryService.Data/Migrations`
- [x] T013 Implement `TestDatabaseFixture` with Testcontainers (Postgres + Redis) in `Maliev.CountryService.Tests/Fixtures/TestDatabaseFixture.cs`
- [x] T014 Implement `TestWebApplicationFactory` with dynamic RSA key generation in `Maliev.CountryService.Tests/Fixtures/TestWebApplicationFactory.cs`
- [x] T015 Create base integration test class `Maliev.CountryService.Tests/Integration/IntegrationTestBase.cs`
- [x] T016 Verify foundation with a simple health check test in `Maliev.CountryService.Tests/Integration/HealthCheckTests.cs`
- [x] T017 [P] Configure Response Compression (Brotli/Gzip) in `Maliev.CountryService.Api/Program.cs`
- [x] T018 [P] Configure Rate Limiting policies (Public vs Admin) in `Maliev.CountryService.Api/Program.cs`

## Phase 3: User Story 1 - Fast Country Lookup (P1)

**Goal**: Enable retrieval of country data by ID and ISO codes with caching.
**Test Criteria**: Sub-50ms response for cached items, correct 404s, ETag headers present.

- [x] T019 [US1] Create integration test `Maliev.CountryService.Tests/Integration/CountryLookupTests.cs` (Test GetById, GetByIso2, GetByIso3)
- [x] T020 [P] [US1] Complete `Country` entity definition in `Maliev.CountryService.Data/Entities/Country.cs` (Add ISO codes, Name, Region, etc.)
- [x] T021 [US1] Create `Maliev.CountryService.Data/Migrations` update for full country schema
- [x] T022 [P] [US1] Implement `CountryResponse` DTO and `DomainToDtoMapper` in `Maliev.CountryService.Api/Models/CountryResponse.cs`
- [x] T023 [US1] Implement `ICountryService` and `CountryService` (Read methods) in `Maliev.CountryService.Api/Services/CountryService.cs`
- [x] T024 [US1] Implement Redis caching logic in `CountryService` (Cache-Aside pattern)
- [x] T025 [US1] Implement `CacheWarmingService` (IHostedService) to preload top 50 countries
- [x] T026 [US1] Implement `CountriesController` with lookup endpoints in `Maliev.CountryService.Api/Controllers/CountriesController.cs`
- [x] T027 [US1] Register services and interfaces in `Maliev.CountryService.Api/Program.cs`

## Phase 4: User Story 2 - Country List (P1)

**Goal**: Retrieve paginated list of countries with filtering.
**Test Criteria**: Pagination works, filters apply correctly, performance < 100ms.

- [x] T028 [US2] Add list/search tests to `Maliev.CountryService.Tests/Integration/CountryListTests.cs`
- [x] T029 [P] [US2] Implement `PagedResult<T>` helper in `Maliev.CountryService.Api/Models/PagedResult.cs`
- [x] T030 [US2] Configure Global Query Filter for `IsActive` in `CountryServiceDbContext`
- [x] T031 [US2] Add `GetListAsync` method to `CountryService` (with caching for first page/common filters)
- [x] T032 [US2] Add List endpoint to `CountriesController` with `[FromQuery]` parameters
- [x] T033 [US2] Implement GIN index on Name field in `Maliev.CountryService.Data` for search performance

## Phase 5: User Story 3 - Admin Management (P2)

**Goal**: Create, Update, and Soft-Delete countries with Authentication.
**Test Criteria**: Auth required, Audit logs created, Data persisted.

- [x] T034 [US3] Add CRUD tests to `Maliev.CountryService.Tests/Integration/AdminCountryTests.cs` (Auth simulation required)
- [x] T035 [P] [US3] Define `AuditLog` entity in `Maliev.CountryService.Data/Entities/AuditLog.cs`
- [x] T036 [US3] Create migration for `AuditLog` table
- [x] T037 [P] [US3] Implement `CreateCountryRequest`, `UpdateCountryRequest` DTOs with Data Annotations
- [x] T038 [US3] Implement `CreateAsync`, `UpdateAsync`, `SoftDeleteAsync` in `CountryService`
- [x] T039 [US3] Implement Audit Logging logic (ensure comprehensive history) in `CountryService`
- [x] T040 [US3] Create `AdminCountriesController` in `Maliev.CountryService.Api/Controllers/AdminCountriesController.cs` with `[Authorize]`

## Phase 6: User Story 4 - Optimistic Concurrency (P2)

**Goal**: Prevent lost updates using Version/ETag.
**Test Criteria**: Concurrent updates return 412 Precondition Failed.

- [x] T041 [US4] Add concurrency tests to `Maliev.CountryService.Tests/Integration/ConcurrencyTests.cs`
- [x] T042 [US4] Update `Country` entity to include `Version` using `[ConcurrencyCheck]` and/or `uint xmin` mapping for Postgres
- [x] T043 [US4] Implement logic to check `If-Match` header in `AdminCountriesController` and validate against current Version in `CountryService`
- [x] T044 [US4] Ensure `ETag` header is returned in all Get/Update responses

## Phase 7: User Story 6 - Resilience (P2)

**Goal**: Serve from cache when DB is down.
**Test Criteria**: Stop PG container, verify Read ops still work (stale).

- [x] T045 [US6] Add resilience test `Maliev.CountryService.Tests/Integration/ResilienceTests.cs` (Simulate DB failure)
- [x] T046 [US5] Implement "Safe Get" pattern in `CountryService`
- [x] T047 [US6] Add `X-Served-From-Cache` and `X-Cache-Stale` headers in `CountryService` response metadata

## Phase 8: User Story 5 - Bulk Import (P3)

**Goal**: Async bulk import of country data.
**Test Criteria**: File upload accepted, Job created, Background processing updates DB.

- [x] T048 [US5] Add bulk import tests to `Maliev.CountryService.Tests/Integration/BulkImportTests.cs`
- [x] T049 [P] [US5] Define `BulkImportJob` entity in `Maliev.CountryService.Data/Entities/BulkImportJob.cs`
- [x] T050 [US5] Create migration for `BulkImportJob`
- [x] T051 [US5] Implement `BulkImportService` structure and background queue in `Maliev.CountryService.Api/BackgroundServices/BulkImportService.cs`
- [x] T052 [US5] Implement `BulkImportRequestValidator` with "All-or-Nothing" logic
- [x] T053 [US5] Implement CSV/JSON parsing logic in `BulkImportService`
- [x] T054 [US5] Implement atomic persistence logic (transactional update) in `BulkImportService`
- [x] T055 [US5] Implement `BulkImportController` endpoints (Submit, Status)

## Phase 9: Polish & Documentation

**Goal**: Finalize documentation and code quality.

- [x] T056 [P] Verify OpenAPI generation and Scalar UI at `/countries/scalar`
- [x] T057 [P] Add XML comments to all public Controllers and Models
- [x] T058 Implement Business Metrics (country_lookups_total, import_duration) via `prometheus-net`
- [x] T059 Verify zero warnings in build output (explicit check)
- [x] T060 Run full test suite and ensure 0 warnings in build
- [x] T061 Update `README.md` with service specific run instructions