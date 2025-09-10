using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Data.DbContexts;

public class CountryDbContext : DbContext
{
    public CountryDbContext(DbContextOptions<CountryDbContext> options) : base(options)
    {
    }

    public DbSet<Country> Countries { get; set; } = null!;
    public DbSet<CountryCode> CountryCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Country>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);
                
            entity.Property(e => e.Continent)
                .IsRequired()
                .HasMaxLength(50);
                
            entity.Property(e => e.ISO2)
                .IsRequired()
                .HasMaxLength(2);
                
            entity.Property(e => e.ISO3)
                .IsRequired()
                .HasMaxLength(3);
                
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.ModifiedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.ISO2).IsUnique();
            entity.HasIndex(e => e.ISO3).IsUnique();
            entity.HasIndex(e => e.Continent);

            // Relationships
            entity.HasMany(e => e.CountryCodes)
                .WithOne(cc => cc.Country)
                .HasForeignKey(cc => cc.CountryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CountryCode>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20);
                
            entity.Property(e => e.IsPrimary)
                .IsRequired()
                .HasDefaultValue(false);
                
            entity.Property(e => e.CreatedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
                
            entity.Property(e => e.ModifiedDate)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes - Allow duplicate country codes (US/Canada share '1', Kazakhstan/Russia share '7')
            entity.HasIndex(e => e.Code);
            entity.HasIndex(e => e.CountryId);
            entity.HasIndex(e => new { e.CountryId, e.IsPrimary });
        });
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var countryEntries = ChangeTracker.Entries<Country>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in countryEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            entry.Entity.ModifiedDate = DateTime.UtcNow;
        }

        var countryCodeEntries = ChangeTracker.Entries<CountryCode>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

        foreach (var entry in countryCodeEntries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = DateTime.UtcNow;
            }
            entry.Entity.ModifiedDate = DateTime.UtcNow;
        }
    }
}