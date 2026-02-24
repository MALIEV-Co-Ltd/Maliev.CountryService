using Maliev.CountryService.Api.Metrics;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Maliev.CountryService.Tests.Metrics;

public class BusinessMetricsTests
{
    [Fact]
    public void BusinessMetrics_CanRecordAllMetrics()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["ASPNETCORE_ENVIRONMENT"]).Returns("Testing");
        var metrics = new BusinessMetrics(configMock.Object);

        // Act & Assert (No exceptions should occur)
        metrics.RecordCacheHit("redis");
        metrics.RecordCacheMiss("memory");
        metrics.RecordRequestDuration(0.1, "GetById", "GET", "200");
        metrics.RecordCreateOperation("success");
        metrics.RecordUpdateOperation("failure");
        metrics.RecordDeleteOperation("success", "soft");
        metrics.SetActiveCountryCount(100);
        metrics.SetCircuitBreakerState(1, "redis");
        metrics.RecordBulkImportJob("completed");
        metrics.RecordBulkImportDuration(5.0, "success");

        metrics.Dispose();
    }
}
