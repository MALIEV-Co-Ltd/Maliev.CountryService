using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Models.BulkImport;

/// <summary>
/// Request model for bulk import of countries.
/// </summary>
public class BulkImportRequest
{
    /// <summary>
    /// Gets or sets the list of countries to import.
    /// </summary>
    public List<CreateCountryRequest> Countries { get; set; } = new();
}
