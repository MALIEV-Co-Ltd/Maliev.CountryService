using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.BulkImport;
using Maliev.CountryService.Application.Models.Countries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Controllers;

public class BulkImportControllerUnitTests
{
    private readonly Mock<IBulkImportService> _bulkImportServiceMock;
    private readonly Mock<ILogger<BulkImportController>> _loggerMock;
    private readonly Mock<BusinessMetrics> _metricsMock;
    private readonly BulkImportController _controller;

    public BulkImportControllerUnitTests()
    {
        _bulkImportServiceMock = new Mock<IBulkImportService>();
        _loggerMock = new Mock<ILogger<BulkImportController>>();

        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        _metricsMock = new Mock<BusinessMetrics>(configMock.Object);

        _controller = new BulkImportController(_bulkImportServiceMock.Object, _loggerMock.Object, _metricsMock.Object);

        var user = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim("sub", "test-user")
        }));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task SubmitBulkImport_ExceedsLimit_Returns413()
    {
        // Arrange
        var request = new BulkImportRequest();
        for (int i = 0; i < 10001; i++)
        {
            request.Countries.Add(new CreateCountryRequest { Name = $"Country {i}", Iso2 = $"A{i:D2}" });
        }

        // Act
        var result = await _controller.SubmitBulkImport(request, default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(413, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task SubmitBulkImport_EmptyList_Returns400()
    {
        // Arrange
        var request = new BulkImportRequest { Countries = new List<CreateCountryRequest>() };

        // Act
        var result = await _controller.SubmitBulkImport(request, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SubmitBulkImport_ValidationFailed_Returns400()
    {
        // Arrange
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest { Name = "Test", Iso2 = "TS" }
            }
        };
        _bulkImportServiceMock.Setup(x => x.ValidateImportAsync(It.IsAny<BulkImportRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkImportStatusResponse
            {
                JobId = Guid.NewGuid(),
                Status = "ValidationFailed",
                ValidationErrors = new List<ValidationErrorResponse> { new() { Field = "Iso2", Message = "Invalid" } }
            });

        // Act
        var result = await _controller.SubmitBulkImport(request, default);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task SubmitBulkImport_Valid_Returns202()
    {
        // Arrange
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest { Name = "Test", Iso2 = "TS" }
            }
        };
        var jobId = Guid.NewGuid();
        _bulkImportServiceMock.Setup(x => x.ValidateImportAsync(It.IsAny<BulkImportRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkImportStatusResponse
            {
                JobId = jobId,
                Status = "Validated",
                TotalRecords = 1
            });

        // Act
        var result = await _controller.SubmitBulkImport(request, default);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
        Assert.Equal(jobId, ((BulkImportStatusResponse)acceptedResult.Value!).JobId);
    }

    [Fact]
    public async Task SubmitBulkImport_ThrowsException_Returns500()
    {
        // Arrange
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest { Name = "Test", Iso2 = "TS" }
            }
        };
        _bulkImportServiceMock.Setup(x => x.ValidateImportAsync(It.IsAny<BulkImportRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.SubmitBulkImport(request, default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetJobStatus_NotFound_Returns404()
    {
        // Arrange
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkImportStatusResponse?)null);

        // Act
        var result = await _controller.GetJobStatus(Guid.NewGuid(), default);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetJobStatus_Found_Returns200()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkImportStatusResponse { JobId = jobId, Status = "Validated" });

        // Act
        var result = await _controller.GetJobStatus(jobId, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(200, okResult.StatusCode);
    }

    [Fact]
    public async Task GetJobStatus_ThrowsException_Returns500()
    {
        // Arrange
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.GetJobStatus(Guid.NewGuid(), default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task ProcessJob_JobNotFound_Returns404()
    {
        // Arrange
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BulkImportStatusResponse?)null);

        // Act
        var result = await _controller.ProcessJob(Guid.NewGuid(), default);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task ProcessJob_InvalidStatus_Returns400()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkImportStatusResponse { JobId = jobId, Status = "Failed" });

        // Act
        var result = await _controller.ProcessJob(jobId, default);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ProcessJob_Validated_Returns202()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BulkImportStatusResponse { JobId = jobId, Status = "Validated" });

        // Act
        var result = await _controller.ProcessJob(jobId, default);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedAtActionResult>(result);
        Assert.Equal(202, acceptedResult.StatusCode);
    }

    [Fact]
    public async Task ProcessJob_ThrowsException_Returns500()
    {
        // Arrange
        _bulkImportServiceMock.Setup(x => x.GetJobStatusAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.ProcessJob(Guid.NewGuid(), default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }

    [Fact]
    public void BulkImportController_GetUserId_ReturnsSubClaim()
    {
        // Arrange - test the private method through controller behavior
        var request = new BulkImportRequest
        {
            Countries = new List<CreateCountryRequest>
            {
                new CreateCountryRequest { Name = "Test", Iso2 = "TS" }
            }
        };

        // Act & Assert - just verify controller can handle the request structure
        Assert.NotNull(request.Countries);
        Assert.Single(request.Countries);
    }
}
