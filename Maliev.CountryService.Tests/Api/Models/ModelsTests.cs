using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Maliev.CountryService.Api.Models.Countries;
using Xunit;

namespace Maliev.CountryService.Tests.Api.Models;

public class ModelsTests
{
    [Fact]
    public void CreateCountryRequest_Validation_Works()
    {
        var request = new CreateCountryRequest
        {
            Iso2 = "12", // Invalid
            Name = "" // Required
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage!.Contains("uppercase letters"));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("required"));
    }

    [Fact]
    public void CountryListRequest_Defaults_AreSet()
    {
        var request = new CountryListRequest();
        Assert.Null(request.Page);
        Assert.Null(request.PageSize);
        Assert.Equal("name", request.SortBy);
        Assert.Equal("asc", request.SortOrder);
    }

    [Fact]
    public void CountryServiceException_Properties_Work()
    {
        var ex = new Maliev.CountryService.Api.Exceptions.CountryServiceException("Test");
        Assert.Equal("Test", ex.Message);
    }

    [Fact]
    public void DuplicateCountryException_Properties_Work()
    {
        var ex = new Maliev.CountryService.Api.Exceptions.DuplicateCountryException("Test");
        Assert.Equal("Test", ex.Message);
    }
}
