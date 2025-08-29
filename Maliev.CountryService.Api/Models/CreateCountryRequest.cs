using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Api.Models
{
    public class CreateCountryRequest
    {
        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Name { get; set; }

        [Required]
        [StringLength(50, MinimumLength = 2)]
        public required string Continent { get; set; }

        [Required]
        [StringLength(30, MinimumLength = 2)]
        public required string CountryCode { get; set; }

        [Required]
        [StringLength(2, MinimumLength = 2)]
        public required string Iso2 { get; set; }

        [Required]
        [StringLength(3, MinimumLength = 3)]
        public required string Iso3 { get; set; }
    }
}