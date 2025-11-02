using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Models.BulkImport;

public class BulkImportRequest
{
    public List<CreateCountryRequest> Countries { get; set; } = new();
}
