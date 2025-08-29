namespace Maliev.CountryService.Data.Data
{
    using Maliev.CountryService.Data.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata;

    /// <summary>
    /// Represents the database context for country-related data.
    /// </summary>
    public partial class CountryContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CountryContext"/> class.
        /// </summary>
        /// <param name="options">The options for this context.</param>
        public CountryContext(DbContextOptions<CountryContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the DbSet for the Country entity.
        /// </summary>
        public virtual DbSet<Country> Country { get; set; }

        /// <summary>
        /// Configures the model that was discovered by convention from the entity types exposed in <see cref="DbSet{TEntity}" /> properties on the derived context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Country>(entity =>
            {
                entity.Property(e => e.Id).HasColumnName("ID");

                entity.Property(e => e.Continent).HasMaxLength(50);

                entity.Property(e => e.CountryCode).HasMaxLength(30);

                entity.Property(e => e.CreatedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Iso2)
                    .HasColumnName("ISO2")
                    .HasMaxLength(2);

                entity.Property(e => e.Iso3)
                    .HasColumnName("ISO3")
                    .HasMaxLength(3);

                entity.Property(e => e.ModifiedDate)
                    .HasColumnType("datetime")
                    .HasDefaultValueSql("(getutcdate())");

                entity.Property(e => e.Name)
                    .IsRequired()
                    .HasMaxLength(50);
            });
        }
    }
}
