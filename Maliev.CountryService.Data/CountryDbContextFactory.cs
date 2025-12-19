using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.CountryService.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// Uses environment variable for connection string.
/// </summary>
public class CountryDbContextFactory : IDesignTimeDbContextFactory<CountryDbContext>
{
    public CountryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CountryDbContext>();

        // Read from environment variable for migration commands
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CountryDbContext")
            ?? "Server=localhost;Port=5432;Database=country_app_db;User Id=postgres;Password=postgres;";

        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "public");
        });

        return new CountryDbContext(optionsBuilder.Options);
    }
}
