using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "bulk_import_jobs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    total_records = table.Column<int>(type: "integer", nullable: false),
                    processed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed_records = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    validation_errors = table.Column<string>(type: "jsonb", nullable: true, defaultValue: "[]"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    started_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    claimed_by_worker_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    iso2 = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    iso3 = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    official_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    numeric_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    capital = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    region = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    subregion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    latitude = table.Column<double>(type: "double precision", precision: 10, scale: 8, nullable: true),
                    longitude = table.Column<double>(type: "double precision", precision: 11, scale: 8, nullable: true),
                    demonym = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    area_km2 = table.Column<double>(type: "double precision", precision: 15, scale: 2, nullable: true),
                    population = table.Column<long>(type: "bigint", nullable: true),
                    gini_coefficient = table.Column<double>(type: "double precision", precision: 4, scale: 2, nullable: true),
                    timezones = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    borders = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    calling_codes = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    top_level_domains = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    currencies = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    languages = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    translations = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    flags = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    coat_of_arms = table.Column<string>(type: "jsonb", nullable: true),
                    independent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    un_member = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    landlocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_modified_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
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
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    country_id = table.Column<Guid>(type: "uuid", nullable: true),
                    operation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    action = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    user_roles = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    changes = table.Column<string>(type: "text", nullable: true),
                    before_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    after_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    changed_fields = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_logs_countries_country_id",
                        column: x => x.country_id,
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_correlation_id",
                table: "audit_logs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_country_id",
                table: "audit_logs",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_created_at_utc",
                table: "audit_logs",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_audit_logs_user_id",
                table: "audit_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_claimed_by_worker_id",
                table: "bulk_import_jobs",
                column: "claimed_by_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_correlation_id",
                table: "bulk_import_jobs",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_created_at_utc",
                table: "bulk_import_jobs",
                column: "created_at_utc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_status",
                table: "bulk_import_jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_bulk_import_jobs_user_id",
                table: "bulk_import_jobs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_countries_is_active",
                table: "countries",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_countries_name_gin",
                table: "countries",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_countries_region",
                table: "countries",
                column: "region");

            migrationBuilder.CreateIndex(
                name: "uq_countries_iso2",
                table: "countries",
                column: "iso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_countries_iso3",
                table: "countries",
                column: "iso3",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "bulk_import_jobs");

            migrationBuilder.DropTable(
                name: "countries");
        }
    }
}
