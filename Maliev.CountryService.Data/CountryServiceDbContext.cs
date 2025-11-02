using Microsoft.EntityFrameworkCore;
using Maliev.CountryService.Data.Models;
using Maliev.CountryService.Data.Configurations;

namespace Maliev.CountryService.Data;

public class CountryServiceDbContext : DbContext
{
    public CountryServiceDbContext(DbContextOptions<CountryServiceDbContext> options)
        : base(options)
    {
    }

    public DbSet<Country> Countries => Set<Country>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<BulkImportJob> BulkImportJobs => Set<BulkImportJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CountryConfiguration());
        modelBuilder.ApplyConfiguration(new AuditLogConfiguration());
        modelBuilder.ApplyConfiguration(new BulkImportJobConfiguration());

        base.OnModelCreating(modelBuilder);
    }
}
