using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Api.Models.Common;
using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Domain.Exceptions;
using Xunit;

namespace Maliev.CountryService.Tests.Api.Models;

public class ModelsTests
{
    [Fact]
    public void CreateCountryRequest_Validation_Works()
    {
        var request = new CreateCountryRequest
        {
            Iso2 = "12",
            Name = ""
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

    [Fact]
    public void CountryServiceException_Domain_Properties_Work()
    {
        var ex = new CountryServiceException("Test", new Exception("inner"));
        Assert.Equal("Test", ex.Message);
        Assert.NotNull(ex.InnerException);
    }

    [Fact]
    public void DuplicateCountryException_Domain_Properties_Work()
    {
        var ex = new DuplicateCountryException("Test");
        Assert.Equal("Test", ex.Message);
    }

    [Fact]
    public void DatabaseUnavailableException_Domain_Properties_Work()
    {
        var ex = new DatabaseUnavailableException("Test", new Exception("inner"));
        Assert.Equal("Test", ex.Message);
    }

    [Fact]
    public void ConcurrencyConflictException_Domain_Properties_Work()
    {
        var ex = new ConcurrencyConflictException("Test", new Exception("inner"));
        Assert.Equal("Test", ex.Message);
    }

    [Fact]
    public void CountryCodeDto_Properties_Work()
    {
        var dto = new CountryCodeDto
        {
            Id = Guid.NewGuid(),
            Code = "US",
            IsPrimary = true
        };

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("US", dto.Code);
        Assert.True(dto.IsPrimary);
    }

    [Fact]
    public void CountryDto_Properties_Work()
    {
        var dto = new CountryDto
        {
            Id = Guid.NewGuid(),
            Name = "United States",
            Continent = "North America",
            ISO2 = "US",
            ISO3 = "USA"
        };

        Assert.NotEqual(Guid.Empty, dto.Id);
        Assert.Equal("United States", dto.Name);
        Assert.Equal("North America", dto.Continent);
        Assert.Equal("US", dto.ISO2);
    }

    [Fact]
    public void CountryListRequest_Properties_Work()
    {
        var request = new CountryListRequest
        {
            Region = "Europe",
            Subregion = "Western Europe"
        };

        Assert.Equal("Europe", request.Region);
        Assert.Equal("Western Europe", request.Subregion);
    }

    [Fact]
    public void ErrorResponse_Properties_Work()
    {
        var response = new ErrorResponse
        {
            ErrorCode = "ERR001",
            Message = "Error occurred",
            Details = "Detailed error info",
            CorrelationId = Guid.NewGuid().ToString()
        };

        Assert.Equal("ERR001", response.ErrorCode);
        Assert.Equal("Error occurred", response.Message);
    }

    [Fact]
    public void ValidationErrorResponse_Properties_Work()
    {
        var response = new ValidationErrorResponse
        {
            RowNumber = 1,
            Field = "name",
            Message = "Required field"
        };

        Assert.Equal(1, response.RowNumber);
        Assert.Equal("name", response.Field);
        Assert.Equal("Required field", response.Message);
    }

    [Fact]
    public void CountrySearchRequest_Properties_Work()
    {
        var request = new CountrySearchRequest
        {
            Name = "United",
            Continent = "North America",
            ISO2 = "US",
            ISO3 = "USA",
            CountryCode = "+1",
            PageSize = 100,
            PageNumber = 2,
            SortBy = "Name",
            SortDirection = "desc"
        };

        Assert.Equal("United", request.Name);
        Assert.Equal("North America", request.Continent);
        Assert.Equal("US", request.ISO2);
        Assert.Equal("USA", request.ISO3);
        Assert.Equal("+1", request.CountryCode);
        Assert.Equal(100, request.PageSize);
        Assert.Equal(2, request.PageNumber);
        Assert.Equal("Name", request.SortBy);
        Assert.Equal("desc", request.SortDirection);
    }

    [Fact]
    public void CountrySearchRequest_Defaults_AreSet()
    {
        var request = new CountrySearchRequest();

        Assert.Equal(50, request.PageSize);
        Assert.Equal(1, request.PageNumber);
        Assert.Equal("Name", request.SortBy);
        Assert.Equal("asc", request.SortDirection);
    }

    [Fact]
    public void UpdateCountryRequest_Properties_Work()
    {
        var request = new UpdateCountryRequest
        {
            Name = "New Name",
            OfficialName = "New Official Name",
            Region = "Asia"
        };

        Assert.Equal("New Name", request.Name);
        Assert.Equal("New Official Name", request.OfficialName);
        Assert.Equal("Asia", request.Region);
    }

    [Fact]
    public void PatchCountryRequest_Properties_Work()
    {
        var request = new PatchCountryRequest
        {
            Name = "Patched Name",
            Region = "Europe"
        };

        Assert.Equal("Patched Name", request.Name);
        Assert.Equal("Europe", request.Region);
    }

    [Fact]
    public void BulkImportRequest_Properties_Work()
    {
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest { Name = "Test" }
            }
        };

        Assert.Single(request.Countries);
    }

    [Fact]
    public void BulkImportStatusResponse_Properties_Work()
    {
        var response = new BulkImportStatusResponse
        {
            JobId = Guid.NewGuid(),
            Status = "Completed",
            TotalRecords = 100,
            ProcessedRecords = 100,
            ValidationErrors = new List<ValidationErrorResponse>()
        };

        Assert.Equal("Completed", response.Status);
        Assert.Equal(100, response.TotalRecords);
    }

    [Fact]
    public void CountryResponse_Properties_Work()
    {
        var response = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "Thailand",
            Iso2 = "TH",
            Iso3 = "THA",
            Region = "Asia",
            Population = 70000000
        };

        Assert.Equal("Thailand", response.Name);
        Assert.Equal("TH", response.Iso2);
        Assert.Equal("THA", response.Iso3);
    }

    [Fact]
    public void PagedResult_Properties_Work()
    {
        var result = new PagedResult<CountryDto>(
            new List<CountryDto> { new CountryDto { Name = "Test", Continent = "Asia", ISO2 = "TH", ISO3 = "THA" } },
            1, 10, 100);

        Assert.Single(result.Data);
        Assert.Equal(1, result.Page);
        Assert.Equal(10, result.PageSize);
    }
}
