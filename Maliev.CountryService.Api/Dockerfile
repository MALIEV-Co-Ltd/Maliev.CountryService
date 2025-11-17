# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
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
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Create logs directory and ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN mkdir -p /app/logs && chown -R app:app /app

# Switch to non-root user
USER app

# Copy published app (now owned by app)
COPY --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/countries/liveness || exit 1

ENTRYPOINT ["dotnet", "Maliev.CountryService.Api.dll"]