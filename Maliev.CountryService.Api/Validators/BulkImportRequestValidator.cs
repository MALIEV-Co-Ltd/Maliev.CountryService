using FluentValidation;
using Maliev.CountryService.Api.Models.BulkImport;

namespace Maliev.CountryService.Api.Validators;

public class BulkImportRequestValidator : AbstractValidator<BulkImportRequest>
{
    public BulkImportRequestValidator()
    {
        RuleFor(x => x.Countries)
            .NotNull().WithMessage("Countries list cannot be null")
            .NotEmpty().WithMessage("Countries list cannot be empty")
            .Must(x => x.Count <= 1000).WithMessage("Cannot import more than 1000 countries per batch");

        RuleForEach(x => x.Countries)
            .SetValidator(new CreateCountryRequestValidator());
    }
}
