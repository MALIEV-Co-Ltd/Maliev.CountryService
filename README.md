# Maliev Country Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.CountryService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

High-performance reference data service for international country and region information.

**Role in MALIEV Architecture**: The centralized source of truth for geographical reference data. It provides ISO standard country codes, names, and regional hierarchies to all other services, ensuring consistent internationalization and data validation across the platform.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (Low-latency reference data resolution)
- **Messaging**: RabbitMQ via MassTransit
- **API Documentation**: OpenAPI 3.1 + Scalar UI
- **Resilience**: Polly Circuit Breaker with stale-data fallback

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Standardized Reference Data**: Full support for ISO-3166-1 alpha-2 and alpha-3 country codes.
- **Ultra-Low Latency**: Sub-50ms p95 read latency through multi-layered caching strategies.
- **Graceful Degradation**: Stale-while-revalidate pattern ensures data availability even during database outages.
- **Bulk Import Engine**: Efficient management of large-scale country dataset updates (up to 1,000 records).
- **Search & Discovery**: Full-text search and paginated listing for easy resource discovery.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.CountryService.git
cd Maliev.CountryService
```

2. **Spin up Infrastructure**
```bash
docker run --name country-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name country-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__CountryDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.CountryService.Api
dotnet run --project Maliev.CountryService.Api
```

The service will be available at `http://localhost:5000/country`. Access the interactive documentation at `http://localhost:5000/country/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/country/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/countries` | List countries (paginated) |
| GET | `/countries/iso2/{iso2}` | Get country by ISO2 code |
| GET | `/countries/search` | Full-text search for countries |
| POST | `/admin/bulk-import` | Submit large-scale updates (Admin) |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /country/liveness`
- **Readiness**: `GET /country/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /country/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-country-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
