using FluentAssertions;
using Maliev.CountryService.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace Maliev.CountryService.Tests.Unit;

public class CountrySearchRequestValidationTests
{
    [Fact]
    public void CountrySearchRequest_WithValidPageNumber_ShouldPassValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CountrySearchRequest_WithInvalidPageNumber_ShouldFailValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 0, // Invalid: must be greater than 0
            PageSize = 50
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Contain("PageNumber must be greater than 0");
    }

    [Fact]
    public void CountrySearchRequest_WithValidPageSize_ShouldPassValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CountrySearchRequest_WithInvalidPageSize_TooLarge_ShouldFailValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 1001 // Invalid: must be between 1 and 1000
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Contain("PageSize must be between 1 and 1000");
    }

    [Fact]
    public void CountrySearchRequest_WithInvalidPageSize_Zero_ShouldFailValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 0 // Invalid: must be between 1 and 1000
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Contain("PageSize must be between 1 and 1000");
    }

    [Fact]
    public void CountrySearchRequest_WithValidSortBy_ShouldPassValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50,
            SortBy = "Name" // Valid sort field
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CountrySearchRequest_WithInvalidSortBy_ShouldFailValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50,
            SortBy = "InvalidField" // Invalid sort field
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Contain("SortBy must be one of: Name, Continent, CountryCode, ISO2, ISO3, CreatedDate, ModifiedDate");
    }

    [Fact]
    public void CountrySearchRequest_WithValidSortDirection_ShouldPassValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50,
            SortDirection = "asc" // Valid sort direction
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    [Fact]
    public void CountrySearchRequest_WithInvalidSortDirection_ShouldFailValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            PageNumber = 1,
            PageSize = 50,
            SortDirection = "invalid" // Invalid sort direction
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().ContainSingle();
        validationResults.First().ErrorMessage.Should().Contain("SortDirection must be either 'asc' or 'desc'");
    }

    [Fact]
    public void CountrySearchRequest_WithAllValidFields_ShouldPassValidation()
    {
        // Arrange
        var request = new CountrySearchRequest
        {
            Name = "United States",
            Continent = "North America",
            ISO2 = "US",
            ISO3 = "USA",
            CountryCode = "1",
            PageNumber = 1,
            PageSize = 50,
            SortBy = "Name",
            SortDirection = "asc"
        };

        // Act
        var validationResults = ValidateModel(request);

        // Assert
        validationResults.Should().BeEmpty();
    }

    private static List<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}