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
    /// Seeds the countries table from the bundled C# seed data if the table is empty.
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

            logger.LogInformation("Seeding countries from C# seed data...");
            var countries = CountrySeedData.GetAll().ToList();

            var strategy = context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await context.Countries.AddRangeAsync(countries);
                await context.SaveChangesAsync();
                logger.LogInformation("Successfully seeded {Count} countries.", countries.Count);
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
}
