using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace Maliev.CountryService.Tests.Services;

[Collection("TestDatabase")]
public class RedisCacheServiceTests
{
    private readonly TestWebApplicationFactory _factory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly MemoryCacheService _fallbackCache;

    public RedisCacheServiceTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _redis = factory.Services.GetRequiredService<IConnectionMultiplexer>();
        _logger = factory.Services.GetRequiredService<ILogger<RedisCacheService>>();
        _fallbackCache = factory.Services.GetRequiredService<MemoryCacheService>();
    }

    [Fact]
    public async Task SetAndGet_Success()
    {
        // Arrange
        var service = new RedisCacheService(_logger, _fallbackCache, _redis);
        var key = "test-key-" + Guid.NewGuid();
        var value = new TestData { Name = "Test" };

        // Act
        await service.SetAsync(key, value, TimeSpan.FromMinutes(1));
        var result = await service.GetAsync<TestData>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task Get_NonExistent_ReturnsNull()
    {
        // Arrange
        var service = new RedisCacheService(_logger, _fallbackCache, _redis);
        var key = "non-existent-" + Guid.NewGuid();

        // Act
        var result = await service.GetAsync<TestData>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Remove_Success()
    {
        // Arrange
        var service = new RedisCacheService(_logger, _fallbackCache, _redis);
        var key = "remove-key-" + Guid.NewGuid();
        var value = new TestData { Name = "Test" };

        await service.SetAsync(key, value, TimeSpan.FromMinutes(1));

        // Act
        await service.RemoveAsync(key);
        var result = await service.GetAsync<TestData>(key);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task RemovePattern_Success()
    {
        // Arrange
        var service = new RedisCacheService(_logger, _fallbackCache, _redis);
        var prefix = "pattern-" + Guid.NewGuid();
        var key1 = $"{prefix}:1";
        var key2 = $"{prefix}:2";
        var value = new TestData { Name = "Test" };

        await service.SetAsync(key1, value, TimeSpan.FromMinutes(1));
        await service.SetAsync(key2, value, TimeSpan.FromMinutes(1));

        // Act
        await service.RemovePatternAsync($"{prefix}:*");
        var result1 = await service.GetAsync<TestData>(key1);
        var result2 = await service.GetAsync<TestData>(key2);

        // Assert
        Assert.Null(result1);
        Assert.Null(result2);
    }

    [Fact]
    public async Task Get_RedisUnavailable_FallbackToMemory()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.IsConnected).Returns(false);

        var service = new RedisCacheService(_logger, _fallbackCache, mockRedis.Object);
        var key = "fallback-key-" + Guid.NewGuid();
        var value = new TestData { Name = "TestFallback" };

        // Set in memory cache directly (as service.SetAsync would also try Redis first)
        await _fallbackCache.SetAsync(key, value, TimeSpan.FromMinutes(1));

        // Act
        var result = await service.GetAsync<TestData>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Name, result.Name);
    }

    [Fact]
    public async Task Set_RedisUnavailable_UsesFallback()
    {
        // Arrange
        var mockRedis = new Mock<IConnectionMultiplexer>();
        mockRedis.Setup(r => r.IsConnected).Returns(false);

        var service = new RedisCacheService(_logger, _fallbackCache, mockRedis.Object);
        var key = "set-fallback-key-" + Guid.NewGuid();
        var value = new TestData { Name = "SetFallback" };

        // Act
        await service.SetAsync(key, value, TimeSpan.FromMinutes(1));
        var result = await _fallbackCache.GetAsync<TestData>(key);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(value.Name, result.Name);
    }

    private class TestData
    {
        public string Name { get; set; } = string.Empty;
    }
}
