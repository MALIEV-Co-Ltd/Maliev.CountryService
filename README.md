# Maliev Country Service

A high-performance, production-ready RESTful API service for managing country reference data with advanced caching, resilience patterns, and GitOps deployment.

## Overview

The Country Service provides comprehensive country information management with:
- **Sub-50ms p95 read latency** through Redis distributed caching with in-memory fallback
- **Graceful degradation** maintaining read availability during database outages
- **Optimistic concurrency control** preventing data conflicts in concurrent updates
- **Bulk import capabilities** for efficient dataset updates (up to 1,000 records per batch)
- **GitOps deployment** with Kubernetes, ArgoCD, and Kustomize

## Architecture

```
┌─────────────────┐
│   API Gateway   │
│   (maliev.com)  │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌──────────────┐
│ Country Service │◄────►│ Redis Cache  │
│   (2 replicas)  │      │ (Distributed)│
└────────┬────────┘      └──────────────┘
         │
         ▼
┌─────────────────┐
│  PostgreSQL 18  │
│  (Primary DB)   │
└─────────────────┘

Monitoring: Prometheus → Grafana
Logging: Serilog (console JSON)
```

### Key Technical Features

- **.NET 9.0** - Latest LTS runtime
- **PostgreSQL 18** - Primary data store with retry-on-failure
- **Redis 7** - Distributed cache with Polly circuit breaker
- **Stale-while-revalidate** - 1-hour grace period for cache entries
- **Prometheus metrics** - Custom business metrics + HTTP telemetry
- **Health checks** - Liveness and readiness endpoints with degraded mode support
- **JWT authentication** - Role-based authorization (CountryAdmin, SuperAdmin)
- **Rate limiting** - Sliding window (100/min reads, 20/min admin operations)
- **API versioning** - URL segment versioning (v1)

## Quick Start

### Prerequisites

- .NET 9.0 SDK
- Docker Desktop (for local PostgreSQL and Redis)
- Git

### Local Development Setup

1. **Clone the repository**:
   ```bash
   git clone https://github.com/MALIEV-Co-Ltd/Maliev.CountryService.git
   cd Maliev.CountryService
   ```

2. **Start infrastructure services**:
   ```bash
   docker-compose -f docker-compose.test.yml up -d
   ```

   This starts:
   - PostgreSQL 18 on `localhost:5432`
   - Redis 7 on `localhost:6379`

3. **Apply database migrations**:
   ```bash
   export ConnectionStrings__CountryServiceDbContext="Server=localhost;Port=5432;Database=country_service_db;User Id=postgres;Password=postgres_dev_password;"

   dotnet ef database update --project Maliev.CountryService.Data
   ```

4. **Run the service**:
   ```bash
   dotnet run --project Maliev.CountryService.Api
   ```

   The service will start on `https://localhost:5001` (HTTPS) and `http://localhost:5000` (HTTP).

5. **Access the API**:
   - **OpenAPI documentation**: `http://localhost:5000/openapi/v1.json`
   - **Health checks**:
     - Liveness: `http://localhost:5000/country/v1/liveness`
     - Readiness: `http://localhost:5000/country/v1/readiness`
   - **Metrics**: `http://localhost:5000/metrics`

### Running Tests

```bash
# Run all tests
dotnet test Maliev.CountryService.sln --verbosity normal

# Run with coverage
dotnet test Maliev.CountryService.sln --collect:"XPlat Code Coverage"
```

## API Endpoints

### Public Read Endpoints (Rate limit: 100 req/min per IP)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/country/v1/countries/{id}` | Get country by ID |
| `GET` | `/country/v1/countries/iso2/{iso2}` | Get country by ISO2 code (e.g., "US") |
| `GET` | `/country/v1/countries/iso3/{iso3}` | Get country by ISO3 code (e.g., "USA") |
| `GET` | `/country/v1/countries` | List countries (paginated, filterable, sortable) |
| `GET` | `/country/v1/countries/search?q={query}` | Full-text search countries |

### Admin Endpoints (Rate limit: 20 req/min per user, requires JWT)

| Method | Endpoint | Description | Required Policy |
|--------|----------|-------------|-----------------|
| `POST` | `/country/v1/admin/countries` | Create new country | CountryAdmin |
| `PUT` | `/country/v1/admin/countries/{id}` | Update country (requires If-Match) | CountryAdmin |
| `PATCH` | `/country/v1/admin/countries/{id}` | Partial update country (requires If-Match) | CountryAdmin |
| `DELETE` | `/country/v1/admin/countries/{id}?hard=false` | Soft delete country | CountryAdmin |
| `DELETE` | `/country/v1/admin/countries/{id}?hard=true` | Hard delete country | SuperAdmin |

### Bulk Import Endpoints (CountryAdmin required)

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/country/v1/admin/bulk-import` | Submit bulk import (max 1,000 countries) |
| `GET` | `/country/v1/admin/bulk-import/{jobId}` | Get import job status |
| `POST` | `/country/v1/admin/bulk-import/{jobId}/process` | Trigger processing of validated job |

## API Examples

See [specs/001-country-service/contracts/examples/](specs/001-country-service/contracts/examples/) for detailed curl examples.

### Quick Examples

**Get country by ISO2 code**:
```bash
curl http://localhost:5000/country/v1/countries/iso2/US
```

**List countries with pagination**:
```bash
curl "http://localhost:5000/country/v1/countries?page=1&pageSize=20&sortBy=name&sortOrder=asc"
```

**Search countries**:
```bash
curl "http://localhost:5000/country/v1/countries/search?q=united&page=1&pageSize=10"
```

**Create country (requires JWT)**:
```bash
curl -X POST http://localhost:5000/country/v1/admin/countries \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "iso2": "XX",
    "iso3": "XXX",
    "name": "Example Country",
    "region": "Europe",
    "subregion": "Western Europe"
  }'
```

## Configuration

### Environment Variables / Secrets

The service uses Google Secret Manager in production. For local development, configure in `appsettings.Development.json`:

| Secret Name | Description | Example |
|-------------|-------------|---------|
| `ConnectionStrings__CountryServiceDbContext` | PostgreSQL connection string | `Server=localhost;Port=5432;Database=country_service_db;...` |
| `ConnectionStrings__Redis` | Redis connection string | `localhost:6379` |
| `JwtBearer__Issuer` | JWT token issuer | `https://auth.maliev.com` |
| `JwtBearer__Audience` | JWT audience | `country-service` |
| `JwtBearer__SecurityKey` | JWT signing key | (base64 encoded key) |

### Key Metrics

The service exposes Prometheus metrics at `/metrics`:

| Metric | Type | Description |
|--------|------|-------------|
| `country_cache_hits_total` | Counter | Cache hits by type (redis/memory) |
| `country_cache_misses_total` | Counter | Cache misses by type |
| `country_request_duration_seconds` | Histogram | Request duration (p50, p95, p99) |
| `country_circuit_breaker_state` | Gauge | Circuit breaker state (0=Closed, 1=Open, 2=Half-Open) |
| `country_active_total` | Gauge | Total active countries |
| `country_create_operations_total` | Counter | Country create operations |
| `country_update_operations_total` | Counter | Country update operations |
| `country_delete_operations_total` | Counter | Country delete operations |
| `country_bulk_import_jobs_total` | Counter | Bulk import jobs by status |
| `country_bulk_import_duration_seconds` | Histogram | Bulk import duration |

## Deployment

### GitOps Workflow

The service uses GitOps for deployment with ArgoCD monitoring the `maliev-gitops` repository.

```bash
# Deployment triggered automatically by CI/CD
# 1. Push to branch → GitHub Actions builds Docker image
# 2. Image pushed to GCP Artifact Registry
# 3. GitOps repository updated with new image tag
# 4. ArgoCD syncs changes to Kubernetes cluster
```

### Kubernetes Manifests

Kubernetes manifests are in the `maliev-gitops` repository under `3-apps/country-service/`:

- **Base**: Common configuration
- **Overlays**: Environment-specific configs (development, staging, production)

### Manual Deployment

```bash
# Build Docker image
docker build -t country-service:latest .

# Run container
docker run -p 8080:8080 \
  -e ConnectionStrings__CountryServiceDbContext="..." \
  -e ConnectionStrings__Redis="..." \
  country-service:latest
```

## Performance

### Latency Targets

- **Cached reads**: p95 < 50ms
- **Uncached reads**: p95 < 200ms
- **List operations**: p95 < 100ms (with cache)
- **Admin operations**: p95 < 500ms

### Load Testing

Run k6 load tests:

```bash
k6 run specs/001-country-service/loadtest.k6.js
```

Expected results:
- 1,000 virtual users
- 95th percentile < 50ms for cached reads
- 99th percentile < 200ms for uncached reads
- Zero errors under normal conditions

## Resilience Features

### Graceful Degradation

The service maintains read availability during infrastructure failures:

1. **Redis outage**: Automatic fallback to in-memory cache
2. **Database outage**: Serve stale cache data with `X-Degraded-Mode: true` header
3. **Circuit breaker**: Polly circuit breaker with 50% failure threshold, 60-second break

### Health Check States

- **Healthy**: All dependencies available
- **Degraded**: One dependency unavailable but service functional (e.g., Redis down, database serving from cache)
- **Unhealthy**: Critical dependencies unavailable (e.g., database and cache both down)

### Retry Guidance

503 responses include `Retry-After: 60` header (circuit breaker duration).

## Development

### Project Structure

```
Maliev.CountryService/
├── Maliev.CountryService.Api/        # Web API project
│   ├── Controllers/                  # API endpoints
│   ├── Services/                     # Business logic
│   ├── Models/                       # DTOs
│   ├── Middleware/                   # HTTP middleware
│   ├── HealthChecks/                 # Custom health checks
│   ├── BackgroundServices/           # Hosted services
│   └── Metrics/                      # Prometheus metrics
├── Maliev.CountryService.Data/       # Data layer
│   ├── Models/                       # EF Core entities
│   ├── Migrations/                   # Database migrations
│   └── CountryServiceDbContext.cs    # DbContext
├── Maliev.CountryService.Tests/      # Unit & integration tests
└── specs/001-country-service/        # Specification docs
    ├── spec.md                       # Feature specification
    ├── plan.md                       # Implementation plan
    ├── tasks.md                      # Task breakdown
    ├── data-model.md                 # Database schema
    ├── contracts/                    # API contracts
    └── quickstart.md                 # Setup guide
```

### Code Standards

- **Zero warnings**: All projects have `TreatWarningsAsErrors` enabled
- **No secrets in code**: Secrets via Google Secret Manager or appsettings
- **Structured logging**: Serilog with correlation IDs
- **Rate limiting**: All endpoints have rate limits
- **Optimistic concurrency**: ETag-based for all updates

## Troubleshooting

### Database Connection Issues

```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Test connection
psql -h localhost -p 5432 -U postgres -d country_service_db
```

### Redis Connection Issues

```bash
# Check Redis is running
docker ps | grep redis

# Test connection
redis-cli -h localhost -p 6379 ping
```

### Migration Issues

```bash
# Reset database (DEVELOPMENT ONLY)
dotnet ef database drop --project Maliev.CountryService.Data
dotnet ef database update --project Maliev.CountryService.Data
```

## Links

- **Specification**: [specs/001-country-service/spec.md](specs/001-country-service/spec.md)
- **Implementation Plan**: [specs/001-country-service/plan.md](specs/001-country-service/plan.md)
- **API Contracts**: [specs/001-country-service/contracts/](specs/001-country-service/contracts/)
- **Quick Start Guide**: [specs/001-country-service/quickstart.md](specs/001-country-service/quickstart.md)
- **GitOps Repository**: https://github.com/MALIEV-Co-Ltd/maliev-gitops
- **Grafana Dashboards**: (Access via `scripts/open-grafana.ps1` in maliev-gitops)

## License

Proprietary - Maliev Co. Ltd.

## Support

For issues or questions:
- **GitHub Issues**: https://github.com/MALIEV-Co-Ltd/Maliev.CountryService/issues
- **Internal Wiki**: (Company Confluence/Notion)
