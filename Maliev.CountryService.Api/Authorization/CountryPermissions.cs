namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Defines the granular permissions for the Country Service.
/// Note: Constants include "Permission:" prefix for integration with ServiceDefaults policy provider.
/// </summary>
public static class CountryPermissions
{
    /// <summary>Read country details (public access)</summary>
    public const string CountriesRead = "Permission:country.countries.read";
    /// <summary>List all countries (public access)</summary>
    public const string CountriesList = "Permission:country.countries.list";
    /// <summary>Search countries (public access)</summary>
    public const string CountriesSearch = "Permission:country.countries.search";

    /// <summary>Create new countries</summary>
    public const string CountriesCreate = "Permission:country.countries.create";
    /// <summary>Update country information</summary>
    public const string CountriesUpdate = "Permission:country.countries.update";
    /// <summary>Soft delete countries</summary>
    public const string CountriesDelete = "Permission:country.countries.delete";
    /// <summary>Permanently delete countries (critical)</summary>
    public const string CountriesHardDelete = "Permission:country.countries.hard-delete";
    /// <summary>Restore soft-deleted countries</summary>
    public const string CountriesRestore = "Permission:country.countries.restore";

    /// <summary>Upload bulk import file</summary>
    public const string ImportUpload = "Permission:country.import.upload";
    /// <summary>Execute bulk import</summary>
    public const string ImportExecute = "Permission:country.import.execute";
    /// <summary>View import job status</summary>
    public const string ImportStatus = "Permission:country.import.status";
    /// <summary>Cancel running import jobs</summary>
    public const string ImportCancel = "Permission:country.import.cancel";
    /// <summary>View import history</summary>
    public const string ImportHistory = "Permission:country.import.history";

    /// <summary>Rebuild country cache</summary>
    public const string SystemRebuildCache = "Permission:country.system.rebuild-cache";
    /// <summary>Export all country data</summary>
    public const string SystemExport = "Permission:country.system.export";
    /// <summary>View service statistics</summary>
    public const string SystemViewStats = "Permission:country.system.view-stats";

    /// <summary>All available permissions</summary>
    public static readonly string[] All =
    [
        CountriesRead, CountriesList, CountriesSearch,
        CountriesCreate, CountriesUpdate, CountriesDelete, CountriesHardDelete, CountriesRestore,
        ImportUpload, ImportExecute, ImportStatus, ImportCancel, ImportHistory,
        SystemRebuildCache, SystemExport, SystemViewStats
    ];
}
