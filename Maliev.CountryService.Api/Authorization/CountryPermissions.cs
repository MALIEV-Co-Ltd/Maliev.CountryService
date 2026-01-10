namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Defines the granular permissions for the Country Service.
/// Follows GCP-style naming: {service}.{resource}.{action}
/// </summary>
public static class CountryPermissions
{
    /// <summary>Read country details</summary>
    public const string CountriesRead = "country.countries.read";
    /// <summary>List all countries</summary>
    public const string CountriesList = "country.countries.list";
    /// <summary>Search countries</summary>
    public const string CountriesSearch = "country.countries.search";

    /// <summary>Create new countries</summary>
    public const string CountriesCreate = "country.countries.create";
    /// <summary>Update country information</summary>
    public const string CountriesUpdate = "country.countries.update";
    /// <summary>Soft delete countries</summary>
    public const string CountriesDelete = "country.countries.delete";
    /// <summary>Permanently delete countries</summary>
    public const string CountriesHardDelete = "country.countries.hard-delete";
    /// <summary>Restore soft-deleted countries</summary>
    public const string CountriesRestore = "country.countries.restore";

    /// <summary>Upload bulk import file</summary>
    public const string ImportUpload = "country.import.upload";
    /// <summary>Execute bulk import</summary>
    public const string ImportExecute = "country.import.execute";
    /// <summary>View import job status</summary>
    public const string ImportStatus = "country.import.status";
    /// <summary>Cancel running import jobs</summary>
    public const string ImportCancel = "country.import.cancel";
    /// <summary>View import history</summary>
    public const string ImportHistory = "country.import.history";

    /// <summary>Rebuild country cache</summary>
    public const string SystemRebuildCache = "country.system.rebuild-cache";
    /// <summary>Export all country data</summary>
    public const string SystemExport = "country.system.export";
    /// <summary>View service statistics</summary>
    public const string SystemViewStats = "country.system.view-stats";

    /// <summary>
    /// Collection of all defined country permissions with descriptions.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { CountriesRead, "Read country details" },
        { CountriesList, "List all countries" },
        { CountriesSearch, "Search countries" },
        { CountriesCreate, "Create new countries" },
        { CountriesUpdate, "Update country information" },
        { CountriesDelete, "Soft delete countries" },
        { CountriesHardDelete, "Permanently delete countries" },
        { CountriesRestore, "Restore soft-deleted countries" },
        { ImportUpload, "Upload bulk import file" },
        { ImportExecute, "Execute bulk import" },
        { ImportStatus, "View import job status" },
        { ImportCancel, "Cancel running import jobs" },
        { ImportHistory, "View import history" },
        { SystemRebuildCache, "Rebuild country cache" },
        { SystemExport, "Export all country data" },
        { SystemViewStats, "View service statistics" }
    };

    /// <summary>All available permission codes</summary>
    public static IEnumerable<string> All => AllWithDescriptions.Keys;
}
