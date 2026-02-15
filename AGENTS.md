# Maliev.CountryService Development Guidelines for Agents

## 1. Commands

### Build & Run
- **Build**: `dotnet build`
- **Run API**: `dotnet run --project Maliev.CountryService.Api` (or use `F5` in VS/VS Code)

### Testing
- **Run All Tests**: `dotnet test`
- **Run Single Test**: `dotnet test --filter "FullyQualifiedName=Namespace.ClassName.MethodName"`
  - *Example*: `dotnet test --filter "FullyQualifiedName=Maliev.CountryService.Tests.Integration.CountriesControllerTests.GetById_ReturnsOk"`
- **Run Tests with Coverage**: `dotnet test --collect:"XPlat Code Coverage"`

### Linting & Formatting
- **Format Code**: `dotnet format`
- **Check Style**: Build the project; warnings are generally treated as errors in CI. Pay attention to build output.

## 2. Code Style & Conventions

### General
- **Framework**: .NET 10.0 (ASP.NET Core WebAPI).
- **Language**: C# 12+.
- **Nullable Reference Types**: Enabled (`<Nullable>enable</Nullable>`).
- **Observability**: Uses `Maliev.Aspire.ServiceDefaults` for OpenTelemetry, logging, and health checks.

### Naming Conventions
- **Classes/Methods/Properties**: `PascalCase`.
- **Parameters/Local Variables**: `camelCase`.
- **Private Fields**: `_camelCase` (underscore prefix).
- **Interfaces**: `IPascalCase` (prefix with 'I').
- **Async Methods**: Suffix with `Async` (e.g., `GetByIdAsync`).

### API Design
- **Controllers**: Inherit from `ControllerBase`, decorated with `[ApiController]`.
- **Versioning**: Use `[ApiVersion("1.0")]` and `[Route("country/v{version:apiVersion}/[controller]")]`.
- **Attributes**: Explicitly define HTTP verbs (`[HttpGet]`, `[HttpPost]`).
- **Response Types**: Use `[ProducesResponseType]` for all possible status codes (200, 404, etc.).
- **Validation**: Use Data Annotations and check `ModelState.IsValid` (handled automatically by `[ApiController]`).

### Asynchronous Programming
- Use `async/await` for all I/O operations.
- **Always** propagate `CancellationToken` to async methods (e.g., controller actions, services, EF Core calls).

### Dependency Injection
- Use constructor injection.
- Register services in `Program.cs` using the appropriate lifetime (`AddScoped`, `AddSingleton`, `AddTransient`).

### Logging
- Use `ILogger<T>` for logging.
- Prefer `[LoggerMessage]` source generator for high-performance logging (see `Program.cs` partial class example).

## 3. Testing Guidelines

### Framework
- **xUnit** for unit and integration tests.
- **TestContainers** for PostgreSQL, Redis, RabbitMQ.

### Test Structure
- **Integration Tests**: Inherit from `IntegrationTestBase`.
- **Collections**:
  - Use `[Collection("TestDatabase")]` for tests sharing the DB container.
  - Use `[Collection("ResilienceTests")]` for tests needing DB restart/manipulation.
- **Database Cleanup**: `CleanDatabaseAsync()` is called automatically in `IntegrationTestBase.InitializeAsync()`. Ensure your test class inherits correctly.
- **Factory**: Use `TestWebApplicationFactory` to create the test client.

### Critical Rules (from CLAUDE.md)
1. **Required Fields**: `CreateCountryRequest` MUST include:
   - `Timezones`, `Borders`, `CallingCodes`, `TopLevelDomains`
   - `Currencies`, `Languages`, `Translations`, `Flags`
2. **ISO Validation**:
   - Iso2: `^[A-Z]{2}$` (2 uppercase letters).
   - Iso3: `^[A-Z]{3}$` (3 uppercase letters).
3. **Resilience**: Query by ISO code instead of ID after DB restarts in resilience tests (IDs may change or be unreliable across resets/seeds if not careful, though UUIDs typically persist, logic implies state reset).

## 4. Project Structure
- `Maliev.CountryService.Api`: Main Web API application.
  - `Controllers/`: API Endpoints.
  - `Services/`: Business logic.
  - `Models/`: DTOs.
  - `Data/`: EF Core context and entities.
- `Maliev.CountryService.Tests`: Integration and Unit tests.
  - `Integration/`: End-to-end tests with TestContainers.
  - `Fixtures/`: Shared test context and factories.

## 5. Error Handling
- Use global exception handling (middleware/exception filters).
- Return standard `ProblemDetails` or specific error DTOs.
- `NotFound()` for missing resources.
- `BadRequest()` for validation failures.
