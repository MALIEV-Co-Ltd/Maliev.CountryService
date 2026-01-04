using Maliev.CountryService.Data;
using Maliev.CountryService.Tests.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Maliev.CountryService.Tests;

public class TestWebApplicationFactory : BaseIntegrationTestFactory<Program, CountryDbContext>
{
    protected override void ConfigureEnvironmentVariables()
    {
        base.ConfigureEnvironmentVariables();
    }

    protected override void ConfigureAdditionalServices(IServiceCollection services)
    {
        // Remove domain background services by default to prevent interference with regular tests
        // Infrastructure background services (MassTransit, IAM registration) are kept to ensure health checks pass
        var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
        foreach (var service in hostedServices)
        {
            var typeName = service.ImplementationType?.Name ??
                          service.ImplementationFactory?.Method.ReturnType.Name ?? "";

            if (typeName.Contains("CacheWarmingService") ||
                typeName.Contains("BulkImportWorkerService"))
            {
                services.Remove(service);
            }
        }
    }
}

/// <summary>
/// Test factory with background services enabled for testing CacheWarmingService and BulkImportWorkerService
/// </summary>
public class TestWebApplicationFactoryWithBackgroundServices : BaseIntegrationTestFactory<Program, CountryDbContext>
{
    // This factory keeps background services enabled for explicit background service tests
}
