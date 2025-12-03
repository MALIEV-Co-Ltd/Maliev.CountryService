using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Maliev.CountryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Countries",
                table: "Countries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BulkImportJobs",
                table: "BulkImportJobs");

            migrationBuilder.RenameTable(
                name: "Countries",
                newName: "countries");

            migrationBuilder.RenameTable(
                name: "BulkImportJobs",
                newName: "bulk_import_jobs");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "countries",
                newName: "version");

            migrationBuilder.RenameColumn(
                name: "Translations",
                table: "countries",
                newName: "translations");

            migrationBuilder.RenameColumn(
                name: "Timezones",
                table: "countries",
                newName: "timezones");

            migrationBuilder.RenameColumn(
                name: "Subregion",
                table: "countries",
                newName: "subregion");

            migrationBuilder.RenameColumn(
                name: "Region",
                table: "countries",
                newName: "region");

            migrationBuilder.RenameColumn(
                name: "Population",
                table: "countries",
                newName: "population");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "countries",
                newName: "name");

            migrationBuilder.RenameColumn(
                name: "Longitude",
                table: "countries",
                newName: "longitude");

            migrationBuilder.RenameColumn(
                name: "Latitude",
                table: "countries",
                newName: "latitude");

            migrationBuilder.RenameColumn(
                name: "Languages",
                table: "countries",
                newName: "languages");

            migrationBuilder.RenameColumn(
                name: "Landlocked",
                table: "countries",
                newName: "landlocked");

            migrationBuilder.RenameColumn(
                name: "Iso3",
                table: "countries",
                newName: "iso3");

            migrationBuilder.RenameColumn(
                name: "Iso2",
                table: "countries",
                newName: "iso2");

            migrationBuilder.RenameColumn(
                name: "Independent",
                table: "countries",
                newName: "independent");

            migrationBuilder.RenameColumn(
                name: "Flags",
                table: "countries",
                newName: "flags");

            migrationBuilder.RenameColumn(
                name: "Demonym",
                table: "countries",
                newName: "demonym");

            migrationBuilder.RenameColumn(
                name: "Currencies",
                table: "countries",
                newName: "currencies");

            migrationBuilder.RenameColumn(
                name: "Capital",
                table: "countries",
                newName: "capital");

            migrationBuilder.RenameColumn(
                name: "Borders",
                table: "countries",
                newName: "borders");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "countries",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                table: "countries",
                newName: "updated_by");

            migrationBuilder.RenameColumn(
                name: "UnMember",
                table: "countries",
                newName: "un_member");

            migrationBuilder.RenameColumn(
                name: "TopLevelDomains",
                table: "countries",
                newName: "top_level_domains");

            migrationBuilder.RenameColumn(
                name: "OfficialName",
                table: "countries",
                newName: "official_name");

            migrationBuilder.RenameColumn(
                name: "NumericCode",
                table: "countries",
                newName: "numeric_code");

            migrationBuilder.RenameColumn(
                name: "LastModifiedUtc",
                table: "countries",
                newName: "last_modified_utc");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "countries",
                newName: "is_active");

            migrationBuilder.RenameColumn(
                name: "GiniCoefficient",
                table: "countries",
                newName: "gini_coefficient");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                table: "countries",
                newName: "deleted_at");

            migrationBuilder.RenameColumn(
                name: "CreatedBy",
                table: "countries",
                newName: "created_by");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "countries",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "CoatOfArms",
                table: "countries",
                newName: "coat_of_arms");

            migrationBuilder.RenameColumn(
                name: "CallingCodes",
                table: "countries",
                newName: "calling_codes");

            migrationBuilder.RenameColumn(
                name: "AreaKm2",
                table: "countries",
                newName: "area_km2");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_Iso3",
                table: "countries",
                newName: "IX_countries_iso3");

            migrationBuilder.RenameIndex(
                name: "IX_Countries_Iso2",
                table: "countries",
                newName: "IX_countries_iso2");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "bulk_import_jobs",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "bulk_import_jobs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "ValidationErrors",
                table: "bulk_import_jobs",
                newName: "validation_errors");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "bulk_import_jobs",
                newName: "user_id");

            migrationBuilder.RenameColumn(
                name: "TotalRecords",
                table: "bulk_import_jobs",
                newName: "total_records");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "bulk_import_jobs",
                newName: "started_at_utc");

            migrationBuilder.RenameColumn(
                name: "ProcessedRecords",
                table: "bulk_import_jobs",
                newName: "processed_records");

            migrationBuilder.RenameColumn(
                name: "FailedRecords",
                table: "bulk_import_jobs",
                newName: "failed_records");

            migrationBuilder.RenameColumn(
                name: "ErrorMessage",
                table: "bulk_import_jobs",
                newName: "error_message");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "bulk_import_jobs",
                newName: "created_at_utc");

            migrationBuilder.RenameColumn(
                name: "CompletedAtUtc",
                table: "bulk_import_jobs",
                newName: "completed_at_utc");

            migrationBuilder.AddPrimaryKey(
                name: "pk_countries",
                table: "countries",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_bulk_import_jobs",
                table: "bulk_import_jobs",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "IX_countries_name",
                table: "countries",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "pk_countries",
                table: "countries");

            migrationBuilder.DropIndex(
                name: "IX_countries_name",
                table: "countries");

            migrationBuilder.DropPrimaryKey(
                name: "pk_bulk_import_jobs",
                table: "bulk_import_jobs");

            migrationBuilder.RenameTable(
                name: "countries",
                newName: "Countries");

            migrationBuilder.RenameTable(
                name: "bulk_import_jobs",
                newName: "BulkImportJobs");

            migrationBuilder.RenameColumn(
                name: "version",
                table: "Countries",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "translations",
                table: "Countries",
                newName: "Translations");

            migrationBuilder.RenameColumn(
                name: "timezones",
                table: "Countries",
                newName: "Timezones");

            migrationBuilder.RenameColumn(
                name: "subregion",
                table: "Countries",
                newName: "Subregion");

            migrationBuilder.RenameColumn(
                name: "region",
                table: "Countries",
                newName: "Region");

            migrationBuilder.RenameColumn(
                name: "population",
                table: "Countries",
                newName: "Population");

            migrationBuilder.RenameColumn(
                name: "name",
                table: "Countries",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "longitude",
                table: "Countries",
                newName: "Longitude");

            migrationBuilder.RenameColumn(
                name: "latitude",
                table: "Countries",
                newName: "Latitude");

            migrationBuilder.RenameColumn(
                name: "languages",
                table: "Countries",
                newName: "Languages");

            migrationBuilder.RenameColumn(
                name: "landlocked",
                table: "Countries",
                newName: "Landlocked");

            migrationBuilder.RenameColumn(
                name: "iso3",
                table: "Countries",
                newName: "Iso3");

            migrationBuilder.RenameColumn(
                name: "iso2",
                table: "Countries",
                newName: "Iso2");

            migrationBuilder.RenameColumn(
                name: "independent",
                table: "Countries",
                newName: "Independent");

            migrationBuilder.RenameColumn(
                name: "flags",
                table: "Countries",
                newName: "Flags");

            migrationBuilder.RenameColumn(
                name: "demonym",
                table: "Countries",
                newName: "Demonym");

            migrationBuilder.RenameColumn(
                name: "currencies",
                table: "Countries",
                newName: "Currencies");

            migrationBuilder.RenameColumn(
                name: "capital",
                table: "Countries",
                newName: "Capital");

            migrationBuilder.RenameColumn(
                name: "borders",
                table: "Countries",
                newName: "Borders");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Countries",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_by",
                table: "Countries",
                newName: "UpdatedBy");

            migrationBuilder.RenameColumn(
                name: "un_member",
                table: "Countries",
                newName: "UnMember");

            migrationBuilder.RenameColumn(
                name: "top_level_domains",
                table: "Countries",
                newName: "TopLevelDomains");

            migrationBuilder.RenameColumn(
                name: "official_name",
                table: "Countries",
                newName: "OfficialName");

            migrationBuilder.RenameColumn(
                name: "numeric_code",
                table: "Countries",
                newName: "NumericCode");

            migrationBuilder.RenameColumn(
                name: "last_modified_utc",
                table: "Countries",
                newName: "LastModifiedUtc");

            migrationBuilder.RenameColumn(
                name: "is_active",
                table: "Countries",
                newName: "IsActive");

            migrationBuilder.RenameColumn(
                name: "gini_coefficient",
                table: "Countries",
                newName: "GiniCoefficient");

            migrationBuilder.RenameColumn(
                name: "deleted_at",
                table: "Countries",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by",
                table: "Countries",
                newName: "CreatedBy");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "Countries",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "coat_of_arms",
                table: "Countries",
                newName: "CoatOfArms");

            migrationBuilder.RenameColumn(
                name: "calling_codes",
                table: "Countries",
                newName: "CallingCodes");

            migrationBuilder.RenameColumn(
                name: "area_km2",
                table: "Countries",
                newName: "AreaKm2");

            migrationBuilder.RenameIndex(
                name: "IX_countries_iso3",
                table: "Countries",
                newName: "IX_Countries_Iso3");

            migrationBuilder.RenameIndex(
                name: "IX_countries_iso2",
                table: "Countries",
                newName: "IX_Countries_Iso2");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "BulkImportJobs",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "BulkImportJobs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "validation_errors",
                table: "BulkImportJobs",
                newName: "ValidationErrors");

            migrationBuilder.RenameColumn(
                name: "user_id",
                table: "BulkImportJobs",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "total_records",
                table: "BulkImportJobs",
                newName: "TotalRecords");

            migrationBuilder.RenameColumn(
                name: "started_at_utc",
                table: "BulkImportJobs",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "processed_records",
                table: "BulkImportJobs",
                newName: "ProcessedRecords");

            migrationBuilder.RenameColumn(
                name: "failed_records",
                table: "BulkImportJobs",
                newName: "FailedRecords");

            migrationBuilder.RenameColumn(
                name: "error_message",
                table: "BulkImportJobs",
                newName: "ErrorMessage");

            migrationBuilder.RenameColumn(
                name: "created_at_utc",
                table: "BulkImportJobs",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "completed_at_utc",
                table: "BulkImportJobs",
                newName: "CompletedAtUtc");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Countries",
                table: "Countries",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BulkImportJobs",
                table: "BulkImportJobs",
                column: "Id");
        }
    }
}
