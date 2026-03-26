namespace Maliev.CountryService.Application.Authorization;

/// <summary>
/// Defines the permissions for the Country Service.
/// </summary>
public static class CountryPermissions
{
    public const string CountryRead = "country.countries.read";
    public const string CountryManage = "country.countries.manage";

    public const string RegionRead = "country.regions.read";
    public const string RegionManage = "country.regions.manage";

    public const string TimezoneRead = "country.timezones.read";

    public static readonly IReadOnlyDictionary<string, string> AllWithDescriptions = new Dictionary<string, string>
    {
        { CountryRead, "Read country data" },
        { CountryManage, "Manage country data" },
        { RegionRead, "Read region data" },
        { RegionManage, "Manage region data" },
        { TimezoneRead, "Read timezone data" },
    };

    public static string[] All => AllWithDescriptions.Keys.ToArray();
}
