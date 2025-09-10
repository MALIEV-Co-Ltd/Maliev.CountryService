using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Data.Entities;

public class CountryCode
{
    public int Id { get; set; }

    public int CountryId { get; set; }

    [Required]
    [MaxLength(20)]
    public required string Code { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    // Navigation properties
    public Country Country { get; set; } = null!;
}