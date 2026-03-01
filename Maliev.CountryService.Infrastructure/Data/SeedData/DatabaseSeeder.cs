using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maliev.CountryService.Infrastructure.Data.SeedData;

/// <summary>
/// Handles initial database seeding for country data.
/// </summary>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the countries table from the bundled SQL file if the table is empty.
    /// </summary>
    /// <param name="host">The application host to resolve services from.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static async Task SeedCountriesAsync(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        try
        {
            if (await context.Countries.AnyAsync())
            {
                logger.LogInformation("Countries table already has data. Skipping seed.");
                return;
            }

            var seedFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "countries_seed.sql");
            if (!File.Exists(seedFilePath))
            {
                logger.LogWarning("Seed file not found at {Path}. Skipping seed.", seedFilePath);
                return;
            }

            logger.LogInformation("Seeding countries from {Path}...", seedFilePath);
            var sql = await File.ReadAllTextAsync(seedFilePath);

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await context.Database.BeginTransactionAsync();
                try
                {
                    // Use ADO.NET directly to avoid EF Core's string.Format parsing of curly braces in JSONB
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        await connection.OpenAsync();
                    }

                    using var command = connection.CreateCommand();
                    command.CommandText = sql;
                    command.Transaction = transaction.GetDbTransaction();
                    await command.ExecuteNonQueryAsync();

                    await transaction.CommitAsync();
                    logger.LogInformation("Successfully seeded countries.");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    logger.LogError(ex, "Failed to execute seed SQL.");
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
