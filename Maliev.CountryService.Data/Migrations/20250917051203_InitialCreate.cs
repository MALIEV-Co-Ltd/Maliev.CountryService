using System;
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
                name: "Countries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Continent = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ISO2 = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    ISO3 = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Countries", x => x.Id);
                });

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
                name: "IX_Countries_Continent",
                table: "Countries",
                column: "Continent");

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ISO2",
                table: "Countries",
                column: "ISO2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_ISO3",
                table: "Countries",
                column: "ISO3",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Countries_Name",
                table: "Countries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_Code",
                table: "CountryCodes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_CountryId",
                table: "CountryCodes",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_CountryCodes_CountryId_IsPrimary",
                table: "CountryCodes",
                columns: new[] { "CountryId", "IsPrimary" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CountryCodes");

            migrationBuilder.DropTable(
                name: "Countries");
        }
    }
}
