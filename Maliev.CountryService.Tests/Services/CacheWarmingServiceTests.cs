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
        // Since it has a 5s delay, I'll use a short timeout and hope for the best or mock it if possible
        // Actually, 5s is too long for a test. I might want to change the code to allow smaller delay in tests,
        // but I shouldn't change the code unless necessary.
        // Let's try to run it.
        var startTask = service.StartAsync(cts.Token);

        // Wait for it to finish or timeout
        await Task.WhenAny(startTask, Task.Delay(6000));

        // Assert
        _countryServiceMock.Verify(x => x.GetByIso2Async("TH", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _countryServiceMock.Verify(x => x.GetByIso2Async("US", It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
