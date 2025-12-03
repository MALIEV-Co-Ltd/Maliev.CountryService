FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY nuget.config ./
COPY ["Maliev.CountryService.Api/Maliev.CountryService.Api.csproj", "Maliev.CountryService.Api/"]
COPY ["Maliev.CountryService.Data/Maliev.CountryService.Data.csproj", "Maliev.CountryService.Data/"]
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.CountryService.Api/Maliev.CountryService.Api.csproj"
COPY . .
WORKDIR "/src/Maliev.CountryService.Api"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN chown -R app:app /app
USER app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
HEALTHCHECK CMD curl -f http://localhost:8080/countries/liveness || exit 1
ENTRYPOINT ["dotnet", "Maliev.CountryService.Api.dll"]