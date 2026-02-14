using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Controllers;

public class CountryControllerTests
{
    private readonly Mock<ICountryService> _countryServiceMock;
    private readonly CountriesController _controller;

    public CountryControllerTests()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _controller = new CountriesController(_countryServiceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [Fact]
    public async Task GetByIso2_ReturnsOk()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "Thailand", Iso2 = "TH" });

        // Act
        var result = await _controller.GetByIso2("TH", null, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CountryResponse>(okResult.Value);
        Assert.Equal("TH", response.Iso2);
    }

    [Fact]
    public async Task GetByIso2_ReturnsNotFound()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.GetByIso2Async("XX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        // Act
        var result = await _controller.GetByIso2("XX", null, default);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

public class AdminCountryControllerTests
{
    private readonly Mock<ICountryService> _countryServiceMock;
    private readonly Mock<ILogger<AdminCountriesController>> _loggerMock;
    private readonly BusinessMetrics _businessMetrics;
    private readonly AdminCountriesController _controller;

    public AdminCountryControllerTests()
    {
        _countryServiceMock = new Mock<ICountryService>();
        _loggerMock = new Mock<ILogger<AdminCountriesController>>();

        var configMock = new Mock<IConfiguration>();
        _businessMetrics = new BusinessMetrics(configMock.Object);

        _controller = new AdminCountriesController(_countryServiceMock.Object, _loggerMock.Object, _businessMetrics);

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
    public async Task Create_ReturnsCreatedAtAction()
    {
        // Arrange
        var id = Guid.NewGuid();
        var request = new CreateCountryRequest { Name = "Test", Iso2 = "TS", Iso3 = "TST", NumericCode = "123" };
        var response = new CountryResponse { Id = id, Name = "Test", Iso2 = "TS" };
        _countryServiceMock.Setup(x => x.CreateAsync(request, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.Create(request, default);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(response, createdResult.Value);
    }
}
