using Maliev.CountryService.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.CountryService.Data
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CountryDbContext>
    {
        public CountryDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<CountryDbContext>();

            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__CountryDbContext");

            optionsBuilder.UseNpgsql(connectionString);

            return new CountryDbContext(optionsBuilder.Options);
        }
    }
}