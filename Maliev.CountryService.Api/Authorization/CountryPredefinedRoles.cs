namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Defines predefined roles for the Country Service.
/// </summary>
public static class CountryPredefinedRoles
{
    /// <summary>Country Administrator: Full control over country data.</summary>
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "country-admin",
        RoleName = "Country Administrator",
        Description = "Full control over country data",
        Permissions = CountryPermissions.All
    };

    /// <summary>Country Manager: Manage country data except hard delete.</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "country-manager",
        RoleName = "Country Manager",
        Description = "Manage country data except hard delete",
        Permissions =
        [
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.CountriesSearch,
            CountryPermissions.CountriesCreate,
            CountryPermissions.CountriesUpdate,
            CountryPermissions.CountriesDelete,
            CountryPermissions.CountriesRestore,
            CountryPermissions.ImportUpload,
            CountryPermissions.ImportExecute,
            CountryPermissions.ImportStatus,
            CountryPermissions.ImportCancel,
            CountryPermissions.ImportHistory,
            CountryPermissions.SystemRebuildCache,
            CountryPermissions.SystemExport,
            CountryPermissions.SystemViewStats
        ]
    };

    /// <summary>Country Data Importer: Execute bulk imports.</summary>
    public static readonly RoleRegistration Importer = new()
    {
        RoleId = "country-importer",
        RoleName = "Country Data Importer",
        Description = "Execute bulk imports",
        Permissions =
        [
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.ImportUpload,
            CountryPermissions.ImportExecute,
            CountryPermissions.ImportStatus,
            CountryPermissions.ImportHistory,
            CountryPermissions.SystemViewStats
        ]
    };

    /// <summary>Country Data Viewer: Read-only access to country data.</summary>
    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "country-viewer",
        RoleName = "Country Data Viewer",
        Description = "Read-only access to country data",
        Permissions =
        [
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.CountriesSearch
        ]
    };

    /// <summary>All predefined roles.</summary>
    public static readonly RoleRegistration[] All = [Admin, Manager, Importer, Viewer];
}