# Maliev.CountryService

A comprehensive CRUD API service for managing country data with advanced features including caching, rate limiting, and full-text search capabilities. Built with ASP.NET Core 9.0 and designed for high-performance, scalable operations.

## 🌟 Features

### Core Functionality
- **Complete CRUD Operations**: Create, Read, Update, Delete countries with full validation
- **Advanced Search & Filtering**: Search by name, continent, ISO codes, and country codes
- **Paginated Results**: Efficient pagination with configurable page sizes
- **Normalized Data Model**: Proper one-to-many relationship for multiple country codes per country
- **Unique Constraints**: Enforced uniqueness for country names, ISO2, and ISO3 codes

### Performance & Scalability
- **Memory Caching**: Intelligent caching of country data with configurable TTL and automatic invalidation
- **Rate Limiting**: Multi-tiered rate limiting (global and endpoint-specific)
- **Optimized Queries**: Async/await patterns with efficient database queries
- **Resource Optimization**: CPU and memory optimized for resource-constrained environments

### Security & Compliance
- **JWT Authentication**: Bearer token authentication with configurable validation
- **CORS Configuration**: Secure cross-origin resource sharing
- **Input Validation**: Comprehensive request validation with detailed error responses
- **Secrets Management**: Flexible configuration via environment variables, files, or cloud secrets

### DevOps & Monitoring
- **Health Checks**: Liveness and readiness probes for container orchestration
- **Structured Logging**: Serilog with correlation IDs and contextual enrichment
- **Containerization**: Multi-stage Docker builds with security best practices
- **Observability**: Prometheus metrics and structured logging

## 🏗️ Architecture

### Project Structure
```
Maliev.CountryService/
├── Maliev.CountryService.Api/           # Web API layer
│   ├── Controllers/                     # API controllers
│   ├── Services/                        # Business logic services
│   ├── Models/                          # DTOs and request models
│   ├── Middleware/                      # Custom middleware
│   ├── HealthChecks/                    # Health check implementations
│   ├── Mapping/                         # AutoMapper profiles
│   ├── Exceptions/                      # Custom exception types
│   ├── Configurations/                  # Configuration models and options
│   └── Properties/                      # Application properties
├── Maliev.CountryService.Data/          # Data access layer
│   ├── DbContexts/                      # Entity Framework contexts
│   ├── Entities/                        # Database entities
│   └── Migrations/                      # EF Core migrations
├── Maliev.CountryService.Tests/         # Test suite
│   ├── Unit/                            # Unit tests
│   └── Integration/                     # Integration tests
└── .github/workflows/                   # CI/CD pipelines
```

### Technology Stack
- **Framework**: ASP.NET Core 9.0
- **Database**: PostgreSQL with Entity Framework Core 9.0
- **Caching**: In-Memory caching with configurable policies and automatic invalidation
- **Authentication**: JWT Bearer tokens
- **API Documentation**: OpenAPI/Swagger with versioning
- **Testing**: xUnit with FluentAssertions and Moq
- **Containerization**: Docker with multi-stage builds
- **CI/CD**: GitHub Actions
- **Monitoring**: Prometheus metrics and Serilog structured logging

## 🚀 Getting Started

### Prerequisites
- .NET 9.0 SDK
- PostgreSQL 12+ or Docker
- Visual Studio 2022 / VS Code (optional)

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Maliev.CountryService
   ```

2. **Configure database connection**
   
   **Option A: Using Docker PostgreSQL (Recommended for development)**
   ```bash
   # Start PostgreSQL in Docker
   docker run -d --name postgres-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=CountryService -p 5432:5432 postgres:15
   
   # Set connection string
   dotnet user-secrets set "ConnectionStrings:CountryDbContext" "Host=localhost;Port=5432;Database=CountryService;Username=postgres;Password=postgres"
   ```
   
   **Option B: Using existing PostgreSQL instance**
   ```bash
   # Set connection string to your PostgreSQL instance
   dotnet user-secrets set "ConnectionStrings:CountryDbContext" "Host=your-host;Port=5432;Database=your-database;Username=your-username;Password=your-password"
   ```

3. **Create and run database migrations**
   ```bash
   cd Maliev.CountryService.Data
   dotnet ef database update --project ../Maliev.CountryService.Api
   ```

4. **Run the application**
   ```bash
   cd ../Maliev.CountryService.Api
   dotnet run
   ```

5. **Access the API**
   - Swagger UI: `http://localhost:5000/countries/swagger`
   - API Base URL: `http://localhost:5000/countries/v1.0`

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Docker Deployment
```bash
# Build container
docker build -t maliev-country-service -f Maliev.CountryService.Api/Dockerfile .

# Run container (requires PostgreSQL)
docker run -p 8080:8080 -e "ConnectionStrings__CountryDbContext=Host=host.docker.internal;Port=5432;Database=CountryService;Username=postgres;Password=postgres" maliev-country-service
```

## 📚 API Documentation

### Base URL
- **Development**: `http://localhost:5000/countries/v1.0`
- **Swagger UI**: `http://localhost:5000/countries/swagger`

### Authentication
All endpoints except liveness and readiness require JWT Bearer token authentication:
```
Authorization: Bearer <your-jwt-token>
```

### Core Endpoints

#### Countries
- `GET /countries/v1.0/{id}` - Get country by ID
- `GET /countries/v1.0/search` - Search countries with filters
- `GET /countries/v1.0` - Get all countries with pagination
- `POST /countries/v1.0` - Create new country
- `PUT /countries/v1.0/{id}` - Update existing country
- `DELETE /countries/v1.0/{id}` - Delete country

#### Utility
- `GET /countries/v1.0/continents` - Get list of continents
- `GET /countries/liveness` - Health check (liveness probe)
- `GET /countries/readiness` - Health check (readiness probe)
- `GET /countries/metrics` - Prometheus metrics

### Example Requests

#### Create Country
```json
POST /countries/v1.0
{
  "name": "Singapore",
  "continent": "Asia",
  "countryCode": "65",
  "iso2": "SG",
  "iso3": "SGP"
}
```

#### Search Countries
```
GET /countries/v1.0/search?continent=Asia&pageSize=10&pageNumber=1&sortBy=name&sortDirection=asc
```

#### Get All Countries (paginated)
```
GET /countries/v1.0?pageNumber=1&pageSize=50
```

### Response Format
```json
{
  "id": 1,
  "name": "Singapore",
  "continent": "Asia",
  "countryCodes": [
    {
      "id": 1,
      "code": "65",
      "isPrimary": true
    }
  ],
  "iso2": "SG",
  "iso3": "SGP",
  "createdDate": "2024-01-01T00:00:00Z",
  "modifiedDate": "2024-01-01T00:00:00Z"
}
```

## ⚙️ Configuration

### Environment Variables
- `ConnectionStrings__CountryDbContext` - Database connection string
- `Jwt__Issuer` - JWT issuer (optional)
- `Jwt__Audience` - JWT audience (optional)
- `Jwt__SecurityKey` - JWT signing key (optional)

### Configuration Sections

#### Rate Limiting
```json
{
  "RateLimit": {
    "Global": {
      "PermitLimit": 1000,
      "Window": "00:01:00",
      "QueueLimit": 100
    },
    "CountryEndpoint": {
      "PermitLimit": 100,
      "Window": "00:01:00",
      "QueueLimit": 50
    }
  }
}
```

#### Caching
```json
{
  "Cache": {
    "CountryCacheDurationMinutes": 60,
    "SearchCacheDurationMinutes": 30,
    "MaxCacheSize": 1000
  }
}
```

## 🧪 Testing

### Test Coverage
- **Unit Tests**: Service layer business logic testing
- **Integration Tests**: Full API endpoint testing with in-memory database
- **Validation Tests**: Input validation and error handling
- **Exception Handling Tests**: Specific exception scenarios

### Test Architecture
- **In-Memory Database**: Isolated test database for each test
- **Test Fixtures**: Reusable test data and configurations
- **Mocking**: External dependencies mocked for unit tests
- **Assertions**: FluentAssertions for readable test assertions

## 🔒 Security

### Authentication & Authorization
- JWT Bearer token validation (optional - can be disabled for development)
- Configurable token validation parameters
- Rate limiting per IP address
- CORS policy enforcement

### Data Protection
- Input sanitization and validation
- SQL injection prevention via Entity Framework
- Secure secret management via environment variables or mounted volumes
- Connection string and JWT secrets loaded from secure sources only

### Container Security
- Non-root user execution
- Minimal attack surface
- Health check implementation

## 📊 Monitoring & Observability

### Logging
- Structured JSON logging with Serilog
- Correlation ID tracking across requests
- Contextual enrichment (machine, process, thread)
- Configurable log levels and filtering

### Health Checks
- **Liveness**: Basic application health
- **Readiness**: Database connectivity and dependencies
- Kubernetes-native health check endpoints

### Metrics
- Prometheus metrics endpoint at `/countries/metrics`
- Response time monitoring
- Cache hit/miss rate tracking
- Rate limiting metrics
- Database query performance

## 🚀 Deployment

### Docker Compose (Simple Deployment)
```yaml
version: '3.8'
services:
  database:
    image: postgres:15
    environment:
      POSTGRES_DB: CountryService
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  api:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ConnectionStrings__CountryDbContext=Host=database;Port=5432;Database=CountryService;Username=postgres;Password=postgres
    depends_on:
      - database

volumes:
  postgres_data:
```

### Kubernetes Deployment
The service is designed to run in Kubernetes with:
- ConfigMaps for non-secret configuration
- Secrets for sensitive data
- Liveness and readiness probes
- Resource limits and requests
- Horizontal Pod Autoscaling

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Development Guidelines
- Follow existing code patterns and architecture
- Write comprehensive tests for new features
- Update documentation for API changes
- Ensure all tests pass and maintain code coverage
- Use conventional commit messages

## 📝 License

This project is part of the Maliev Co. Ltd. microservices ecosystem. All rights reserved.

## 🆘 Support

For support and questions:
- **API Issues**: Create GitHub issue with reproduction steps
- **Infrastructure**: Contact DevOps team
- **Business Logic**: Contact development team

---

**Maliev Country Service** - Built with ❤️ for scalable, reliable country data management.