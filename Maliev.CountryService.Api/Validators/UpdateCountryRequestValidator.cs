using FluentValidation;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Validators;

/// <summary>
/// Validator for update country requests (full replacement).
/// </summary>
public class UpdateCountryRequestValidator : AbstractValidator<UpdateCountryRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCountryRequestValidator"/> class.
    /// </summary>
    public UpdateCountryRequestValidator()
    {
        // Same rules as CreateCountryRequestValidator since Update is full replacement
        RuleFor(x => x.Iso2)
            .NotEmpty().WithMessage("ISO2 code is required")
            .Length(2).WithMessage("ISO2 code must be exactly 2 characters")
            .Matches("^[A-Z]{2}$").WithMessage("ISO2 code must be uppercase letters only");

        RuleFor(x => x.Iso3)
            .NotEmpty().WithMessage("ISO3 code is required")
            .Length(3).WithMessage("ISO3 code must be exactly 3 characters")
            .Matches("^[A-Z]{3}$").WithMessage("ISO3 code must be uppercase letters only");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Country name is required")
            .MaximumLength(100).WithMessage("Country name must not exceed 100 characters");

        RuleFor(x => x.OfficialName)
            .MaximumLength(200).WithMessage("Official name must not exceed 200 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.OfficialName));

        RuleFor(x => x.NumericCode)
            .Length(3).WithMessage("Numeric code must be exactly 3 characters")
            .Matches("^[0-9]{3}$").WithMessage("Numeric code must be digits only")
            .When(x => !string.IsNullOrWhiteSpace(x.NumericCode));

        RuleFor(x => x.Capital)
            .MaximumLength(100).WithMessage("Capital must not exceed 100 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Capital));

        RuleFor(x => x.Region)
            .MaximumLength(50).WithMessage("Region must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Region));

        RuleFor(x => x.Subregion)
            .MaximumLength(50).WithMessage("Subregion must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Subregion));

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Demonym)
            .MaximumLength(50).WithMessage("Demonym must not exceed 50 characters")
            .When(x => !string.IsNullOrWhiteSpace(x.Demonym));

        RuleFor(x => x.AreaKm2)
            .GreaterThan(0).WithMessage("Area must be greater than 0")
            .When(x => x.AreaKm2.HasValue);

        RuleFor(x => x.Population)
            .GreaterThanOrEqualTo(0).WithMessage("Population cannot be negative")
            .When(x => x.Population.HasValue);

        RuleFor(x => x.GiniCoefficient)
            .InclusiveBetween(0, 100).WithMessage("Gini coefficient must be between 0 and 100")
            .When(x => x.GiniCoefficient.HasValue);
    }
}
