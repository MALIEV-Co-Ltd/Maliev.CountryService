using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Infrastructure.Data;
using Maliev.CountryService.Infrastructure.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Middleware;

public class MiddlewareTests : IAsyncLifetime
{
    private readonly Testcontainers.PostgreSql.PostgreSqlContainer _postgresContainer;

    public MiddlewareTests()
    {
        _postgresContainer = new Testcontainers.PostgreSql.PostgreSqlBuilder("postgres:18-alpine")
            .Build();
    }

    public async Task InitializeAsync() => await _postgresContainer.StartAsync();
    public async Task DisposeAsync() => await _postgresContainer.DisposeAsync();

    private CountryDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<CountryDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;
        var context = new CountryDbContext(options);
        context.Database.Migrate();
        return context;
    }

    [Fact]
    public async Task DegradationHeaderMiddleware_AddsHeaders_WhenDegraded()
    {
        // Arrange
        var nextCalled = false;
        RequestDelegate next = (ctx) => { nextCalled = true; return Task.CompletedTask; };
        var loggerMock = new Mock<ILogger<DegradationHeaderMiddleware>>();
        var middleware = new DegradationHeaderMiddleware(next, loggerMock.Object);
        var context = new DefaultHttpContext();
        var degradationContext = new DegradationContext { IsDegraded = true, DegradationReason = "Redis down" };

        // Act
        await middleware.InvokeAsync(context, degradationContext);

        // Assert
        Assert.True(nextCalled);
        Assert.Equal("true", context.Response.Headers["X-Degraded-Mode"]);
        Assert.Equal("Redis down", context.Response.Headers["X-Degradation-Reason"]);
    }

    [Fact]
    public async Task PermissionDenialLoggingMiddleware_Logs_WhenForbidden()
    {
        // Arrange
        RequestDelegate next = (ctx) => { ctx.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; };
        var middleware = new PermissionDenialLoggingMiddleware(next);
        var context = new DefaultHttpContext();

        using var dbContext = CreateDbContext();
        var loggerMock = new Mock<ILogger<PermissionDenialLoggingMiddleware>>();

        // Act
        await middleware.InvokeAsync(context, dbContext, loggerMock.Object);

        // Assert
        var logs = await dbContext.AuditLogs.ToListAsync();
        Assert.Single(logs);
        Assert.Equal("ACCESS_DENIED", logs[0].Action);
    }
}
