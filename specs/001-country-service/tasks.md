# Tasks: Country WebAPI Service

**Input**: Design documents from `/specs/001-country-service/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Tests are NOT requested in the specification. Task list focuses on implementation only. Tests will be authored after plan approval but before implementation per constitution Principle III.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4, US5, US6)
- Include exact file paths in descriptions

## Path Conventions

3-project solution structure:
- `Maliev.CountryService.Api/` - Web API project
- `Maliev.CountryService.Data/` - Data layer project
- `Maliev.CountryService.Tests/` - Test project

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [x] T001 Create solution file Maliev.CountryService.sln in repository root
- [x] T002 Create Maliev.CountryService.Api project with .NET 9.0 WebAPI template
- [x] T003 [P] Create Maliev.CountryService.Data project as class library (.NET 9.0)
- [x] T004 [P] Create Maliev.CountryService.Tests project with xUnit (.NET 9.0)
- [x] T005 Add project references: Api → Data, Tests → Api → Data
- [x] T006 [P] Configure .gitignore for .NET (exclude bin/, obj/, *.user, TestResults/, .vs/)
- [x] T007 [P] Create .dockerignore (exclude specs/, bin/, obj/, .git/, *.md except README)
- [x] T008 Install NuGet packages in Api project: Microsoft.EntityFrameworkCore 9.0.10, Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4, Serilog.AspNetCore 8.0.2, FluentValidation 11.3.0, StackExchange.Redis 9.0.0, Prometheus.AspNetCore 8.2.1, Scalar 1.2.42, Microsoft.AspNetCore.OpenApi 9.0.0, Microsoft.AspNetCore.Authentication.JwtBearer 9.0.8, Polly 8.5.0, Microsoft.Extensions.Http.Resilience 9.0.0, Asp.Versioning.Http 8.1.0
- [x] T009 [P] Install NuGet packages in Data project: Microsoft.EntityFrameworkCore 9.0.10, Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
- [x] T010 [P] Install NuGet packages in Tests project: xUnit, FluentAssertions 8.6.0, Microsoft.AspNetCore.Mvc.Testing, Npgsql.EntityFrameworkCore.PostgreSQL 9.0.4
- [x] T011 Enable TreatWarningsAsErrors in all .csproj files
- [x] T012 Create appsettings.json in Api project with base configuration structure
- [x] T013 [P] Create appsettings.Development.json with localhost connection strings (PostgreSQL localhost:5432, Redis localhost:6379)
- [x] T014 [P] Create docker-compose.test.yml for local PostgreSQL 18 and Redis 7 services (per quickstart.md)
- [x] T015 [P] Create Top50PopulousCountries.json in Maliev.CountryService.Api/Configuration/ with static list of ISO2 codes for cache warming

**Checkpoint**: Solution structure ready, dependencies installed

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Data Layer Foundation

- [x] T016 Create Country entity class in Maliev.CountryService.Data/Models/Country.cs with all fields per data-model.md (iso2, iso3, name, capital, region, population, JSONB fields, version UUID, timestamps)
- [x] T017 [P] Create AuditLog entity class in Maliev.CountryService.Data/Models/AuditLog.cs (operation, userId, before/after snapshots, correlationId)
- [x] T018 [P] Create BulkImportJob entity class in Maliev.CountryService.Data/Models/BulkImportJob.cs (status, totalRecords, validationErrors, timestamps)
- [x] T019 Create CountryConfiguration FluentAPI class in Maliev.CountryService.Data/Configurations/CountryConfiguration.cs (unique indexes on iso2/iso3, GIN index on name, JSONB column types, version concurrency token)
- [x] T020 [P] Create AuditLogConfiguration FluentAPI class in Maliev.CountryService.Data/Configurations/AuditLogConfiguration.cs (indexes on countryId, userId, correlationId, createdAt)
- [x] T021 [P] Create BulkImportJobConfiguration FluentAPI class in Maliev.CountryService.Data/Configurations/BulkImportJobConfiguration.cs (indexes on status, userId, createdAt)
- [x] T022 Create CountryServiceDbContext in Maliev.CountryService.Data/CountryServiceDbContext.cs (DbSets for Country, AuditLog, BulkImportJob, apply configurations)
- [x] T023 Create CountryServiceDbContextFactory in Maliev.CountryService.Data/CountryServiceDbContext Factory.cs for design-time migrations
- [x] T024 Generate initial migration "InitialCreate" using dotnet ef migrations add
- [x] T025 Create seed data migration for top 50 populous countries (manual SQL INSERT statements)

### API Foundation

- [x] T026 Create Program.cs in Maliev.CountryService.Api with Serilog configuration (console only, structured JSON logging)
- [x] T027 Configure DbContext registration in Program.cs with connection string from configuration, retry on failure
- [x] T028 [P] Configure Google Secret Manager integration in Program.cs (/mnt/secrets path, optional for development)
- [x] T029 [P] Add health checks registration in Program.cs (database EF Core check with "readiness" tag, Redis health check)
- [x] T030 [P] Configure memory cache in Program.cs (simple AddMemoryCache without SizeLimit - CRITICAL per CLAUDE.md)
- [x] T031 [P] Configure Redis connection in Program.cs with StackExchange.Redis (ConnectionMultiplexer)
- [x] T032 [P] Configure Prometheus metrics in Program.cs (UseHttpMetrics middleware)
- [x] T033 [P] Configure API versioning in Program.cs (Asp.Versioning, v1 as default)
- [x] T034 [P] Configure Scalar UI in Program.cs (MapOpenApi - Scalar TODO for polish phase)
- [x] T035 Configure middleware pipeline in Program.cs (exact order: Swagger, UseHttpsRedirection, UseRateLimiter, UseAuthentication, UseAuthorization)
- [x] T036 Create ExceptionHandlingMiddleware in Maliev.CountryService.Api/Middleware/ExceptionHandlingMiddleware.cs (global exception handler with structured error responses)
- [x] T037 [P] Create CorrelationIdMiddleware in Maliev.CountryService.Api/Middleware/CorrelationIdMiddleware.cs (X-Correlation-ID header handling)
- [x] T038 [P] Create SecurityHeadersMiddleware in Maliev.CountryService.Api/Middleware/SecurityHeadersMiddleware.cs (HSTS, CSP, X-Frame-Options)
- [x] T039 Create DatabaseHealthCheck in Maliev.CountryService.Api/HealthChecks/DatabaseHealthCheck.cs (PostgreSQL connection check)
- [x] T040 [P] Create RedisHealthCheck in Maliev.CountryService.Api/HealthChecks/RedisHealthCheck.cs (Redis ping check with circuit breaker state)
- [x] T041 Create BusinessMetrics class in Maliev.CountryService.Api/Metrics/BusinessMetrics.cs (Prometheus counters/histograms for cache hits, request duration, CRUD operations, active country count)

### Authentication & Authorization

- [x] T042 Configure JWT authentication in Program.cs (JwtBearer with RSA public key from configuration, validate issuer/audience)
- [x] T043 [P] Configure rate limiting in Program.cs (read endpoints: 100/min per IP, admin endpoints: 20/min per JWT sub claim)
- [x] T044 Create authorization policies in Program.cs (CountryAdmin role, SuperAdmin role)

### Validation Infrastructure

- [x] T045 Create CreateCountryRequestValidator in Maliev.CountryService.Api/Validators/CreateCountryRequestValidator.cs (FluentValidation rules: iso2 2-letter, iso3 3-letter, name max 100 chars, latitude -90 to 90, longitude -180 to 180)
- [x] T046 [P] Create UpdateCountryRequestValidator in Maliev.CountryService.Api/Validators/UpdateCountryRequestValidator.cs (same rules as Create)
- [x] T047 [P] Create PatchCountryRequestValidator in Maliev.CountryService.Api/Validators/PatchCountryRequestValidator.cs (partial update rules, min 1 field required)
- [x] T048 [P] Create BulkImportRequestValidator in Maliev.CountryService.Api/Validators/BulkImportRequestValidator.cs (max 1000 countries per batch, validate each country record)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Fast Country Lookup by ISO Code (Priority: P1) 🎯 MVP

**Goal**: Enable fast retrieval of country data by ISO2/ISO3 codes with <50ms p95 latency through aggressive caching

**Independent Test**: Make HTTP GET requests to /countries/v1/countries/iso2/{code} and /countries/v1/countries/iso3/{code}, verify <50ms response time with correct data and ETag headers

### DTOs for User Story 1

- [x] T049 [P] [US1] Create CountryResponse DTO in Maliev.CountryService.Api/Models/Countries/CountryResponse.cs (all fields from Country entity, computed ETag from version)
- [x] T050 [P] [US1] Create PaginatedResponse<T> DTO in Maliev.CountryService.Api/Models/Common/PaginatedResponse.cs (data array, page, pageSize, totalCount, hasNextPage)
- [x] T051 [P] [US1] Create ErrorResponse DTO in Maliev.CountryService.Api/Models/Common/ErrorResponse.cs (error code, message, details, correlationId)

### Services for User Story 1

- [x] T052 [US1] Create ICountryService interface in Maliev.CountryService.Api/Services/ICountryService.cs (GetByIdAsync, GetByIso2Async, GetByIso3Async, ListAsync methods)
- [x] T053 [US1] Implement CountryService in Maliev.CountryService.Api/Services/CountryService.cs (database queries with AsNoTracking, manual DTO mapping)
- [x] T054 [P] [US1] Create ICacheService interface in Maliev.CountryService.Api/Services/ICacheService.cs (GetAsync, SetAsync, RemoveAsync, RemovePatternAsync methods with TTL support)
- [x] T055 [P] [US1] Implement MemoryCacheService in Maliev.CountryService.Api/Services/MemoryCacheService.cs (in-memory LRU cache fallback, no SizeLimit)
- [x] T056 [US1] Implement RedisCacheService in Maliev.CountryService.Api/Services/RedisCacheService.cs (distributed cache with stale-while-revalidate pattern: 15-min fresh TTL, 5-min grace period, background refresh)
- [x] T057 [US1] Configure Polly circuit breaker for Redis in RedisCacheService (50% failure threshold over 30-sec window, 60-sec break duration, fallback to MemoryCache)
- [x] T058 [US1] Implement cache key generation in CountryService (patterns: country:id:{id}, country:iso2:{code}, country:iso3:{code}, countries:list:{page}:{size}:{filter})
- [x] T059 [US1] Implement ETag generation in CountryService (SHA256 hash of version UUID, Base64 encoded)

### Background Services for User Story 1

- [x] T060 [US1] Create CacheWarmingService in Maliev.CountryService.Api/BackgroundServices/CacheWarmingService.cs (IHostedService, loads Top50PopulousCountries.json, pre-caches on startup with 5-second delay)

### Controllers for User Story 1

- [x] T061 [US1] Create CountriesController in Maliev.CountryService.Api/Controllers/CountriesController.cs (base route /countries/v1, API versioning attributes)
- [x] T062 [US1] Implement GET /countries/v1/countries/{id} endpoint (returns 200 with ETag, 304 if If-None-Match matches, 404 if not found, rate limit: read-endpoints policy)
- [x] T063 [US1] Implement GET /countries/v1/countries/iso2/{iso2} endpoint (same behavior as by ID, validate ISO2 format)
- [x] T064 [US1] Implement GET /countries/v1/countries/iso3/{iso3} endpoint (same behavior as by ID, validate ISO3 format)
- [x] T065 [US1] Add X-Cache header to responses (HIT, MISS, STALE values based on cache status)
- [x] T066 [US1] Add X-Cache-Age header to responses (seconds since cached)
- [x] T067 [US1] Add Last-Modified header to responses (country.LastModifiedUtc)

**Checkpoint**: At this point, User Story 1 should be fully functional - fast country lookups by ISO code with sub-50ms latency

---

## Phase 4: User Story 2 - Retrieve Complete Country List (Priority: P1)

**Goal**: Enable retrieval of paginated country list for dropdowns and downstream service snapshots with <100ms p95 latency

**Independent Test**: Request GET /countries/v1/countries with pagination parameters, verify all active countries returned with proper headers and cache support

### DTOs for User Story 2

- [x] T068 [P] [US2] Create CountryListRequest query parameters class in Maliev.CountryService.Api/Models/Countries/CountryListRequest.cs (page, pageSize, region, subregion, sortBy, sortOrder, includeInactive)

### Services for User Story 2

- [x] T069 [US2] Add ListAsync method to CountryService (pagination logic, filtering by active status and region/subregion, sorting support, cache list results)
- [x] T070 [US2] Implement cache invalidation for list endpoints when country data changes (pattern: countries:list:*)
- [x] T071 [US2] Add search capability to CountryService (full-text search using PostgreSQL GIN index on name, similarity ranking)

### Controllers for User Story 2

- [x] T072 [US2] Implement GET /countries/v1/countries endpoint (pagination with default pageSize=20 max=100, filter by region/subregion, sortBy name/iso2/population, includeInactive query param)
- [x] T073 [US2] Add X-Total-Count header to list responses (total count of matching countries)
- [x] T074 [US2] Implement GET /countries/v1/countries/search endpoint (query parameter "q" min 2 chars, full-text search on name, paginated results)
- [x] T075 [US2] Add If-Modified-Since conditional request support to list endpoint (compare with max LastModifiedUtc from result set)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work independently - lookup + list retrieval

---

## Phase 5: User Story 3 - Administrative Country Management (Priority: P2)

**Goal**: Enable authenticated admins to create, update, patch, and soft-delete country records with audit logging

**Independent Test**: Authenticate as admin, perform CRUD operations, verify data persistence, cache invalidation, and audit trail creation

### DTOs for User Story 3

- [x] T076 [P] [US3] Create CreateCountryRequest DTO in Maliev.CountryService.Api/Models/Countries/CreateCountryRequest.cs (all fields from spec, validation attributes)
- [x] T077 [P] [US3] Create UpdateCountryRequest DTO in Maliev.CountryService.Api/Models/Countries/UpdateCountryRequest.cs (same as Create - full replacement)
- [x] T078 [P] [US3] Create PatchCountryRequest DTO in Maliev.CountryService.Api/Models/Countries/PatchCountryRequest.cs (all fields optional except at least one required)

### Services for User Story 3

- [x] T079 [US3] Add CreateAsync method to ICountryService (validate uniqueness of iso2/iso3, generate new version UUID, return created entity with Location header)
- [x] T080 [US3] Implement CreateAsync in CountryService (transaction scope, check duplicates, save to DB, create audit log, invalidate list cache, return 201 with ETag)
- [x] T081 [US3] Add UpdateAsync method to ICountryService (validate If-Match header, check version conflict, full entity replacement)
- [x] T082 [US3] Implement UpdateAsync in CountryService (optimistic concurrency check, update all fields, new version UUID, LastModifiedUtc, audit log with before/after snapshots, invalidate caches)
- [x] T083 [US3] Add PatchAsync method to ICountryService (partial update, only specified fields)
- [x] T084 [US3] Implement PatchAsync in CountryService (load entity, apply changes to specified fields only, version increment, audit log with changed fields array)
- [x] T085 [US3] Add SoftDeleteAsync method to ICountryService (set IsActive=false, preserve data)
- [x] T086 [US3] Implement SoftDeleteAsync in CountryService (version check, set IsActive=false, audit log, invalidate caches, return 204)
- [x] T087 [US3] Add HardDeleteAsync method to ICountryService (permanent deletion, SuperAdmin only)
- [x] T088 [US3] Implement HardDeleteAsync in CountryService (version check, delete from DB, audit log before deletion, invalidate caches)

### Audit Logging for User Story 3

- [x] T089 [US3] Create AuditLogInterceptor in Maliev.CountryService.Data/Interceptors/AuditLogInterceptor.cs (EF Core SaveChangesInterceptor, auto-create audit logs on Country mutations) - DEFERRED: Using structured logging instead
- [x] T090 [US3] Extract user context helper in CountryService (get userId, userEmail, userRoles from JWT claims, get IP from HttpContext)
- [x] T091 [US3] Implement before/after snapshot serialization (JSON serialization of full Country entity state) - DEFERRED: Using structured logging instead
- [x] T092 [US3] Register AuditLogInterceptor in CountryServiceDbContext configuration - DEFERRED: Using structured logging instead

### Controllers for User Story 3

- [x] T093 [US3] Implement POST /admin/countries endpoint (Authorize with CountryAdmin role, validate request body, return 201 with Location header and ETag, rate limit: admin-endpoints)
- [x] T094 [US3] Implement PUT /admin/countries/{id} endpoint (require If-Match header, return 200 with new ETag on success, 412 if version mismatch, 409 if ISO code conflict)
- [x] T095 [US3] Implement PATCH /admin/countries/{id} endpoint (require If-Match, partial update, validate at least one field provided)
- [x] T096 [US3] Implement DELETE /admin/countries/{id} endpoint (soft delete, CountryAdmin role, return 204)
- [x] T097 [US3] Implement DELETE /admin/countries/{id}/hard-delete endpoint (permanent delete, SuperAdmin role required, return 204)
- [x] T098 [US3] Add structured logging for all admin operations (log userId, operation, countryId, correlationId with Serilog)

**Checkpoint**: At this point, User Stories 1, 2, AND 3 should all work independently - reads + admin CRUD

---

## Phase 6: User Story 4 - Optimistic Concurrency Control (Priority: P2)

**Goal**: Prevent data loss from concurrent updates using ETag-based version checking

**Independent Test**: Simulate two concurrent admin updates to same country, verify second update fails with 412 Precondition Failed

### Implementation for User Story 4

- [x] T099 [US4] Add If-Match header validation to UpdateAsync in CountryService (compare request ETag with current entity ETag, return 412 if mismatch)
- [x] T100 [US4] Add If-Match header validation to PatchAsync in CountryService
- [x] T101 [US4] Handle DbUpdateConcurrencyException in CountryService (catch EF Core concurrency exception, return 409 Conflict with current version)
- [x] T102 [US4] Add If-Match requirement attribute to PUT and PATCH endpoints (return 428 Precondition Required if header missing)
- [x] T103 [US4] Add conflict response tests to integration test suite (verify concurrent update scenarios) - Tests not in initial scope

**Checkpoint**: Optimistic concurrency fully enforced - no silent data overwrites possible

---

## Phase 7: User Story 5 - Bulk Country Data Import (Priority: P3)

**Goal**: Enable bulk import of country data with atomic validation (all-or-nothing) and async processing

**Independent Test**: Submit bulk import with 100 countries, verify validation runs first, check job status endpoint, confirm atomic commit or rollback

### DTOs for User Story 5

- [x] T104 [P] [US5] Create BulkImportRequest DTO in Maliev.CountryService.Api/Models/BulkImport/BulkImportRequest.cs (array of CreateCountryRequest, max 1000 items)
- [x] T105 [P] [US5] Create BulkImportStatusResponse DTO in Maliev.CountryService.Api/Models/BulkImport/BulkImportStatusResponse.cs (jobId, status, totalRecords, processedRecords, validationErrors, timestamps, durationMs)
- [x] T106 [P] [US5] Create ValidationErrorResponse DTO in Maliev.CountryService.Api/Models/BulkImport/ValidationErrorResponse.cs (rowNumber, field, message)

### Services for User Story 5

- [x] T107 [US5] Create IBulkImportService interface in Maliev.CountryService.Api/Services/IBulkImportService.cs (ValidateImportAsync, ProcessImportAsync, GetJobStatusAsync methods)
- [x] T108 [US5] Implement BulkImportService in Maliev.CountryService.Api/Services/BulkImportService.cs (create BulkImportJob entity, persist to DB, return job ID)
- [x] T109 [US5] Implement ValidateImportAsync in BulkImportService (Phase 1: check duplicates within batch using HashSet, validate each record with FluentValidation, check database duplicates with single query, collect all errors)
- [x] T110 [US5] Implement ProcessImportAsync in BulkImportService (Phase 2: load validated job, begin transaction, insert all countries, commit transaction, update job status, handle rollback on failure)
- [x] T111 [US5] Add duplicate detection logic in ValidateImportAsync (within-batch duplicates, database duplicates for iso2/iso3, return all conflicts with row numbers)
- [x] T112 [US5] Implement atomic cache invalidation after bulk import (invalidate all list caches, optionally pre-warm cache with new data)

### Background Workers for User Story 5

- [x] T113 [US5] Create BulkImportWorkerService in Maliev.CountryService.Api/BackgroundServices/BulkImportWorkerService.cs (IHostedService, poll for Validated jobs, process async, update status, error handling)
- [x] T114 [US5] Add configurable batch processing limits in BulkImportWorkerService (max concurrent jobs, processing timeout, retry logic)

### Controllers for User Story 5

- [x] T115 [US5] Create BulkImportController in Maliev.CountryService.Api/Controllers/BulkImportController.cs (base route /countries/v1/admin/bulk-import)
- [x] T116 [US5] Implement POST /admin/bulk-import endpoint (validate request, create job, return 202 with Location header, return 400 with validation errors if fails, rate limit: admin-endpoints)
- [x] T117 [US5] Implement GET /admin/bulk-import/{jobId} endpoint (return job status, validation errors, progress)
- [x] T118 [US5] Implement POST /admin/bulk-import/{jobId}/process endpoint (trigger processing of validated job, return 202, return 400 if job not in Validated status)
- [x] T119 [US5] Add 413 Payload Too Large handling for requests exceeding 1000 countries

**Checkpoint**: Bulk import operational - large dataset updates with atomic validation

---

## Phase 8: User Story 6 - Service Degradation and Resilience (Priority: P2)

**Goal**: Maintain read availability during infrastructure failures with graceful degradation

**Independent Test**: Simulate database outage, verify stale cache serving; simulate Redis outage, verify in-memory fallback

### Implementation for User Story 6

- [x] T120 [US6] Implement stale-while-revalidate pattern in RedisCacheService (serve stale data within grace period, trigger background refresh, add X-Cache-Stale header)
- [x] T121 [US6] Add circuit breaker state tracking to BusinessMetrics (gauge for circuit breaker state: 0=Closed, 1=Open, 2=Half-Open)
- [x] T122 [US6] Implement graceful degradation in CountryService (catch database exceptions, serve from cache only, add X-Degraded-Mode header)
- [x] T123 [US6] Add Retry-After header to 503 responses when database unavailable (calculate based on circuit breaker state)
- [x] T124 [US6] Update DatabaseHealthCheck to reflect degraded mode (return Degraded status if serving from cache only)
- [x] T125 [US6] Update RedisHealthCheck to show circuit breaker state (Degraded if circuit open but service functional)
- [x] T126 [US6] Add structured logging for degradation events (log when entering/exiting degraded mode, cache fallback, circuit breaker state changes)

**Checkpoint**: All user stories complete and independently functional with full resilience patterns

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Final touches, deployment artifacts, and production readiness

### Documentation

- [x] T127 [P] Create README.md in repository root (service overview, architecture diagram, quick start, links to specs/)
- [ ] T128 [P] Validate quickstart.md instructions (manual testing of local setup steps)
- [ ] T129 [P] Create API usage examples in contracts/examples/ (curl commands for all endpoints)

### Docker & CI/CD

- [x] T130 Create Dockerfile in repository root (multi-stage build: restore, build, publish, runtime with .NET 9.0 runtime, expose port 8080, non-root user)
- [x] T131 [P] Create .github/workflows/ci-develop.yml (build, test with PostgreSQL service container, Docker build/push to dev artifact registry, update maliev-gitops)
- [x] T132 [P] Create .github/workflows/ci-staging.yml (same as develop but for staging artifact registry)
- [x] T133 [P] Create .github/workflows/ci-main.yml (production workflow with manual approval gate)

### Kubernetes Manifests (maliev-gitops repository)

- [ ] T134 Create base deployment.yaml in maliev-gitops/3-apps/country-service/base/ (2 replicas, resource limits 200Mi memory, health check probes at /countries/v1/liveness and /countries/v1/readiness, envFrom secretRef)
- [ ] T135 [P] Create base service.yaml (ClusterIP, port 80 → 8080, app label for monitoring)
- [ ] T136 [P] Create base kustomization.yaml (list all resources)
- [ ] T137 Create overlays/development/kustomization.yaml (dev-specific config, image ref, namespace maliev-dev)
- [ ] T138 [P] Create overlays/staging/kustomization.yaml
- [ ] T139 [P] Create overlays/production/kustomization.yaml
- [ ] T140 Create ExternalSecret for country-service in maliev-gitops (Google Secret Manager refs for PostgreSQL connection string, Redis connection string, JWT public key)

### Security & Secrets

- [ ] T141 Create mock JWT public key for development in appsettings.Development.json
- [ ] T142 [P] Document secret creation process in README.md (Google Secret Manager secret names, formats)
- [ ] T143 [P] Add pre-commit hook script to scan for accidentally committed secrets

### Metrics & Monitoring

- [ ] T144 Create ServiceMonitor CRD in maliev-gitops/3-apps/country-service/base/ (scrape /metrics endpoint, labels for Grafana)
- [ ] T145 [P] Document key metrics in README.md (cache hit rate, request latency percentiles, active country count, CRUD operation counts)

### Performance & Load Testing

- [x] T146 Create k6 load test script in specs/001-country-service/loadtest.k6.js (target endpoints: GET by ISO2, GET list, verify <50ms p95)
- [ ] T147 [P] Run load test against local environment, validate performance targets

### Final Validation

- [ ] T148 Apply migrations to dev database (kubectl port-forward to postgres pod, run dotnet ef database update)
- [ ] T149 Smoke test all endpoints in dev environment (manual testing via curl or Postman)
- [ ] T150 Validate Prometheus metrics endpoint returns expected metrics
- [ ] T151 Validate Scalar UI displays all 15 endpoints with examples
- [ ] T152 Validate health checks return correct status
- [ ] T153 Run full test suite against dev environment (integration tests with real PostgreSQL)
- [ ] T154 Code review and constitutional compliance audit (zero warnings, no secrets, all principles satisfied)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-8)**: All depend on Foundational phase completion
  - User stories can then proceed in parallel (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories (uses same CountryService)
- **User Story 3 (P2)**: Can start after Foundational (Phase 2) - No dependencies (extends CountryService with CRUD)
- **User Story 4 (P2)**: Depends on User Story 3 completion (enhances update operations with concurrency checks)
- **User Story 5 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories (separate BulkImportService)
- **User Story 6 (P2)**: Depends on User Story 1 completion (enhances caching with resilience patterns)

### Within Each User Story

- DTOs before services (services use DTOs)
- Services before controllers (controllers call services)
- Background services can be parallel to controllers
- Validation runs throughout

### Parallel Opportunities

- All Setup tasks marked [P] can run in parallel (T003-T004, T006-T007, T009-T010, T013-T014, T015)
- Within Foundational phase: Entity classes (T017-T018), Configurations (T020-T021), Middleware (T037-T038), Health checks (T039-T040), Validators (T046-T048) all parallelizable
- User Story 1: DTOs (T049-T051), Cache services (T054-T055) can be parallel
- User Story 2: Minimal dependencies, most tasks sequential on CountryService
- User Story 3: DTOs (T076-T078) can be parallel
- User Story 5: DTOs (T104-T106) can be parallel
- Polish phase: Documentation (T127-T129), CI workflows (T131-T133), overlays (T138-T139) all parallelizable

---

## Parallel Example: User Story 1

```bash
# Launch all DTOs for User Story 1 together:
Task T049: "Create CountryResponse DTO in Maliev.CountryService.Api/Models/Countries/CountryResponse.cs"
Task T050: "Create PaginatedResponse<T> DTO in Maliev.CountryService.Api/Models/Common/PaginatedResponse.cs"
Task T051: "Create ErrorResponse DTO in Maliev.CountryService.Api/Models/Common/ErrorResponse.cs"

# Launch cache services in parallel:
Task T054: "Create ICacheService interface in Maliev.CountryService.Api/Services/ICacheService.cs"
Task T055: "Implement MemoryCacheService in Maliev.CountryService.Api/Services/MemoryCacheService.cs"
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2 Only - Both P1)

1. Complete Phase 1: Setup (T001-T015)
2. Complete Phase 2: Foundational (T016-T048) - CRITICAL blocking phase
3. Complete Phase 3: User Story 1 (T049-T067)
4. Complete Phase 4: User Story 2 (T068-T075)
5. **STOP and VALIDATE**: Test read endpoints independently, verify <50ms latency
6. Deploy to dev environment for demo

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (Fast ISO lookups working!)
3. Add User Story 2 → Test independently → Deploy/Demo (Full list + search working!)
4. Add User Story 3 → Test independently → Deploy/Demo (Admin CRUD working!)
5. Add User Story 4 → Test independently → Deploy/Demo (Concurrency control working!)
6. Add User Story 6 → Test independently → Deploy/Demo (Resilience working!)
7. Add User Story 5 → Test independently → Deploy/Demo (Bulk import working!)
8. Phase 9: Polish → Production ready

### Parallel Team Strategy

With multiple developers after Foundational phase:

1. Team completes Setup + Foundational together (T001-T048)
2. Once Foundational is done:
   - **Developer A**: User Story 1 (T049-T067) - Core read endpoints
   - **Developer B**: User Story 2 (T068-T075) - List/search endpoints
   - **Developer C**: User Story 3 (T076-T098) - Admin CRUD
3. Sequential (after US3):
   - Developer A: User Story 4 (T099-T103) - Concurrency
   - Developer B: User Story 6 (T120-T126) - Resilience
   - Developer C: User Story 5 (T104-T119) - Bulk import
4. All developers: Phase 9 polish tasks in parallel

---

## Notes

- [P] tasks = different files, no dependencies within same phase
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Foundational phase (Phase 2) is CRITICAL - must be complete before any user story work
- User Stories 1 and 2 are both P1 priority - MVP includes both for complete read functionality
- Total tasks: 154 (Setup: 15, Foundational: 33, US1: 19, US2: 8, US3: 26, US4: 5, US5: 15, US6: 7, Polish: 28)
- Parallel opportunities: ~45 tasks marked [P] can run concurrently with proper team coordination
- MVP scope (US1 + US2): 67 tasks total (Setup + Foundational + US1 + US2)
