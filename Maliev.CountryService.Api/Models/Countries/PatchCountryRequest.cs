namespace Maliev.CountryService.Api.Models.Countries;

public class PatchCountryRequest
{
    // All fields optional for partial updates - at least one required
    public string? Iso2 { get; set; }
    public string? Iso3 { get; set; }
    public string? Name { get; set; }
    public string? OfficialName { get; set; }
    public string? NumericCode { get; set; }
    public string? Capital { get; set; }
    public string? Region { get; set; }
    public string? Subregion { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Demonym { get; set; }
    public decimal? AreaKm2 { get; set; }
    public long? Population { get; set; }
    public decimal? GiniCoefficient { get; set; }
    public string? Timezones { get; set; }
    public string? Borders { get; set; }
    public string? CallingCodes { get; set; }
    public string? TopLevelDomains { get; set; }
    public string? Currencies { get; set; }
    public string? Languages { get; set; }
    public string? Translations { get; set; }
    public string? Flags { get; set; }
    public string? CoatOfArms { get; set; }
    public bool? Independent { get; set; }
    public bool? UnMember { get; set; }
    public bool? Landlocked { get; set; }
}
