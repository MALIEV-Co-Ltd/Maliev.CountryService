# Maliev.CountryService Development Guidelines

Last updated: 2025-12-28

## Active Technologies

- .NET 10.0 (ASP.NET Core WebAPI)
- PostgreSQL (via TestContainers for testing)
- Redis (caching)
- RabbitMQ (messaging)
- xUnit (testing framework)

## Project Structure

```text
Maliev.CountryService.Api/          # Web API layer
Maliev.CountryService.Data/         # Data access layer (EF Core)
Maliev.CountryService.Tests/        # Integration tests
```

## Commands

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"
```

## Code Style

- Follow standard .NET conventions
- Use meaningful variable names
- Add XML documentation for public APIs
- Use required fields for validation

## Test Guidelines

### Important Testing Rules

1. **Database Cleanup**: Always call `CleanDatabaseAsync()` at the start of each test
2. **Required Fields**: All `CreateCountryRequest` instances must include:
   - Timezones, Borders, CallingCodes, TopLevelDomains
   - Currencies, Languages, Translations, Flags
3. **ISO Code Validation**:
   - Iso2: Exactly 2 uppercase LETTERS (not digits) - Regex: `^[A-Z]{2}$`
   - Iso3: Exactly 3 uppercase LETTERS (not digits) - Regex: `^[A-Z]{3}$`
4. **Test Isolation**: ResilienceTests use a separate collection to avoid container interference

### Test Collections

- `[Collection("TestDatabase")]` - Standard tests with shared database container
- `[Collection("ResilienceTests")]` - Tests that manipulate database container (stop/start)

## Recent Changes

- 2025-12-28: Fixed all test failures (78/78 passing)
  - Isolated ResilienceTests to prevent container interference
  - Added required fields to all CreateCountryRequest instances
  - Fixed ISO code validation issues
  - Added proper cleanup in test finally blocks
  - Query by ISO code instead of ID after DB restart

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
