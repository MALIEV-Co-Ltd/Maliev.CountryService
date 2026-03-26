namespace Maliev.CountryService.Application.Authorization;

/// <summary>
/// Provides access to predefined roles for the Country Service.
/// </summary>
public static class CountryPredefinedRoles
{
    public const string Admin = "roles.country.admin";
    public const string Viewer = "roles.country.viewer";

    public static readonly IReadOnlyList<(string RoleId, string Description, string[] Permissions)> All = new List<(string, string, string[])>
    {
        (
            Admin,
            "Country Administrator with full access",
            new[]
            {
                CountryPermissions.CountryRead,
                CountryPermissions.CountryManage,
                CountryPermissions.RegionRead,
                CountryPermissions.RegionManage,
                CountryPermissions.TimezoneRead,
            }
        ),
        (
            Viewer,
            "Country Viewer with read-only access",
            new[]
            {
                CountryPermissions.CountryRead,
                CountryPermissions.RegionRead,
                CountryPermissions.TimezoneRead,
            }
        ),
    };
}
