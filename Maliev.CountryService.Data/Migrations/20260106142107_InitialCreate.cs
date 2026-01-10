using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "bulk_import_jobs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    processed_records = table.Column<int>(type: "integer", nullable: false),
                    failed_records = table.Column<int>(type: "integer", nullable: false),
                    validation_errors = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payload_data = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bulk_import_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    iso2 = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    iso3 = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    official_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numeric_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    capital = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subregion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    demonym = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    area_km2 = table.Column<double>(type: "double precision", nullable: true),
                    population = table.Column<long>(type: "bigint", nullable: true),
                    gini_coefficient = table.Column<double>(type: "double precision", nullable: true),
                    timezones = table.Column<string>(type: "jsonb", nullable: false),
                    borders = table.Column<string>(type: "jsonb", nullable: false),
                    calling_codes = table.Column<string>(type: "jsonb", nullable: false),
                    top_level_domains = table.Column<string>(type: "jsonb", nullable: false),
                    currencies = table.Column<string>(type: "jsonb", nullable: false),
                    languages = table.Column<string>(type: "jsonb", nullable: false),
                    translations = table.Column<string>(type: "jsonb", nullable: false),
                    flags = table.Column<string>(type: "jsonb", nullable: false),
                    coat_of_arms = table.Column<string>(type: "jsonb", nullable: true),
                    independent = table.Column<bool>(type: "boolean", nullable: false),
                    un_member = table.Column<bool>(type: "boolean", nullable: false),
                    landlocked = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<Guid>(type: "uuid", nullable: false),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    country_id = table.Column<long>(type: "bigint", nullable: true),
                    action = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    changes = table.Column<string>(type: "text", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_country_id",
                table: "audit_logs",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "IX_countries_iso2",
                table: "countries",
                column: "iso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_countries_iso3",
                table: "countries",
                column: "iso3",
                unique: true,
                filter: "iso3 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_countries_name",
                table: "countries",
                column: "name");


            // Seed all 197 production countries from external SQL file
            var seedFilePath = Path.Combine(AppContext.BaseDirectory, "SeedData", "countries_seed.sql");
            if (!File.Exists(seedFilePath))
            {
                throw new FileNotFoundException($"Seed data file not found: {seedFilePath}");
            }

            var seedSql = File.ReadAllText(seedFilePath);
            migrationBuilder.Sql($@"
SET session_replication_role = replica;
{seedSql}
SET session_replication_role = DEFAULT;");
        }
    }
}
