# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["Maliev.CountryService.Api/Maliev.CountryService.Api.csproj", "Maliev.CountryService.Api/"]
COPY ["Maliev.CountryService.Data/Maliev.CountryService.Data.csproj", "Maliev.CountryService.Data/"]
RUN dotnet restore "Maliev.CountryService.Api/Maliev.CountryService.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/Maliev.CountryService.Api"
RUN dotnet build "Maliev.CountryService.Api.csproj" -c Release -o /app/build

# Publish stage  
FROM build AS publish
RUN dotnet publish "Maliev.CountryService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" --uid 10001 appuser

# Create logs directory and set permissions
RUN mkdir -p /app/logs && chown -R appuser:appuser /app

# Copy published app
COPY --from=publish /app/publish .

# Switch to non-root user
USER appuser

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/countries/liveness || exit 1

# Expose port
EXPOSE 8080

ENTRYPOINT ["dotnet", "Maliev.CountryService.Api.dll"]