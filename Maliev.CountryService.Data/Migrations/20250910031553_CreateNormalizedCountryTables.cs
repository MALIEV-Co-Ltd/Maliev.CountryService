using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class CreateNormalizedCountryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Countries_CountryCode",
                table: "Countries");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                table: "Countries",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.CreateTable(
                name: "CountryCodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CountryCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CountryCodes_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_Code",
                table: "CountryCodes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_CountryId",
                table: "CountryCodes",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_CountryId_IsPrimary",
                table: "CountryCodes",
                columns: new[] { "CountryId", "IsPrimary" });

            // Migrate existing country codes from Countries.CountryCode to CountryCodes table
            // This handles comma-separated codes like "1-809, 1-829, 1-849" for Dominican Republic
            migrationBuilder.Sql(@"
                INSERT INTO ""CountryCodes"" (""CountryId"", ""Code"", ""IsPrimary"", ""CreatedDate"", ""ModifiedDate"")
                SELECT 
                    c.""Id"" as ""CountryId"",
                    TRIM(code_part.code_value) as ""Code"",
                    (code_part.code_rank = 1) as ""IsPrimary"",
                    CURRENT_TIMESTAMP as ""CreatedDate"",
                    CURRENT_TIMESTAMP as ""ModifiedDate""
                FROM ""Countries"" c
                CROSS JOIN LATERAL (
                    SELECT 
                        unnest(string_to_array(c.""CountryCode"", ',')) as code_value,
                        ROW_NUMBER() OVER () as code_rank
                ) code_part
                WHERE c.""CountryCode"" IS NOT NULL 
                  AND c.""CountryCode"" != ''
                  AND TRIM(code_part.code_value) != '';
            ");

            // Remove the CountryCode column from Countries table since we now have normalized structure
            migrationBuilder.DropColumn(
                name: "CountryCode", 
                table: "Countries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add CountryCode column back to Countries table
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                table: "Countries",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            // Restore country codes by joining all codes for each country (comma-separated)
            migrationBuilder.Sql(@"
                UPDATE ""Countries"" 
                SET ""CountryCode"" = subquery.""AllCodes""
                FROM (
                    SELECT 
                        ""CountryId"",
                        STRING_AGG(""Code"", ', ' ORDER BY ""IsPrimary"" DESC, ""Code"") as ""AllCodes""
                    FROM ""CountryCodes""
                    GROUP BY ""CountryId""
                ) subquery
                WHERE ""Countries"".""Id"" = subquery.""CountryId"";
            ");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_CountryCode",
                table: "Countries",
                column: "CountryCode",
                unique: true);

            migrationBuilder.DropTable(
                name: "CountryCodes");
        }
    }
}
