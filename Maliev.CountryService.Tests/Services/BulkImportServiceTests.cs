using System.Text.Json;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.BulkImport;
using Maliev.CountryService.Application.Models.Countries;
using Maliev.CountryService.Application.Services;
using Maliev.CountryService.Domain.Entities;
using Maliev.CountryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit;

namespace Maliev.CountryService.Tests.Services;

[Collection("TestDatabase")]
public class BulkImportServiceTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly ICountryDbContext _context;
    private readonly ICountryService _countryService;
    private readonly ILogger<BulkImportService> _logger;

    public BulkImportServiceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _context = factory.GetDbContext();
        _countryService = factory.Services.GetRequiredService<ICountryService>();
        _logger = factory.Services.GetRequiredService<ILogger<BulkImportService>>();
    }

    [Fact]
    public async Task ValidateImport_ValidData_ReturnsSuccess()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "AA", Iso3 = "AAA", Name = "Country A" },
                new() { Iso2 = "BB", Iso3 = "BBB", Name = "Country B" }
            }
        };

        // Act
        var result = await service.ValidateImportAsync(request, "test-user");

        // Assert
        Assert.Equal("Validated", result.Status);
        Assert.Empty(result.ValidationErrors);
        Assert.Equal(2, result.TotalRecords);
    }

    [Fact]
    public async Task ValidateImport_InvalidData_ReturnsErrors()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "INVALID", Iso3 = "A", Name = "" }, // Multiple errors
                new() { Iso2 = "BB", Iso3 = "BBB", Name = new string('A', 101) } // Name too long
            }
        };

        // Act
        var result = await service.ValidateImportAsync(request, "test-user");

        // Assert
        Assert.Equal("ValidationFailed", result.Status);
        Assert.NotEmpty(result.ValidationErrors);
        Assert.Contains(result.ValidationErrors, e => e.Field == "Request" && e.Message.Contains("ISO2"));
        Assert.Contains(result.ValidationErrors, e => e.Field == "Request" && e.Message.Contains("name"));
    }

    [Fact]
    public async Task ValidateImport_DuplicateInBatch_ReturnsErrors()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "AA", Iso3 = "AAA", Name = "Country A" },
                new() { Iso2 = "AA", Iso3 = "BBB", Name = "Country B" } // Duplicate ISO2
            }
        };

        // Act
        var result = await service.ValidateImportAsync(request, "test-user");

        // Assert
        Assert.Equal("ValidationFailed", result.Status);
        Assert.Contains(result.ValidationErrors, e => e.Field == "Iso2" && e.Message.Contains("batch"));
    }

    [Fact]
    public async Task ValidateImport_DuplicateInDatabase_ReturnsErrors()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);

        // Seed database
        _context.Countries.Add(new Country
        {
            Iso2 = "AA",
            Iso3 = "AAA",
            Name = "Existing",
            CreatedBy = "system",
            UpdatedBy = "system",
            Version = Guid.NewGuid()
        });
        await _context.SaveChangesAsync();

        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "AA", Iso3 = "BBB", Name = "Duplicate DB" }
            }
        };

        // Act
        var result = await service.ValidateImportAsync(request, "test-user");

        // Assert
        Assert.Equal("ValidationFailed", result.Status);
        Assert.Contains(result.ValidationErrors, e => e.Field == "Iso2" && e.Message.Contains("database"));
    }

    [Fact]
    public async Task ProcessImport_Success()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new() { Iso2 = "AA", Iso3 = "AAA", Name = "Country A" },
                new() { Iso2 = "BB", Iso3 = "BBB", Name = "Country B" }
            }
        };

        var validationResult = await service.ValidateImportAsync(request, "test-user");

        // Act
        var processResult = await service.ProcessImportAsync(validationResult.JobId);

        // Assert
        Assert.Equal("Completed", processResult.Status);
        Assert.Equal(2, processResult.ProcessedRecords);

        var count = await _context.Countries.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task GetJobStatus_Success()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var service = new BulkImportService(_context, _countryService, _logger);
        var request = new BulkImportRequest { Countries = new List<CreateCountryRequest>() };
        var validationResult = await service.ValidateImportAsync(request, "test-user");

        // Act
        var result = await service.GetJobStatusAsync(validationResult.JobId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(validationResult.JobId, result.JobId);
    }

    [Fact]
    public async Task ProcessImport_JobNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var service = new BulkImportService(_context, _countryService, _logger);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ProcessImportAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ProcessImport_InvalidStatus_ThrowsInvalidOperationException()
    {
        // Arrange
        await _factory.CleanDatabaseAsync();
        var job = new BulkImportJob { Status = "Failed", CreatedBy = "user", CreatedAtUtc = DateTime.UtcNow, ValidationErrors = "[]", UserId = "user" };
        _context.BulkImportJobs.Add(job);
        await _context.SaveChangesAsync();

        var service = new BulkImportService(_context, _countryService, _logger);
        var jobId = job.Id;

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ProcessImportAsync(jobId));
    }
}
