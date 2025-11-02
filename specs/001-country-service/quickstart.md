# Quickstart Guide: Country WebAPI Service

**Feature**: Country WebAPI Service
**Branch**: `001-country-service`
**Date**: 2025-11-01

## Overview

This guide provides step-by-step instructions for setting up the Country WebAPI Service for local development, running tests, and deploying to Kubernetes.

---

## Prerequisites

Before starting, ensure you have the following installed:

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | 9.0 or later | Building and running the service |
| Docker Desktop | Latest | Running PostgreSQL and Redis locally |
| kubectl | Latest | Kubernetes management (for deployment) |
| Git | Latest | Version control |
| PowerShell | 7.0+ (recommended) | Running migration scripts |

**Optional** (for advanced development):
- [Postman](https://www.postman.com/) or [Bruno](https://www.usebruno.com/) for API testing
- [pgAdmin 4](https://www.pgadmin.org/) for database management
- [Redis Insight](https://redis.com/redis-enterprise/redis-insight/) for cache monitoring

---

## Local Development Setup

### Step 1: Clone the Repository

```bash
cd R:\maliev
git clone https://github.com/MALIEV-Co-Ltd/Maliev.CountryService.git
cd Maliev.CountryService
git checkout 001-country-service
```

### Step 2: Start PostgreSQL and Redis with Docker Compose

The service requires PostgreSQL 18 and Redis for local development.

**Create `docker-compose.test.yml`** (if not exists):

```yaml
version: '3.8'

services:
  postgres:
    image: postgres:18
    container_name: country-service-postgres
    environment:
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: country_service_app_db
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U postgres"]
      interval: 5s
      timeout: 5s
      retries: 5

  redis:
    image: redis:7-alpine
    container_name: country-service-redis
    ports:
      - "6379:6379"
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 5s
      timeout: 3s
      retries: 5

volumes:
  postgres_data:
```

**Start the services**:

```bash
docker-compose -f docker-compose.test.yml up -d
```

**Verify services are running**:

```bash
docker ps
# Should show both country-service-postgres and country-service-redis as "Up"
```

### Step 3: Configure Connection Strings

**For local development**, connection strings are configured in `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "CountryServiceDbContext": "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"
  },
  "Redis": {
    "ConnectionString": "localhost:6379"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

**Note**: These are localhost-only defaults. Production secrets are managed via Google Secret Manager.

### Step 4: Apply Database Migrations

Navigate to the Data project and apply EF Core migrations:

**Option A: Using dotnet ef command**

```bash
# Set connection string as environment variable
$env:CountryServiceDbContext = "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"

# Apply migrations
dotnet ef database update --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api
```

**Option B: Using PowerShell script** (recommended)

```powershell
# From repository root
.\scripts\apply-migrations-local.ps1
```

**Verify migration**:

```bash
# Connect to PostgreSQL
docker exec -it country-service-postgres psql -U postgres -d country_service_app_db

# List tables
\dt

# Should show: countries, audit_logs, bulk_import_jobs, __ef_migrations_history
\q
```

### Step 5: Build the Solution

```bash
dotnet restore Maliev.CountryService.sln
dotnet build Maliev.CountryService.sln --no-restore
```

**Zero warnings policy**: The build must complete with zero warnings. If you see warnings, fix them before proceeding.

### Step 6: Run Tests

```bash
dotnet test Maliev.CountryService.sln --no-build --verbosity normal
```

**Expected output**: All tests should pass. Tests use real PostgreSQL database (no in-memory provider).

### Step 7: Run the API Locally

```bash
cd Maliev.CountryService.Api
dotnet run --environment Development
```

**Expected output**:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### Step 8: Access API Documentation

Open your browser and navigate to:

- **Scalar UI**: [http://localhost:5000/countries/v1/scalar/v1](http://localhost:5000/countries/v1/scalar/v1)
- **Swagger JSON**: [http://localhost:5000/countries/v1/swagger/v1/swagger.json](http://localhost:5000/countries/v1/swagger/v1/swagger.json)

**Scalar UI** provides an interactive API explorer with request/response examples.

---

## Testing the API

### Anonymous Read Endpoints (No Authentication)

#### Get Country by ISO2 Code

```bash
curl http://localhost:5000/countries/v1/countries/iso2/US
```

**Expected Response** (200 OK):

```json
{
  "id": 1,
  "iso2": "US",
  "iso3": "USA",
  "name": "United States",
  "region": "Americas",
  "population": 331900000,
  "isActive": true,
  "version": "123e4567-e89b-12d3-a456-426614174000",
  "createdAtUtc": "2025-01-01T00:00:00Z",
  "lastModifiedUtc": "2025-01-01T00:00:00Z"
}
```

#### List All Countries

```bash
curl "http://localhost:5000/countries/v1/countries?page=1&pageSize=10"
```

#### Search Countries by Name

```bash
curl "http://localhost:5000/countries/v1/countries/search?q=united"
```

### Admin Endpoints (Requires JWT Authentication)

For local development, you'll need a JWT token from the Maliev Auth Service.

#### Create a Country

```bash
JWT_TOKEN="<your-jwt-token>"

curl -X POST http://localhost:5000/countries/v1/admin/countries \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "iso2": "JP",
    "iso3": "JPN",
    "name": "Japan",
    "capital": "Tokyo",
    "region": "Asia",
    "population": 125800000,
    "independent": true,
    "unMember": true,
    "landlocked": false
  }'
```

**Expected Response** (201 Created):

```json
{
  "id": 2,
  "iso2": "JP",
  "iso3": "JPN",
  "name": "Japan",
  "capital": "Tokyo",
  "region": "Asia",
  "population": 125800000,
  "isActive": true,
  "version": "abc123-def456-...",
  "createdAtUtc": "2025-11-01T10:00:00Z",
  "lastModifiedUtc": "2025-11-01T10:00:00Z"
}
```

#### Update a Country (with ETag)

```bash
# 1. Get current country and ETag
ETAG=$(curl -s -D - http://localhost:5000/countries/v1/countries/2 | grep -i etag | cut -d' ' -f2 | tr -d '\r')

# 2. Update with If-Match header
curl -X PUT http://localhost:5000/countries/v1/admin/countries/2 \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "If-Match: $ETAG" \
  -H "Content-Type: application/json" \
  -d '{
    "iso2": "JP",
    "iso3": "JPN",
    "name": "Japan",
    "capital": "Tokyo",
    "region": "Asia",
    "population": 126000000,
    "independent": true,
    "unMember": true,
    "landlocked": false
  }'
```

**Expected Response** (200 OK with new ETag)

**If ETag doesn't match**: 412 Precondition Failed

---

## Health Checks

### Liveness Probe

```bash
curl http://localhost:5000/countries/v1/liveness
```

**Expected**: `Healthy`

### Readiness Probe

```bash
curl http://localhost:5000/countries/v1/readiness
```

**Expected Response**:

```json
{
  "status": "Healthy",
  "checks": {
    "database": {
      "status": "Healthy",
      "description": "PostgreSQL connection successful"
    },
    "redis": {
      "status": "Healthy",
      "description": "Redis connection successful"
    }
  }
}
```

---

## Database Management

### Accessing PostgreSQL

```bash
# Connect to PostgreSQL container
docker exec -it country-service-postgres psql -U postgres -d country_service_app_db

# Common queries
SELECT * FROM countries LIMIT 10;
SELECT * FROM audit_logs ORDER BY created_at_utc DESC LIMIT 20;
SELECT * FROM bulk_import_jobs ORDER BY created_at_utc DESC;

# Check table sizes
SELECT
  schemaname,
  tablename,
  pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
```

### Creating New Migrations

When modifying entity models, create a new migration:

```bash
# 1. Modify entity classes in Maliev.CountryService.Data/Models/

# 2. Generate migration
dotnet ef migrations add <MigrationName> \
  --project Maliev.CountryService.Data \
  --startup-project Maliev.CountryService.Api \
  --output-dir Migrations

# 3. Review generated migration in Maliev.CountryService.Data/Migrations/

# 4. Apply migration
dotnet ef database update --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api
```

### Reverting Migrations

```bash
# Revert to previous migration
dotnet ef database update <PreviousMigrationName> \
  --project Maliev.CountryService.Data \
  --startup-project Maliev.CountryService.Api

# Remove last migration from code (only if not applied to production!)
dotnet ef migrations remove \
  --project Maliev.CountryService.Data \
  --startup-project Maliev.CountryService.Api
```

---

## Monitoring and Debugging

### Prometheus Metrics

Metrics are exposed at `/metrics` endpoint:

```bash
curl http://localhost:5000/metrics
```

**Key metrics**:
- `country_cache_hits_total{type="fresh|stale"}`
- `country_cache_misses_total`
- `country_request_duration_seconds{endpoint="...",method="GET|POST|..."}`
- `country_active_count` (gauge)

### Viewing Logs

The service uses Serilog with structured logging to stdout.

**Watch logs in real-time**:

```bash
cd Maliev.CountryService.Api
dotnet run --environment Development | jq '.'
```

**Sample log output**:

```json
{
  "Timestamp": "2025-11-01T10:15:23.4567890Z",
  "Level": "Information",
  "MessageTemplate": "Country {CountryId} retrieved from cache",
  "Properties": {
    "CountryId": 1,
    "CacheHit": true,
    "CacheAge": 45
  }
}
```

---

## Kubernetes Deployment (Development Environment)

### Prerequisites

- Access to Maliev GKE cluster
- `kubectl` configured with cluster credentials
- GitHub Actions workflows enabled

### Deploy to Development

**Option 1: Via GitHub Actions (Recommended)**

```bash
# Push to develop branch triggers CI/CD
git checkout develop
git merge 001-country-service
git push origin develop
```

GitHub Actions will:
1. Build and test the solution
2. Build Docker image
3. Push to Artifact Registry
4. Update maliev-gitops repository
5. ArgoCD auto-syncs to GKE cluster

**Option 2: Manual Deployment**

```bash
# 1. Build Docker image
docker build -t asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/country-service:local .

# 2. Push to Artifact Registry
docker push asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/country-service:local

# 3. Update maliev-gitops (separate repository)
cd ../maliev-gitops
cd 3-apps/country-service/overlays/development
kustomize edit set image country-service=asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-dev/country-service:local
git add . && git commit -m "Deploy country-service local build" && git push

# 4. Wait for ArgoCD sync (automatic within 3 minutes)
```

### Verify Deployment

```bash
# Check pod status
kubectl get pods -n maliev-dev -l app=maliev-country-service

# Check service
kubectl get svc -n maliev-dev maliev-country-service

# View logs
kubectl logs -f deployment/maliev-country-service -n maliev-dev

# Port-forward to access locally
kubectl port-forward -n maliev-dev svc/maliev-country-service 8080:80

# Access API
curl http://localhost:8080/countries/v1/countries
```

### Applying Migrations in Kubernetes

**IMPORTANT**: Migrations MUST be applied manually to production databases.

```bash
# 1. Port-forward directly to PostgreSQL pod (NOT service)
kubectl get pods -n maliev-dev -l app=postgres-cluster
kubectl port-forward -n maliev-dev postgres-cluster-1 5432:5432

# 2. Set connection string with production credentials
$env:CountryServiceDbContext = "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=<ACTUAL_PASSWORD>;"

# 3. Apply migration
dotnet ef database update --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api

# 4. Stop port-forward
# Press Ctrl+C
```

---

## Troubleshooting

### Issue: Docker Compose fails to start

**Error**: `port 5432 is already allocated`

**Solution**:

```bash
# Stop conflicting PostgreSQL instance
docker ps -a | grep postgres
docker stop <container-id>

# Restart docker-compose
docker-compose -f docker-compose.test.yml up -d
```

### Issue: EF Core migrations fail

**Error**: `No connection string named 'CountryServiceDbContext' was found`

**Solution**:

```bash
# Set environment variable explicitly
$env:CountryServiceDbContext = "Server=localhost;Port=5432;Database=country_service_app_db;User Id=postgres;Password=postgres;"

# Retry migration
dotnet ef database update --project Maliev.CountryService.Data --startup-project Maliev.CountryService.Api
```

### Issue: Tests fail with database connection error

**Error**: `Npgsql.NpgsqlException: connection refused`

**Solution**:

```bash
# Check PostgreSQL is running
docker ps | grep postgres

# Verify health check
docker exec country-service-postgres pg_isready -U postgres

# Restart if unhealthy
docker-compose -f docker-compose.test.yml restart postgres
```

### Issue: JWT authentication fails

**Error**: `401 Unauthorized` when calling admin endpoints

**Solution**:

1. Verify JWT token is valid:
   ```bash
   # Decode JWT (without verification)
   echo "<your-jwt-token>" | cut -d'.' -f2 | base64 -d | jq '.'
   ```

2. Check token has required claims:
   - `sub`: User ID
   - `role`: `CountryAdmin` or `SuperAdmin`

3. Ensure token is not expired (`exp` claim)

### Issue: Redis connection fails

**Error**: `StackExchange.Redis.RedisConnectionException: It was not possible to connect to the redis server(s)`

**Solution**:

```bash
# Check Redis is running
docker ps | grep redis

# Test connection
docker exec country-service-redis redis-cli ping
# Should return "PONG"

# Restart if needed
docker-compose -f docker-compose.test.yml restart redis
```

---

## Performance Testing

### Load Testing with k6

Create `load-test.js`:

```javascript
import http from 'k6/http';
import { check, sleep } from 'k6';

export let options = {
  stages: [
    { duration: '30s', target: 100 }, // Ramp up to 100 users
    { duration: '1m', target: 100 },  // Stay at 100 users
    { duration: '30s', target: 0 },   // Ramp down
  ],
  thresholds: {
    http_req_duration: ['p(95)<50'], // 95% of requests must complete below 50ms
  },
};

export default function () {
  const res = http.get('http://localhost:5000/countries/v1/countries/iso2/US');

  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 50ms': (r) => r.timings.duration < 50,
  });

  sleep(0.1);
}
```

**Run test**:

```bash
k6 run load-test.js
```

**Expected**: p95 latency <50ms for cached reads

---

## Next Steps

1. **Read the API specification**: Review [contracts/openapi.yaml](./contracts/openapi.yaml)
2. **Understand the data model**: Review [data-model.md](./data-model.md)
3. **Review technology decisions**: Read [research.md](./research.md)
4. **Start implementing**: Use `/speckit.tasks` to generate implementation tasks

---

## Additional Resources

- **Maliev Microservices Standards**: See [CLAUDE.md](../../CLAUDE.md)
- **Constitution Principles**: See [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- **Feature Specification**: See [spec.md](./spec.md)
- **Implementation Plan**: See [plan.md](./plan.md)

For questions or issues, contact the Maliev Platform Team.
