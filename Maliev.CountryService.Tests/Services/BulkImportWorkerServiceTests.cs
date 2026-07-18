using Maliev.CountryService.Api.BackgroundServices;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Domain.Entities;
using Maliev.CountryService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Testcontainers.PostgreSql;
using Xunit;

namespace Maliev.CountryService.Tests.Services;

public class BulkImportWorkerServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly Mock<ILogger<BulkImportWorkerService>> _loggerMock;
    private readonly Mock<IBulkImportService> _bulkImportServiceMock;

    public BulkImportWorkerServiceTests()
    {
#pragma warning disable CS0618
        _postgresContainer = new PostgreSqlBuilder().WithImage("postgres:18-alpine")
            .Build();
#pragma warning restore CS0618
        _loggerMock = new Mock<ILogger<BulkImportWorkerService>>();
        _bulkImportServiceMock = new Mock<IBulkImportService>();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<CountryDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        using var dbContext = new CountryDbContext(options);
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    private IServiceScopeFactory CreateScopeFactory()
    {
        var services = new ServiceCollection();
        services.AddDbContext<CountryDbContext>(options =>
            options.UseNpgsql(_postgresContainer.GetConnectionString()));
        services.AddScoped<ICountryDbContext>(sp => sp.GetRequiredService<CountryDbContext>());
        services.AddSingleton(_bulkImportServiceMock.Object);
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IServiceScopeFactory>();
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesValidatedJob()
    {
        // Arrange
        var scopeFactory = CreateScopeFactory();
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
            var job = new BulkImportJob
            {
                Status = "Validated",
                TotalRecords = 1,
                CreatedAtUtc = DateTime.UtcNow,
                UserId = "test-user",
                CreatedBy = "test-user",
                ValidationErrors = "[]"
            };
            context.BulkImportJobs.Add(job);
            await context.SaveChangesAsync();
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["BulkImport:PollIntervalSeconds"] = "1" })
            .Build();
#pragma warning restore CS0618
        var service = new BulkImportWorkerService(scopeFactory, _loggerMock.Object, config);

        // Act
        var cts = new CancellationTokenSource();
        var executeTask = service.StartAsync(cts.Token);

        // Wait for processing
        await Task.Delay(1000);

        cts.Cancel();
        try { await executeTask; } catch (OperationCanceledException) { }

        // Assert
        using (var scope = scopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<CountryDbContext>();
            var updatedJob = await context.BulkImportJobs.FirstOrDefaultAsync();
            Assert.NotNull(updatedJob);
            Assert.Equal("Processing", updatedJob.Status);
        }

        _bulkImportServiceMock.Verify(x => x.ProcessImportAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}



