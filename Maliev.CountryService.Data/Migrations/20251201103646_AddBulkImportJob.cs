using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkImportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BulkImportJobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TotalRecords = table.Column<int>(type: "integer", nullable: false),
                    ProcessedRecords = table.Column<int>(type: "integer", nullable: false),
                    FailedRecords = table.Column<int>(type: "integer", nullable: false),
                    ValidationErrors = table.Column<string>(type: "jsonb", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BulkImportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Iso2 = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    Iso3 = table.Column<string>(type: "character(3)", fixedLength: true, maxLength: 3, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OfficialName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    NumericCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    Capital = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Subregion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Demonym = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    AreaKm2 = table.Column<double>(type: "double precision", nullable: true),
                    Population = table.Column<long>(type: "bigint", nullable: true),
                    GiniCoefficient = table.Column<double>(type: "double precision", nullable: true),
                    Timezones = table.Column<string>(type: "jsonb", nullable: false),
                    Borders = table.Column<string>(type: "jsonb", nullable: false),
                    CallingCodes = table.Column<string>(type: "jsonb", nullable: false),
                    TopLevelDomains = table.Column<string>(type: "jsonb", nullable: false),
                    Currencies = table.Column<string>(type: "jsonb", nullable: false),
                    Languages = table.Column<string>(type: "jsonb", nullable: false),
                    Translations = table.Column<string>(type: "jsonb", nullable: false),
                    Flags = table.Column<string>(type: "jsonb", nullable: false),
                    CoatOfArms = table.Column<string>(type: "jsonb", nullable: true),
                    Independent = table.Column<bool>(type: "boolean", nullable: false),
                    UnMember = table.Column<bool>(type: "boolean", nullable: false),
                    Landlocked = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastModifiedUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso2",
                table: "Countries",
                column: "Iso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Iso3",
                table: "Countries",
                column: "Iso3",
                unique: true,
                filter: "iso3 IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BulkImportJobs");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
