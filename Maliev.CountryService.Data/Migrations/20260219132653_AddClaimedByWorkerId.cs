using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimedByWorkerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "claimed_by_worker_id",
                table: "bulk_import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_claimed_by_worker_id",
                table: "bulk_import_jobs",
                column: "claimed_by_worker_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_bulk_import_jobs_claimed_by_worker_id",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "claimed_by_worker_id",
                table: "bulk_import_jobs");
        }
    }
}
