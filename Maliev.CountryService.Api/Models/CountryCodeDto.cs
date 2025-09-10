namespace Maliev.CountryService.Api.Models;

public class CountryCodeDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public bool IsPrimary { get; set; }
}