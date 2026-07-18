# Research & Implementation Guidelines

**Feature**: Country WebAPI Service
**Date**: 2025-12-01

## Decisions

### 1. Framework & Versioning
- **Decision**: Use .NET 10.0 and ASP.NET Core.
- **Rationale**: Standard Maliev stack.
- **Versioning**: `Asp.Versioning.Http` for API versioning (`/countries/v1`).

### 2. Database & ORM
- **Decision**: PostgreSQL with Entity Framework Core 10.0.
- **Rationale**: Standard structured data storage.
- **Migrations**: Apply via `app.MigrateDatabaseAsync<T>()` in `Program.cs` (non-Testing env).

### 3. Caching Strategy
- **Decision**: Hybrid approach (In-Memory + Redis).
- **Rationale**: Spec requires <50ms latency. In-memory for speed, Redis for consistency across replicas.
- **Implementation**: `Microsoft.Extensions.Caching.StackExchangeRedis` via ServiceDefaults.

### 4. Validation Logic
- **Decision**: Data Annotations.
- **Rationale**: Maliev Constitution prohibits FluentValidation due to licensing.
- **Pattern**: `[Required]`, `[StringLength]`, etc. on DTOs.

### 5. Object Mapping
- **Decision**: Manual mapping via Extension Methods.
- **Rationale**: Maliev Constitution prohibits AutoMapper.
- **Pattern**: `public static UserResponse ToResponse(this User entity)` in `DomainToDtoMapper`.

### 6. Testing Strategy
- **Decision**: Real Infrastructure Testing with `Testcontainers`.
- **Rationale**: Maliev Constitution Principle IV.
- **Components**:
    - `Testcontainers.PostgreSql` for database.
    - `Testcontainers.Redis` for cache.
    - `WebApplicationFactory<Program>` for integration testing.
    - Dynamic RSA key generation for JWT testing.

### 7. Observability
- **Decision**: `Maliev.Aspire.ServiceDefaults`.
- **Rationale**: Mandatory for all new services. Handles OpenTelemetry, Logging, Metrics.
- **Metrics**: `prometheus-net.AspNetCore` for business metrics.

## Implementation Patterns

### Program.cs Composition
Must follow the strictly defined order:
1. `builder.AddServiceDefaults()` (FIRST)
2. Infrastructure (DB, Redis, Auth)
3. API/Controllers
4. App Services
5. `app.MapDefaultEndpoints()`

### Bulk Import
- **Pattern**: Asynchronous processing.
- **Flow**: Upload -> Store File -> Ack (202 "Accepted") -> Background Service processes -> Updates DB -> Invalidates Cache.
- **Concurrency**: Optimistic Concurrency Control (Version field) on individual records.

### API Documentation
- **Tool**: Scalar (`Scalar.AspNetCore`) + `Microsoft.AspNetCore.OpenApi`.
- **Path**: `/countries/scalar`.