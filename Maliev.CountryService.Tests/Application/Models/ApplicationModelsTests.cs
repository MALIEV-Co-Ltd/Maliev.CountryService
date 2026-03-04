using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Maliev.CountryService.Application.Models.Countries;
using Maliev.CountryService.Application.Models.BulkImport;
using Maliev.CountryService.Application.Models.Common;
using Xunit;

namespace Maliev.CountryService.Tests.Application.Models;

public class ApplicationModelsTests
{
    [Fact]
    public void CreateCountryRequest_Properties_Work()
    {
        var request = new CreateCountryRequest
        {
            Name = "United States",
            Iso2 = "US",
            Iso3 = "USA"
        };

        Assert.Equal("United States", request.Name);
        Assert.Equal("US", request.Iso2);
        Assert.Equal("USA", request.Iso3);
    }

    [Fact]
    public void UpdateCountryRequest_Properties_Work()
    {
        var request = new UpdateCountryRequest
        {
            Name = "United States",
            Iso2 = "US"
        };

        Assert.Equal("United States", request.Name);
        Assert.Equal("US", request.Iso2);
    }

    [Fact]
    public void PatchCountryRequest_Properties_Work()
    {
        var request = new PatchCountryRequest
        {
            Name = "United States Updated"
        };

        Assert.Equal("United States Updated", request.Name);
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
    public void CountryResponse_Properties_Work()
    {
        var response = new CountryResponse
        {
            Id = Guid.NewGuid(),
            Name = "United States",
            Iso2 = "US"
        };

        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal("United States", response.Name);
        Assert.Equal("US", response.Iso2);
    }

    [Fact]
    public void BulkImportRequest_Properties_Work()
    {
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "AA", Iso3 = "AAA", Name = "Test" }
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
            TotalRecords = 10,
            ProcessedRecords = 10,
            ValidationErrors = new List<ValidationErrorResponse>()
        };

        Assert.NotEqual(Guid.Empty, response.JobId);
        Assert.Equal("Completed", response.Status);
        Assert.Equal(10, response.TotalRecords);
    }

    [Fact]
    public void PaginatedResponse_Properties_Work()
    {
        var response = new PaginatedResponse<string>
        {
            Data = new List<string> { "test" },
            Page = 1,
            PageSize = 10,
            TotalCount = 100
        };

        Assert.Single(response.Data);
        Assert.Equal(1, response.Page);
        Assert.Equal(10, response.PageSize);
        Assert.Equal(100, response.TotalCount);
        Assert.Equal(10, response.TotalPages);
    }

    [Fact]
    public void ErrorResponse_Properties_Work()
    {
        var response = new ErrorResponse
        {
            Error = "ERR001",
            Message = "Error occurred"
        };

        Assert.Equal("ERR001", response.Error);
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
}
