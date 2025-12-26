# Tasks: IAM Integration Migration

**Feature Name**: IAM Integration Migration
**Plan**: [plan.md](./plan.md)
**Branch**: `002-iam-integration`

## Implementation Strategy

We will follow a **Test-First** incremental delivery approach. We begin with foundational constants and the registration infrastructure. For each user story, we will define integration tests *before* implementing the enforcement logic. This ensures that every permission check is verified against real infrastructure via Testcontainers.

## Phase 1: Setup & Constants

- [X] T001 [P] Define permission constants in `Maliev.CountryService.Api/Authorization/CountryPermissions.cs`
- [X] T002 [P] Define predefined roles and their permission mappings in `Maliev.CountryService.Api/Authorization/CountryPredefinedRoles.cs`

## Phase 2: Foundational Infrastructure

- [X] T003 Update `AuditLog` entity to make `CountryId` nullable in `Maliev.CountryService.Data/Entities/AuditLog.cs`
- [X] T004 Update `AuditLog` configuration in `Maliev.CountryService.Data/CountryDbContext.cs` to handle nullable `CountryId`
- [X] T005 [P] Create and apply Entity Framework migration for the `AuditLog` change
- [X] T006 Implement `CountryIAMRegistrationService` for idempotent permission/role registration in `Maliev.CountryService.Api/Services/CountryIAMRegistrationService.cs`
- [X] T007 Register `IHttpClientFactory` for IAM and the registration background service in `Maliev.CountryService.Api/Program.cs`

## Phase 3: User Story 1 - Secure Administrative Data Management (Priority: P1)

**Goal**: Protect country modification endpoints with granular permissions and audit denials.
**Independent Test**: Use `AuthorizationTests.cs` to verify 403 Forbidden for unauthorized users and 200 OK for authorized users.

- [X] T008 [US1] Create integration tests for authorized and unauthorized administrative operations in `Maliev.CountryService.Tests/Integration/AuthorizationTests.cs`
- [X] T009 [P] [US1] Apply `[RequirePermission]` attributes to all modification endpoints in `Maliev.CountryService.Api/Controllers/AdminCountriesController.cs`
- [X] T010 [US1] Implement soft vs hard delete logic branching based on permission in `Maliev.CountryService.Api/Services/CountryService.cs` (FR-004)
- [X] T011 [US1] Implement audit logging for denied modification attempts in `AdminCountriesController.cs`

## Phase 4: User Story 2 - Controlled Bulk Import Operations (Priority: P2)

**Goal**: Restrict bulk import lifecycle operations to authorized importers.
**Independent Test**: Verify that anonymous or viewer users cannot trigger or cancel imports using integration tests.

- [X] T012 [US2] Add integration tests for bulk import permission enforcement in `Maliev.CountryService.Tests/Integration/AuthorizationTests.cs`
- [X] T013 [P] [US2] Apply `[RequirePermission]` attributes to all endpoints in `Maliev.CountryService.Api/Controllers/BulkImportController.cs`
- [X] T014 [US2] Implement audit logging for denied bulk import attempts in `BulkImportController.cs`

## Phase 5: User Story 3 - Unhindered Public Access (Priority: P3)

**Goal**: Ensure public endpoints remain accessible without authentication.
**Independent Test**: Verify `GET /api/v1/countries` returns 200 OK without a Bearer token.

- [X] T016 [US3] Add integration tests verifying unauthenticated access to all public endpoints in `Maliev.CountryService.Tests/Integration/AuthorizationTests.cs`
- [X] T017 [US3] Verify `[AllowAnonymous]` usage on read-only endpoints in `Maliev.CountryService.Api/Controllers/CountriesController.cs`

## Phase 6: User Story 4 - Restricted System Maintenance (Priority: P4)

**Goal**: Protect system maintenance operations like cache rebuilding and statistics.
**Independent Test**: Verify only users with `country.system.*` permissions can rebuild the cache.

- [X] T018 [US4] Add integration tests for system maintenance permission enforcement in `Maliev.CountryService.Tests/Integration/AuthorizationTests.cs`
- [X] T019 [P] [US4] Apply `[RequirePermission]` attributes to system operations (RebuildCache, Export) in `Maliev.CountryService.Api/Controllers/AdminCountriesController.cs`

## Phase 7: Polish & Monitoring

- [X] T020 [P] Add a benchmark/integration test to verify authorization check latency is < 5ms (SC-005)
- [X] T021 Finalize configuration schema and default values in `Maliev.CountryService.Api/appsettings.json`
- [X] T022 Verify zero compiler warnings and ensure all integration tests pass in the `002-iam-integration` branch

## Dependencies

- Foundational (Phase 2) MUST be completed before User Stories (Phase 3, 4, 6)
- Tests in each Phase MUST be created/run before Implementation tasks per Constitution III
- US1, US2, US4 are independent of each other but depend on Phase 2
- US3 is independent of other user stories

## Parallel Execution Examples

- **Foundational Parallel**: T001 and T002 can be implemented simultaneously.
- **Controller Parallel**: T009, T013, and T017 can be implemented simultaneously once their respective tests (T008, T012, T018) are ready.