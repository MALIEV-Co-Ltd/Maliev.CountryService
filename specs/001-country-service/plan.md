# Implementation Plan: Country WebAPI Service

**Branch**: `001-country-service` | **Date**: 2025-12-01 | **Spec**: [specs/001-country-service/spec.md](spec.md)
**Input**: Feature specification from `specs/001-country-service/spec.md` and Maliev Service Implementation Guidelines.

## Summary

Implementation of a high-performance, read-optimized Country WebAPI Service serving as the canonical source for country data. Key features include fast lookups by ISO codes, administrative CRUD with optimistic concurrency, and bulk import capabilities.

## Technical Context

**Language/Version**: .NET 10.0 (C#)
**Framework**: ASP.NET Core Web API using `Microsoft.NET.Sdk.Web`
**Primary Dependencies**:
- `Maliev.Aspire.ServiceDefaults` (NuGet) for observability/resilience
- `Npgsql.EntityFrameworkCore.PostgreSQL` for database
- `StackExchange.Redis` for distributed caching
- `MassTransit.RabbitMQ` for messaging (if needed for audit/events)
- `AspNetCore.HealthChecks` ecosystem
- `Scalar.AspNetCore` & `Microsoft.AspNetCore.OpenApi` for documentation
**Storage**: PostgreSQL 16+ (via Entity Framework Core)
**Caching**: Redis (StackExchange.Redis) + In-Memory (IMemoryCache)
**Testing**: xUnit with `Testcontainers` (PostgreSQL, Redis) for real infrastructure testing.
**Target Platform**: Linux (Docker), deploying to Kubernetes.
**Project Type**: Web API Microservice
**Performance Goals**: p95 < 50ms for single lookups, < 100ms for list lookups.
**Constraints**: Strict adherence to Maliev Constitution (ServiceDefaults, No FluentValidation, No AutoMapper, No Serilog).

## Constitution Check

*GATE: Passed.*

- **Service Autonomy**: Own database (CountryServiceDbContext), no direct DB access.
- **Explicit Contracts**: OpenAPI/Scalar used.
- **Test-First**: Plan includes xUnit + Testcontainers setup.
- **Real Infrastructure Testing**: Mandated Testcontainers for PG and Redis.
- **Auditability**: Structured logging (System.Text.Json), AuditLog entity.
- **Security**: JWT Auth, Google Secret Manager.
- **Aspire Integration**: `Maliev.Aspire.ServiceDefaults` usage confirmed.
- **Docker**: Best practices (app user, multi-stage) enforced.

## Project Structure

### Documentation (this feature)

```text
specs/001-country-service/
├── plan.md              # This file
├── research.md          # Technical decisions & guidelines
├── data-model.md        # Entity & Schema definitions
├── quickstart.md        # Run & Test instructions
├── contracts/           # OpenAPI specs
└── tasks.md             # Implementation tasks
```

### Source Code (repository root)

```text
Maliev.CountryService.sln
├── Maliev.CountryService.Api/       # Main Web API Project
│   ├── Controllers/
│   ├── Models/                      # DTOs
│   ├── Services/                    # Business Logic
│   ├── Data/                        # EF Core DbContext (moved to separate lib if shared, keeping simpler here per guidelines usually suggests separation but prompt structure implies standard)
│   ├── Program.cs                   # App composition
│   └── Dockerfile
├── Maliev.CountryService.Data/      # Data Layer (Entities, DbContext, Migrations)
│   ├── Entities/
│   └── CountryServiceDbContext.cs
├── Maliev.CountryService.Tests/     # xUnit Tests
│   ├── Integration/                 # Testcontainers tests
│   └── Unit/
└── docker-compose.test.yml          # For local infra (optional with Testcontainers)
```
*Note: Structure adapted from standard Maliev patterns and prompt guidelines.*

## Implementation Guidelines

(Detailed in `research.md`)
- **Validation**: Data Annotations only.
- **Mapping**: Extension methods only.
- **Logging**: Standard `ILogger` / OpenTelemetry.
- **Testing**: Testcontainers for everything.