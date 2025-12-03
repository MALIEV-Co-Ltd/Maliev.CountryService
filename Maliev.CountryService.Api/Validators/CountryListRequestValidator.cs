using FluentValidation;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Validators;

/// <summary>
/// Validator for country list requests.
/// </summary>
public class CountryListRequestValidator : AbstractValidator<CountryListRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountryListRequestValidator"/> class.
    /// </summary>
    public CountryListRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(1000).WithMessage("Page size must not exceed 1000");

        RuleFor(x => x.SortBy)
            .Must(value => string.IsNullOrEmpty(value) || new[] { "name", "iso2", "iso3", "population", "area" }.Contains(value.ToLower()))
            .WithMessage("SortBy must be one of: name, iso2, iso3, population, area")
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy));

        RuleFor(x => x.SortOrder)
            .Must(value => string.IsNullOrEmpty(value) || new[] { "asc", "desc" }.Contains(value.ToLower()))
            .WithMessage("SortOrder must be either 'asc' or 'desc'")
            .When(x => !string.IsNullOrWhiteSpace(x.SortOrder));
    }
}
