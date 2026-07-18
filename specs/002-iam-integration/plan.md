# Implementation Plan: IAM Integration Migration

**Branch**: `002-iam-integration` | **Date**: 2025-12-21 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/002-iam-integration/spec.md`

## Summary
Migrate CountryService to a granular permission-based authorization model integrated with a central IAM service. This involves defining 14 permissions and 4 roles, registering them with IAM on startup, and protecting administrative/import endpoints using the `[RequirePermission]` attribute while maintaining anonymous access for public read operations.

## Technical Context

**Language/Version**: C# / .NET 10.0  
**Primary Dependencies**: `Maliev.Aspire.ServiceDefaults` (NuGet), `IHttpClientFactory`, `Microsoft.AspNetCore.Authentication.JwtBearer`  
**Storage**: PostgreSQL (AuditLogs)  
**Testing**: xUnit, Testcontainers (Redis/PostgreSQL)  
**Target Platform**: Docker / Linux
**Project Type**: Web API (ASP.NET Core)  
**Performance Goals**: Authorization check latency < 5ms  
**Constraints**: 
- Idempotent merge for IAM registration
- Degraded mode support if IAM is down at startup
- Mandatory audit logging for denied administrative requests
- Feature flag for migration bypass

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Note |
|-----------|--------|------|
| I. Service Autonomy | PASSED | Service defines and registers its own permissions. |
| II. Explicit Contracts | PASSED | API contracts for IAM registration defined in `contracts/`. |
| III. Test-First | PASSED | Integration tests planned for all new auth scenarios. |
| IV. Real Infrastructure | PASSED | Uses Testcontainers for integration tests. |
| V. Auditability | PASSED | FR-009 implemented via persistent AuditLogs. |
| IX. Clean Artifacts | PASSED | Proper directory structure in `specs/`. |
| X. Docker Best Practices | PASSED | Dockerfile in API folder. |
| XIII. .NET Aspire | PASSED | Consumes `ServiceDefaults`. |
| XIV. Library Standards | PASSED | No AutoMapper, FluentValidation, or FluentAssertions. |
| XV. Project Structure | PASSED | Flat structure followed. |

## Project Structure

### Documentation (this feature)

```text
specs/002-iam-integration/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           
│   └── iam-registration.yaml
└── checklists/          
    └── requirements.md
```

### Source Code (repository root)

```text
Maliev.CountryService.Api/
├── Authorization/
│   ├── CountryPermissions.cs
│   └── CountryPredefinedRoles.cs
├── Services/
│   └── CountryIAMRegistrationService.cs (IHostedService)
└── Controllers/
    ├── AdminCountriesController.cs (Update with [RequirePermission])
    └── BulkImportController.cs (Update with [RequirePermission])

Maliev.CountryService.Data/
├── Entities/
│   └── AuditLog.cs (Update CountryId to nullable)
└── CountryDbContext.cs (Update AuditLog configuration)

Maliev.CountryService.Tests/
└── Integration/
    └── AuthorizationTests.cs (New)
```

**Structure Decision**: Standard ASP.NET Core structure with specific `Authorization` folder for permission/role constants.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Project Reference for ServiceDefaults | Required for local development velocity. CI workflow (sed script) automatically switches this to a PackageReference during the build process to satisfy Constitution XIII in production. | Manual PackageReference management locally is slower for cross-service development. |