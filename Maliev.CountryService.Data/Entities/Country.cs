using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Data.Entities;

public class Country
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(50)]
    public required string Continent { get; set; }

    [Required]
    [MaxLength(2)]
    public required string ISO2 { get; set; }

    [Required]
    [MaxLength(3)]
    public required string ISO3 { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    // Navigation properties
    public ICollection<CountryCode> CountryCodes { get; set; } = new List<CountryCode>();
}