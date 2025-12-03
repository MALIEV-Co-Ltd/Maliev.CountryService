using FluentValidation;
using Maliev.CountryService.Api.Models.Countries;

namespace Maliev.CountryService.Api.Validators;

/// <summary>
/// Validator for patch country requests (partial updates).
/// </summary>
public class PatchCountryRequestValidator : AbstractValidator<PatchCountryRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PatchCountryRequestValidator"/> class.
    /// </summary>
    public PatchCountryRequestValidator()
    {
        // At least one field must be provided for partial update
        RuleFor(x => x)
            .Must(HaveAtLeastOneField)
            .WithMessage("At least one field must be provided for partial update");

        // Same validation rules as Create/Update, but only when field is provided
        RuleFor(x => x.Iso2)
            .Length(2).WithMessage("ISO2 code must be exactly 2 characters")
            .Matches("^[A-Z]{2}$").WithMessage("ISO2 code must be uppercase letters only")
            .When(x => x.Iso2 != null);

        RuleFor(x => x.Iso3)
            .Length(3).WithMessage("ISO3 code must be exactly 3 characters")
            .Matches("^[A-Z]{3}$").WithMessage("ISO3 code must be uppercase letters only")
            .When(x => x.Iso3 != null);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Country name cannot be empty")
            .MaximumLength(100).WithMessage("Country name must not exceed 100 characters")
            .When(x => x.Name != null);

        RuleFor(x => x.OfficialName)
            .MaximumLength(200).WithMessage("Official name must not exceed 200 characters")
            .When(x => x.OfficialName != null);

        RuleFor(x => x.NumericCode)
            .Length(3).WithMessage("Numeric code must be exactly 3 characters")
            .Matches("^[0-9]{3}$").WithMessage("Numeric code must be digits only")
            .When(x => x.NumericCode != null);

        RuleFor(x => x.Capital)
            .MaximumLength(100).WithMessage("Capital must not exceed 100 characters")
            .When(x => x.Capital != null);

        RuleFor(x => x.Region)
            .MaximumLength(50).WithMessage("Region must not exceed 50 characters")
            .When(x => x.Region != null);

        RuleFor(x => x.Subregion)
            .MaximumLength(50).WithMessage("Subregion must not exceed 50 characters")
            .When(x => x.Subregion != null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90")
            .When(x => x.Latitude.HasValue);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180")
            .When(x => x.Longitude.HasValue);

        RuleFor(x => x.Demonym)
            .MaximumLength(50).WithMessage("Demonym must not exceed 50 characters")
            .When(x => x.Demonym != null);

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

    private static bool HaveAtLeastOneField(PatchCountryRequest request)
    {
        return request.Iso2 != null || request.Iso3 != null || request.Name != null ||
               request.OfficialName != null || request.NumericCode != null || request.Capital != null ||
               request.Region != null || request.Subregion != null || request.Latitude.HasValue ||
               request.Longitude.HasValue || request.Demonym != null || request.AreaKm2.HasValue ||
               request.Population.HasValue || request.GiniCoefficient.HasValue || request.Timezones != null ||
               request.Borders != null || request.CallingCodes != null || request.TopLevelDomains != null ||
               request.Currencies != null || request.Languages != null || request.Translations != null ||
               request.Flags != null || request.CoatOfArms != null || request.Independent.HasValue ||
               request.UnMember.HasValue || request.Landlocked.HasValue;
    }
}
