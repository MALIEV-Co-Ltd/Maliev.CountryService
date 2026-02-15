using System.Text.Json;
using Maliev.CountryService.Api.BackgroundServices;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Services;

public class CacheWarmingServiceTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactoryMock;
    private readonly Mock<ILogger<CacheWarmingService>> _loggerMock;
    private readonly Mock<ICountryService> _countryServiceMock;

    public CacheWarmingServiceTests()
    {
        _scopeFactoryMock = new Mock<IServiceScopeFactory>();
        _loggerMock = new Mock<ILogger<CacheWarmingService>>();
        _countryServiceMock = new Mock<ICountryService>();

        var scopeMock = new Mock<IServiceScope>();
        var serviceProviderMock = new Mock<IServiceProvider>();

        _scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        scopeMock.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);
        serviceProviderMock.Setup(x => x.GetService(typeof(ICountryService))).Returns(_countryServiceMock.Object);
    }

    [Fact]
    public async Task StartAsync_WaramsCache()
    {
        // Arrange
        var configDir = Path.Combine(AppContext.BaseDirectory, "Configuration");
        Directory.CreateDirectory(configDir);
        var filePath = Path.Combine(configDir, "Top50PopulousCountries.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(new List<string> { "TH", "US" }));

        var service = new CacheWarmingService(_scopeFactoryMock.Object, _loggerMock.Object);

        _countryServiceMock.Setup(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "Thailand", Iso2 = "TH" });
        _countryServiceMock.Setup(x => x.GetByIso2Async("US", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CountryResponse { Name = "United States", Iso2 = "US" });

        // Act
        var cts = new CancellationTokenSource();
        // Since we reduced the delay to 50ms in Testing env, it should finish quickly
        await service.StartAsync(cts.Token);

        // Assert
        _countryServiceMock.Verify(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _countryServiceMock.Verify(x => x.GetByIso2Async("US", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
