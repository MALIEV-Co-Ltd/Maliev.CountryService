using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAuditAndImportEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs_countries_country_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "IX_countries_name",
                table: "countries");

            migrationBuilder.RenameIndex(
                name: "IX_countries_iso3",
                table: "countries",
                newName: "ix_countries_iso3");

            migrationBuilder.RenameIndex(
                name: "IX_countries_iso2",
                table: "countries",
                newName: "ix_countries_iso2");

            migrationBuilder.AddColumn<Guid>(
                name: "correlation_id",
                table: "bulk_import_jobs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "error_message",
                table: "bulk_import_jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ip_address",
                table: "bulk_import_jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "bulk_import_jobs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_id",
                table: "bulk_import_jobs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "after_snapshot",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "before_snapshot",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "changed_fields",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "correlation_id",
                table: "audit_logs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at_utc",
                table: "audit_logs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "operation",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "user_agent",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "audit_logs",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "user_roles",
                table: "audit_logs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_countries_name",
                table: "countries",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs__countries_country_id",
                table: "audit_logs",
                column: "country_id",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_audit_logs__countries_country_id",
                table: "audit_logs");

            migrationBuilder.DropIndex(
                name: "ix_countries_name",
                table: "countries");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "error_message",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "ip_address",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "user_id",
                table: "bulk_import_jobs");

            migrationBuilder.DropColumn(
                name: "after_snapshot",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "before_snapshot",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "changed_fields",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "created_at_utc",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "operation",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_agent",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "audit_logs");

            migrationBuilder.DropColumn(
                name: "user_roles",
                table: "audit_logs");

            migrationBuilder.RenameIndex(
                name: "ix_countries_iso3",
                table: "countries",
                newName: "IX_countries_iso3");

            migrationBuilder.RenameIndex(
                name: "ix_countries_iso2",
                table: "countries",
                newName: "IX_countries_iso2");

            migrationBuilder.CreateIndex(
                name: "IX_countries_name",
                table: "countries",
                column: "name");

            migrationBuilder.AddForeignKey(
                name: "fk_audit_logs_countries_country_id",
                table: "audit_logs",
                column: "country_id",
                principalTable: "countries",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,");
        }
    }
}
