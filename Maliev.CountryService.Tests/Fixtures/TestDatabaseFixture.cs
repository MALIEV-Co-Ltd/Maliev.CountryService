using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Maliev.CountryService.Data;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using Testcontainers.RabbitMq;

namespace Maliev.CountryService.Tests.Fixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    public IContainer? PostgresContainer { get; private set; }
    public IContainer? RedisContainer { get; private set; }
    public IContainer? RabbitMqContainer { get; private set; }

    public string ConnectionString { get; private set; } = string.Empty;
    public string RedisConnectionString { get; private set; } = string.Empty;
    public string RabbitMqConnectionString { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        PostgresContainer = new PostgreSqlBuilder().WithName("postgres:18-alpine")
            .WithDatabase("test_db")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        RedisContainer = new RedisBuilder().WithName("redis:8.4-alpine")
            .Build();

        RabbitMqContainer = new RabbitMqBuilder().WithName("rabbitmq:4.2-alpine")
            .Build();

        // Start all containers in parallel
        await Task.WhenAll(
            PostgresContainer.StartAsync(),
            RedisContainer.StartAsync(),
            RabbitMqContainer.StartAsync()
        );

        ConnectionString = ((PostgreSqlContainer)PostgresContainer).GetConnectionString();
        RedisConnectionString = ((RedisContainer)RedisContainer).GetConnectionString();
        RabbitMqConnectionString = ((RabbitMqContainer)RabbitMqContainer).GetConnectionString();

        // Wait for Redis to be ready
        using (var connection = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(RedisConnectionString))
        {
            await connection.GetDatabase().PingAsync();
        }

        var options = new DbContextOptionsBuilder<CountryDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var context = new CountryDbContext(options);
        await context.Database.EnsureDeletedAsync(); // Ensure clean database
        await context.Database.EnsureCreatedAsync(); // Create schema from model

        // Seed initial data for tests
        var countries = new List<Maliev.CountryService.Data.Entities.Country>
        {
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "US", Iso3 = "USA", Name = "United States", OfficialName = "United States of America",
                Region = "Americas", Subregion = "North America", Population = 330000000, AreaKm2 = 9833520,
                Timezones = "[\"-12:00\",\"-11:00\",\"-10:00\",\"-09:00\",\"-08:00\",\"-07:00\",\"-06:00\",\"-05:00\",\"-04:00\",\"+10:00\",\"+12:00\"]",
                Borders = "[\"CAN\",\"MEX\"]",
                CallingCodes = "[\"1\"]",
                TopLevelDomains = "[\".us\"]",
                Currencies = "{\"USD\":{\"name\":\"United States dollar\",\"symbol\":\"$\"}}",
                Languages = "{\"eng\":\"English\"}",
                Translations = "{\"ara\":\"الولايات المتحدة\",\"ces\":\"Spojené státy\",\"cym\":\"Unol Daleithiau\",\"deu\":\"Vereinigte Staaten\",\"est\":\"Ühendriigid\",\"fin\":\"Yhdysvallat\",\"fra\":\"États-Unis\",\"hrv\":\"Sjedinjene Američke Države\",\"hun\":\"Egyesült Államok\",\"ita\":\"Stati Uniti d'America\",\"jpn\":\"アメリカ合衆国\",\"kor\":\"미국\",\"nld\":\"Verenigde Staten\",\"per\":\"ایالات متحده آمریکا\",\"pol\":\"Stany Zjednoczone\",\"por\":\"Estados Unidos\",\"rus\":\"Соединённые Штаты Америки\",\"slk\":\"Spojené štáty\",\"spa\":\"Estados Unidos\",\"swe\":\"Förenta staterna\",\"urd\":\"ریاستہائے متحدہ\",\"zho\":\"美国\"}",
                Flags = "{\"png\":\"https://flagcdn.com/w320/us.png\",\"svg\":\"https://flagcdn.com/us.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "CA", Iso3 = "CAN", Name = "Canada", OfficialName = "Canada",
                Region = "Americas", Subregion = "North America", Population = 38000000, AreaKm2 = 9984670,
                Timezones = "[\"-08:00\",\"-07:00\",\"-06:00\",\"-05:00\",\"-04:00\",\"-03:30\"]",
                Borders = "[\"USA\"]",
                CallingCodes = "[\"1\"]",
                TopLevelDomains = "[\".ca\"]",
                Currencies = "{\"CAD\":{\"name\":\"Canadian dollar\",\"symbol\":\"$\"}}",
                Languages = "{\"eng\":\"English\",\"fra\":\"French\"}",
                Translations = "{\"ara\":\"كندا\",\"ces\":\"Kanada\",\"cym\":\"Canada\",\"deu\":\"Kanada\",\"est\":\"Kanada\",\"fin\":\"Kanada\",\"fra\":\"Canada\",\"hrv\":\"Kanada\",\"hun\":\"Kanada\",\"ita\":\"Canada\",\"jpn\":\"カナダ\",\"kor\":\"캐나다\",\"nld\":\"Canada\",\"per\":\"کانادا\",\"pol\":\"Kanada\",\"por\":\"Canadá\",\"rus\":\"Канада\",\"slk\":\"Kanada\",\"spa\":\"Canadá\",\"swe\":\"Kanada\",\"urd\":\"کینیڈا\",\"zho\":\"加拿大\"}",
                Flags = "{\"png\":\"https://flagcdn.com/w320/ca.png\",\"svg\":\"https://flagcdn.com/ca.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "GB", Iso3 = "GBR", Name = "United Kingdom", OfficialName = "United Kingdom of Great Britain and Northern Ireland",
                Region = "Europe", Subregion = "Northern Europe", Population = 67000000, AreaKm2 = 242900,
                Timezones = "[\"Europe/London\"]",
                Borders = "[\"IRL\"]",
                CallingCodes = "[\"44\"]",
                TopLevelDomains = "[\".uk\"]",
                Currencies = "{\"GBP\":{\"name\":\"British pound\",\"symbol\":\"£\"}}",
                Languages = "{\"eng\":\"English\"}",
                Translations = "{\"ara\":\"المملكة المتحدة\",\"ces\":\"Spojené království\",\"cym\":\"Teyrnas Unedig\",\"deu\":\"Vereinigtes Königreich\",\"est\":\"Ühendkuningriik\",\"fin\":\"Yhdistynyt kuningaskunta\",\"fra\":\"Royaume-Uni\",\"hrv\":\"Ujedinjeno Kraljevstvo\",\"hun\":\"Egyesült Királyság\",\"ita\":\"Regno Unito\",\"jpn\":\"イギリス\",\"kor\":\"영국\",\"nld\":\"Verenigd Koninkrijk\",\"per\":\"بریتانیا\",\"pol\":\"Zjednoczone Królestwo\",\"por\":\"Reino Unido\",\"rus\":\"Великобритания\",\"slk\":\"Spojené kráľovstvo\",\"spa\":\"Reino Unido\",\"swe\":\"Storbritannien\",\"urd\":\"برطانیہ\",\"zho\":\"英国\"}",
                Flags = "{\"png\":\"https://flagcdn.com/w320/gb.png\",\"svg\":\"https://flagcdn.com/gb.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "FR", Iso3 = "FRA", Name = "France", OfficialName = "French Republic",
                Region = "Europe", Subregion = "Western Europe", Population = 67000000, AreaKm2 = 551695,
                Timezones = "[\"Europe/Paris\"]", Borders = "[\"BEL\",\"DEU\",\"ESP\"]",
                CallingCodes = "[\"33\"]", TopLevelDomains = "[\".fr\"]",
                Currencies = "{\"EUR\":{\"name\":\"Euro\",\"symbol\":\"€\"}}",
                Languages = "{\"fra\":\"French\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/fr.png\",\"svg\":\"https://flagcdn.com/fr.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "DE", Iso3 = "DEU", Name = "Germany", OfficialName = "Federal Republic of Germany",
                Region = "Europe", Subregion = "Western Europe", Population = 83000000, AreaKm2 = 357022,
                Timezones = "[\"Europe/Berlin\"]", Borders = "[\"FRA\",\"POL\"]",
                CallingCodes = "[\"49\"]", TopLevelDomains = "[\".de\"]",
                Currencies = "{\"EUR\":{\"name\":\"Euro\",\"symbol\":\"€\"}}",
                Languages = "{\"deu\":\"German\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/de.png\",\"svg\":\"https://flagcdn.com/de.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "JP", Iso3 = "JPN", Name = "Japan", OfficialName = "Japan",
                Region = "Asia", Subregion = "Eastern Asia", Population = 125000000, AreaKm2 = 377975,
                Timezones = "[\"Asia/Tokyo\"]", Borders = "[]",
                CallingCodes = "[\"81\"]", TopLevelDomains = "[\".jp\"]",
                Currencies = "{\"JPY\":{\"name\":\"Japanese yen\",\"symbol\":\"¥\"}}",
                Languages = "{\"jpn\":\"Japanese\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/jp.png\",\"svg\":\"https://flagcdn.com/jp.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "AU", Iso3 = "AUS", Name = "Australia", OfficialName = "Commonwealth of Australia",
                Region = "Oceania", Subregion = "Australia and New Zealand", Population = 25000000, AreaKm2 = 7692024,
                Timezones = "[\"Australia/Sydney\"]", Borders = "[]",
                CallingCodes = "[\"61\"]", TopLevelDomains = "[\".au\"]",
                Currencies = "{\"AUD\":{\"name\":\"Australian dollar\",\"symbol\":\"$\"}}",
                Languages = "{\"eng\":\"English\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/au.png\",\"svg\":\"https://flagcdn.com/au.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "BR", Iso3 = "BRA", Name = "Brazil", OfficialName = "Federative Republic of Brazil",
                Region = "Americas", Subregion = "South America", Population = 212000000, AreaKm2 = 8515767,
                Timezones = "[\"America/Sao_Paulo\"]", Borders = "[\"ARG\"]",
                CallingCodes = "[\"55\"]", TopLevelDomains = "[\".br\"]",
                Currencies = "{\"BRL\":{\"name\":\"Brazilian real\",\"symbol\":\"R$\"}}",
                Languages = "{\"por\":\"Portuguese\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/br.png\",\"svg\":\"https://flagcdn.com/br.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "IN", Iso3 = "IND", Name = "India", OfficialName = "Republic of India",
                Region = "Asia", Subregion = "Southern Asia", Population = 1380000000, AreaKm2 = 3287590,
                Timezones = "[\"Asia/Kolkata\"]", Borders = "[\"PAK\"]",
                CallingCodes = "[\"91\"]", TopLevelDomains = "[\".in\"]",
                Currencies = "{\"INR\":{\"name\":\"Indian rupee\",\"symbol\":\"₹\"}}",
                Languages = "{\"hin\":\"Hindi\",\"eng\":\"English\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/in.png\",\"svg\":\"https://flagcdn.com/in.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            },
            new Maliev.CountryService.Data.Entities.Country
            {
                Iso2 = "MX", Iso3 = "MEX", Name = "Mexico", OfficialName = "United Mexican States",
                Region = "Americas", Subregion = "Central America", Population = 128000000, AreaKm2 = 1964375,
                Timezones = "[\"America/Mexico_City\"]", Borders = "[\"USA\"]",
                CallingCodes = "[\"52\"]", TopLevelDomains = "[\".mx\"]",
                Currencies = "{\"MXN\":{\"name\":\"Mexican peso\",\"symbol\":\"$\"}}",
                Languages = "{\"spa\":\"Spanish\"}",
                Translations = "{}", Flags = "{\"png\":\"https://flagcdn.com/w320/mx.png\",\"svg\":\"https://flagcdn.com/mx.svg\"}",
                Independent = true, UnMember = true, Landlocked = false, IsActive = true, CreatedBy = "test", UpdatedBy = "test", Version = Guid.NewGuid()
            }
        };
        context.Countries.AddRange(countries);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Cleans all data from the database while preserving schema
    /// </summary>
    public async Task CleanDatabaseAsync()
    {
        using var context = new CountryDbContext(new DbContextOptionsBuilder<CountryDbContext>().UseNpgsql(ConnectionString).Options);

        var tableNames = context.Model.GetEntityTypes()
            .Select(t => t.GetTableName())
            .Where(t => t != null)
            .Cast<string>()
            .ToList();

        foreach (var tableName in tableNames)
        {
            try
            {
#pragma warning disable EF1002, EF1003
                await context.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE \"{tableName}\" RESTART IDENTITY CASCADE");
#pragma warning restore EF1002, EF1003
            }
            catch (Npgsql.PostgresException ex) when (ex.SqlState == "42P01")
            {
            }
        }
    }

    public async Task DisposeAsync()
    {
        if (PostgresContainer != null)
        {
            await PostgresContainer.DisposeAsync();
        }

        if (RedisContainer != null)
        {
            await RedisContainer.DisposeAsync();
        }

        if (RabbitMqContainer != null)
        {
            await RabbitMqContainer.DisposeAsync();
        }
    }
}
