using Maliev.CountryService.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.CountryService.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CountryDbContext>
{
    public CountryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CountryDbContext>();
        
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("TEMP_MIGRATION_CONNECTION_STRING")
            ?? throw new InvalidOperationException("Connection string must be provided via ConnectionStrings__Default or TEMP_MIGRATION_CONNECTION_STRING environment variable");

        optionsBuilder.UseNpgsql(connectionString);

        return new CountryDbContext(optionsBuilder.Options);
    }
}