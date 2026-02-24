namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Predefined roles for the Country Service.
/// </summary>
public static class CountryPredefinedRoles
{
    /// <summary>Role for super administrators with absolute control.</summary>
    public const string SuperAdmin = "roles.country.superadmin";
    /// <summary>Role for administrators managing country data.</summary>
    public const string Admin = "roles.country.admin";
    /// <summary>Role for managers with general access.</summary>
    public const string Manager = "roles.country.manager";
    /// <summary>Role for users performing data imports.</summary>
    public const string Importer = "roles.country.importer";
    /// <summary>Role for users with read-only access.</summary>
    public const string Viewer = "roles.country.viewer";

    /// <summary>
    /// Collection of all predefined roles for the Country Service.
    /// </summary>
    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (SuperAdmin, "Absolute full control over country data including permanent deletion", CountryPermissions.All.ToArray()),

        (Admin, "Manage country data except permanent delete", CountryPermissions.All
            .Where(p => p != CountryPermissions.CountriesHardDelete).ToArray()),

        (Manager, "Manage country data except hard delete", new[]
        {
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
        }),

        (Importer, "Execute bulk imports", new[]
        {
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.ImportUpload,
            CountryPermissions.ImportExecute,
            CountryPermissions.ImportStatus,
            CountryPermissions.ImportHistory,
            CountryPermissions.SystemViewStats
        }),

        (Viewer, "Read-only access to country data", new[]
        {
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.CountriesSearch
        })
    };

    /// <summary>
    /// Gets the permissions associated with a predefined role.
    /// </summary>
    /// <param name="roleId">The role ID to look up.</param>
    /// <returns>A collection of permission IDs.</returns>
    public static IEnumerable<string> GetPermissionsForRole(string roleId)
    {
        var role = All.FirstOrDefault(r => r.RoleId == roleId);
        return role.Permissions ?? Enumerable.Empty<string>();
    }
}