using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models
{
    public class CountryDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public required string Continent { get; set; }
        public required string CountryCode { get; set; }
        public required string Iso2 { get; set; }
        public required string Iso3 { get; set; }
    }
}