# Multi-stage Dockerfile for Maliev Country Service
# Build from repository root: docker build -t maliev-country-service .

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["Maliev.CountryService.Api/Maliev.CountryService.Api.csproj", "Maliev.CountryService.Api/"]
COPY ["Maliev.CountryService.Data/Maliev.CountryService.Data.csproj", "Maliev.CountryService.Data/"]
RUN dotnet restore "Maliev.CountryService.Api/Maliev.CountryService.Api.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Maliev.CountryService.Api"
RUN dotnet build "Maliev.CountryService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Maliev.CountryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user (security best practice)
RUN adduser --disabled-password --gecos "" --uid 10001 appuser && \
    mkdir -p /app/logs && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Expose application port
EXPOSE 8080

# Health check using liveness endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/countries/v1/liveness || exit 1

# Set environment to production
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "Maliev.CountryService.Api.dll"]
