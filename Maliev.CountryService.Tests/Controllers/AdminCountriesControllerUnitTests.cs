using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Controllers;

public class AdminCountriesControllerUnitTests
{
    private readonly Mock<ICountryService> _countryServiceMock;
    private readonly Mock<ILogger<AdminCountriesController>> _loggerMock;
    private readonly Mock<Maliev.CountryService.Api.Metrics.BusinessMetrics> _metricsMock;
    private readonly AdminCountriesController _controller;

    public AdminCountriesControllerUnitTests()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _loggerMock = new Mock<ILogger<AdminCountriesController>>();

        // BusinessMetrics needs IConfiguration, mock it
        var configMock = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        _metricsMock = new Mock<Maliev.CountryService.Api.Metrics.BusinessMetrics>(configMock.Object);

        _controller = new AdminCountriesController(_countryServiceMock.Object, _loggerMock.Object, _metricsMock.Object);

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
    public async Task Update_Returns428_WhenIfMatchMissing()
    {
        // Act
        var result = await _controller.Update(1, new UpdateCountryRequest(), default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(428, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Update_Returns412_OnConcurrencyConflict()
    {
        // Arrange
        _controller.Request.Headers["If-Match"] = "etag";
        _countryServiceMock.Setup(x => x.UpdateAsync(It.IsAny<long>(), It.IsAny<UpdateCountryRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Concurrency conflict"));

        // Act
        var result = await _controller.Update(1, new UpdateCountryRequest(), default);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(412, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Patch_ReturnsOk()
    {
        // Arrange
        _controller.Request.Headers["If-Match"] = "etag";
        var response = new CountryResponse { Id = 1, Name = "Patched", Iso2 = "TH", ETag = "new-etag" };
        _countryServiceMock.Setup(x => x.PatchAsync(It.IsAny<long>(), It.IsAny<PatchCountryRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Patch(1, new PatchCountryRequest(), default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(response, okResult.Value);
        Assert.Equal("new-etag", _controller.Response.Headers.ETag);
    }

    [Fact]
    public async Task SoftDelete_ReturnsNoContent()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.SoftDeleteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.SoftDelete(1, default);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task HardDelete_ReturnsNoContent()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.HardDeleteAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.HardDelete(1, default);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Restore_ReturnsNoContent()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.RestoreAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Restore(1, default);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task RebuildCache_ReturnsNoContent()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.InvalidateListCachesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RebuildCache(default);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task ExportAll_ReturnsOk()
    {
        // Arrange
        var list = new List<CountryResponse> { new CountryResponse { Id = 1, Name = "Test" } };
        _countryServiceMock.Setup(x => x.ListAsync(It.IsAny<CountryListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Maliev.CountryService.Api.Models.Common.PaginatedResponse<CountryResponse> { Data = list });

        // Act
        var result = await _controller.ExportAll(default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(list, okResult.Value);
    }
}
