# Maliev.CountryService

A comprehensive CRUD API service for managing country data with advanced features including caching, rate limiting, and full-text search capabilities. Built with ASP.NET Core 9.0 and designed for high-performance, scalable operations in a microservices architecture.

## 🌟 Features

### Core Functionality
- **Complete CRUD Operations**: Create, Read, Update, Delete countries with full validation
- **Advanced Search & Filtering**: Search by name, continent, ISO codes, and country codes
- **Paginated Results**: Efficient pagination with configurable page sizes
- **Unique Constraints**: Enforced uniqueness for country names, ISO2, ISO3, and country codes

### Performance & Scalability
- **Memory Caching**: Intelligent caching of country data with configurable TTL
- **Rate Limiting**: Multi-tiered rate limiting (global and endpoint-specific)
- **Optimized Queries**: Async/await patterns with efficient database queries
- **Resource Optimization**: CPU and memory optimized for resource-constrained environments

### Security & Compliance
- **JWT Authentication**: Bearer token authentication with configurable validation
- **CORS Configuration**: Secure cross-origin resource sharing
- **Input Validation**: Comprehensive request validation with detailed error responses
- **Secrets Management**: Kubernetes-native secret injection

### DevOps & Monitoring
- **Health Checks**: Liveness and readiness probes for Kubernetes
- **Structured Logging**: Serilog with correlation IDs and contextual enrichment
- **Containerization**: Multi-stage Docker builds with security best practices
- **GitOps Ready**: ArgoCD and Kustomize integration for automated deployments

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
│   └── Configurations/                  # Swagger and service configurations
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
- **Caching**: In-Memory caching with configurable policies
- **Authentication**: JWT Bearer tokens
- **API Documentation**: OpenAPI/Swagger with versioning
- **Testing**: xUnit with FluentAssertions and Moq
- **Containerization**: Docker with multi-stage builds
- **CI/CD**: GitHub Actions with GitOps deployment

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
   
   **🔐 PRODUCTION SECURITY: All secrets are managed via Google Secret Manager**
   
   The application loads configuration in this order:
   1. Google Secret Manager (via mounted Kubernetes secrets in `/mnt/secrets/`)
   2. User Secrets (development only)
   3. Environment Variables
   4. appsettings.json (no secrets stored here)
   
   **For Development Setup:**
   
   **Option A: Full Production Environment (Recommended)**
   ```bash
   # Connect to development cluster and port-forward to database
   kubectl port-forward -n maliev-dev service/postgres-cluster-rw 5432:5432
   
   # Set Google Cloud credentials (secrets loaded automatically)
   export GOOGLE_APPLICATION_CREDENTIALS="path/to/your/service-account.json"
   
   # Run the application - all secrets loaded from Google Secret Manager
   dotnet run
   ```
   
   **Option B: Local Development (Limited - No Authentication)**
   ```bash
   # For local PostgreSQL only (JWT authentication will be disabled)
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=country_app_db;Username=postgres;Password=your-local-password"
   
   # ⚠️ WARNING: API will start without authentication for development only
   dotnet run
   ```
   
   **Option C: Production Deployment**
   ```yaml
   # Secrets automatically mounted in Kubernetes from Google Secret Manager
   # via External Secrets Operator at /mnt/secrets/
   # No manual configuration needed
   ```

3. **Create and run database migrations**
   ```bash
   cd Maliev.CountryService.Data
   .\apply-migration.ps1 -ServiceName "country"
   ```

4. **Run the application**
   ```bash
   dotnet run --project Maliev.CountryService.Api
   ```

5. **Access the API**
   - Swagger UI: `https://localhost:5001/countries/swagger`
   - API Base URL: `https://localhost:5001/countries/v1.0`

### Running Tests
```bash
# Run all tests
dotnet test Maliev.CountryService.sln

# Run with coverage
dotnet test Maliev.CountryService.sln --collect:"XPlat Code Coverage"
```

### Docker Deployment
```bash
# Build container
docker build -t maliev-country-service -f Maliev.CountryService.Api/Dockerfile .

# Run container
docker run -p 8080:8080 maliev-country-service
```

## 📚 API Documentation

### Base URL
- **Development**: `https://dev.api.maliev.com/countries/v1.0`
- **Production**: `https://api.maliev.com/countries/v1.0`

### Authentication
All endpoints require JWT Bearer token authentication:
```
Authorization: Bearer <your-jwt-token>
```

### Core Endpoints

#### Countries
- `GET /countries/v1.0/{id}` - Get country by ID
- `GET /countries/v1.0/search` - Search countries with filters
- `POST /countries/v1.0` - Create new country
- `PUT /countries/v1.0/{id}` - Update existing country
- `DELETE /countries/v1.0/{id}` - Delete country

#### Utility
- `GET /countries/v1.0/continents` - Get list of continents
- `GET /countries/liveness` - Health check (liveness probe)
- `GET /countries/readiness` - Health check (readiness probe)

### Example Requests

#### Create Country
```json
POST /countries/v1.0
{
  "name": "Singapore",
  "continent": "Asia",
  "countryCode": 65,
  "iso2": "SG",
  "iso3": "SGP"
}
```

#### Search Countries
```
GET /countries/v1.0/search?continent=Asia&pageSize=10&pageNumber=1&sortBy=name&sortDirection=asc
```

### Response Format
```json
{
  "id": 1,
  "name": "Singapore",
  "continent": "Asia",
  "countryCode": 65,
  "iso2": "SG",
  "iso3": "SGP",
  "createdDate": "2024-01-01T00:00:00Z",
  "modifiedDate": "2024-01-01T00:00:00Z"
}
```

## ⚙️ Configuration

### Environment Variables
- `ConnectionStrings__CountryDbContext` - Database connection string
- `Jwt__SecurityKey` - JWT signing key
- `Jwt__Issuer` - JWT issuer
- `Jwt__Audience` - JWT audience

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
    "MaxCacheSize": 1000,
    "SearchCacheDurationMinutes": 30
  }
}
```

## 🧪 Testing

### Test Coverage
- **Unit Tests**: Service layer business logic testing
- **Integration Tests**: Full API endpoint testing with in-memory database
- **Validation Tests**: Input validation and error handling
- **Performance Tests**: Caching and rate limiting validation

### Test Architecture
- **In-Memory Database**: Isolated test database for each test
- **Test Fixtures**: Reusable test data and configurations
- **Mocking**: External dependencies mocked for unit tests
- **Assertions**: FluentAssertions for readable test assertions

## 🔒 Security

### Authentication & Authorization
- JWT Bearer token validation
- Configurable token validation parameters
- Rate limiting per IP address
- CORS policy enforcement

### Data Protection
- Input sanitization and validation
- SQL injection prevention via Entity Framework
- **No secrets in source code or appsettings.json**
- **Secure secret management via Kubernetes, User Secrets, or Environment Variables**
- Connection string and JWT secrets loaded from secure sources only

### Container Security
- Non-root user execution
- Minimal attack surface
- Security-focused base images
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

### Metrics & Performance
- Response time monitoring via logging
- Cache hit/miss rate tracking
- Rate limiting metrics
- Database query performance

## 🚀 Deployment

### GitOps Pipeline
1. **Code Push** → GitHub repository
2. **CI Pipeline** → Build, test, and create container image
3. **Image Registry** → Google Artifact Registry
4. **GitOps Update** → Kustomize updates deployment manifests
5. **ArgoCD Sync** → Automatic deployment to Kubernetes

### Environments
- **Development**: Auto-deploy from `develop` branch
- **Staging**: Deploy from release tags (`release/v*`)
- **Production**: Auto-deploy from `main` branch

### Container Registry
```
asia-southeast1-docker.pkg.dev/maliev-website/maliev-website-artifact-{env}/maliev-country-service
```

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