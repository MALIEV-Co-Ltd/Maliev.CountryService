using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.Common;
using Maliev.CountryService.Application.Models.Countries;
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

    [Fact]
    public async Task GetByIso2_Returns304_WhenETagMatches()
    {
        // Arrange
        var etag = "\"abc123\"";
        _countryServiceMock.Setup(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "Thailand", Iso2 = "TH", ETag = etag });

        // Act
        var result = await _controller.GetByIso2("TH", etag, default);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(304, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task GetByIso2_SetsCacheHeaders_WhenServedFromCache()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "Thailand", Iso2 = "TH", ETag = "\"etag\"", XServedFromCache = true });

        // Act
        var result = await _controller.GetByIso2("TH", null, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("true", _controller.Response.Headers["X-Served-From-Cache"]);
    }

    [Fact]
    public async Task GetByIso3_ReturnsOk()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.GetByIso3Async("THA", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "Thailand", Iso3 = "THA" });

        // Act
        var result = await _controller.GetByIso3("THA", null, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CountryResponse>(okResult.Value);
        Assert.Equal("THA", response.Iso3);
    }

    [Fact]
    public async Task GetByIso3_ReturnsNotFound()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.GetByIso3Async("XXX", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        // Act
        var result = await _controller.GetByIso3("XXX", null, default);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_ReturnsOk()
    {
        // Arrange
        var id = Guid.NewGuid();
        _countryServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Id = id, Name = "Thailand" });

        // Act
        var result = await _controller.GetById(id, null, default);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<CountryResponse>(okResult.Value);
        Assert.Equal(id, response.Id);
    }

    [Fact]
    public async Task GetById_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _countryServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CountryResponse?)null);

        // Act
        var result = await _controller.GetById(id, null, default);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetById_Returns304_WhenETagMatches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var etag = "\"etag123\"";
        _countryServiceMock.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Id = id, Name = "Thailand", ETag = etag });

        // Act
        var result = await _controller.GetById(id, etag, default);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(304, ((StatusCodeResult)result).StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOk()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.ListAsync(It.IsAny<CountryListRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<CountryResponse> { Data = new List<CountryResponse>() });

        // Act
        var result = await _controller.List(new CountryListRequest(), default);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Search_ReturnsOk()
    {
        // Arrange
        _countryServiceMock.Setup(x => x.SearchAsync("Thai", 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaginatedResponse<CountryResponse> { Data = new List<CountryResponse>() });

        // Act
        var result = await _controller.Search("Thai", 1, 20, default);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Search_Returns400_WhenQueryEmpty()
    {
        // Act
        var result = await _controller.Search("", 1, 20, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Search_Returns400_WhenPageInvalid()
    {
        // Act
        var result = await _controller.Search("test", 0, 20, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Search_Returns400_WhenPageSizeTooLarge()
    {
        // Act
        var result = await _controller.Search("test", 1, 2000, default);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
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
