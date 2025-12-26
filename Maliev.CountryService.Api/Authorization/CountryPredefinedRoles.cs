namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Defines predefined roles for the Country Service.
/// </summary>
public static class CountryPredefinedRoles
{
    /// <summary>Country Super Administrator: Absolute full control.</summary>
    public static readonly RoleRegistration SuperAdmin = new()
    {
        RoleId = "roles.country.superadmin",
        RoleName = "Country Super Administrator",
        Description = "Absolute full control over country data including permanent deletion",
        Permissions = CountryPermissions.All
    };

    /// <summary>Country Administrator: Manage country data except permanent delete.</summary>
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "roles.country.admin",
        RoleName = "Country Administrator",
        Description = "Manage country data except permanent delete",
        Permissions = CountryPermissions.All
            .Where(p => p != CountryPermissions.CountriesHardDelete)
            .ToArray()
    };

    /// <summary>Country Manager: Manage country data except import and hard delete.</summary>
    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "roles.country.manager",
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
        RoleId = "roles.country.importer",
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
        RoleId = "roles.country.viewer",
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
    public static readonly RoleRegistration[] All = [SuperAdmin, Admin, Manager, Importer, Viewer];

    /// <summary>
    /// Gets permissions for a given role ID.
    /// </summary>
    public static IEnumerable<string> GetPermissionsForRole(string roleId)
    {
        return All.FirstOrDefault(r => r.RoleId == roleId)?.Permissions ?? Enumerable.Empty<string>();
    }
}