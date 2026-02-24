# Quickstart: Country WebAPI Service

**Branch**: `001-country-service`

## Prerequisites

- **.NET 10.0 SDK**
- **Docker Desktop** (running, for Testcontainers)
- **IDE**: VS Code or Visual Studio 2022+

## Building the Project

```bash
# Restore dependencies (requires valid nuget.config)
dotnet restore

# Build
dotnet build --no-restore
```

## Running Tests (Real Infrastructure)

This project uses **Testcontainers** for all integration tests. You do NOT need to manually start PostgreSQL or Redis. Docker must be running.

```bash
# Run all tests
dotnet test
```

## Running Locally

1.  **Start Infrastructure**:
    ```bash
    # Optional: Start dependencies if not using Aspire orchestration locally
    docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:18-alpine
    docker run -d -p 6379:6379 redis:alpine
    ```

2.  **Configure Secrets**:
    Ensure `appsettings.Development.json` or User Secrets points to your local infrastructure.

3.  **Run API**:
    ```bash
    cd Maliev.CountryService.Api
    dotnet run
    ```

4.  **Access API**:
    - Scalar UI: `http://localhost:5000/countries/scalar`
    - Metrics: `http://localhost:5000/metrics`

## Project Structure

- `Maliev.CountryService.Api`: Main entry point.
- `Maliev.CountryService.Data`: EF Core context and migrations.
- `Maliev.CountryService.Tests`: Integration tests using Testcontainers.

## Common Commands

- **Add Migration**:
  ```bash
  dotnet ef migrations add InitialCreate -p Maliev.CountryService.Data -s Maliev.CountryService.Api
  ```

- **Update Database**:
  ```bash
  # Database is automatically migrated on startup in non-Testing environments
  # Or manually:
  dotnet ef database update -p Maliev.CountryService.Data -s Maliev.CountryService.Api
  ```