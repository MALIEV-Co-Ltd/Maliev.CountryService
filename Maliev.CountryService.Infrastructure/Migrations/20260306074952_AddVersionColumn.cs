using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "version",
                table: "countries",
                type: "bigint",
                nullable: false,
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "version",
                table: "countries");
        }
    }
}
