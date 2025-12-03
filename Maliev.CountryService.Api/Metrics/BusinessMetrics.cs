using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Maliev.CountryService.Api.Metrics;

/// <summary>
/// Provides OpenTelemetry metrics for business operations in the Country Service.
/// </summary>
public class BusinessMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly string _serviceName;
    private readonly string _environment;

    // Counters (monotonic - only increase)
    private readonly Counter<long> _cacheHits;
    private readonly Counter<long> _cacheMisses;
    private readonly Counter<long> _createOperations;
    private readonly Counter<long> _updateOperations;
    private readonly Counter<long> _deleteOperations;
    private readonly Counter<long> _bulkImportJobs;

    // Histograms (for distributions)
    private readonly Histogram<double> _requestDuration;
    private readonly Histogram<double> _bulkImportDuration;

    // Gauges (using ObservableGauge with state tracking)
    private long _activeCountryCount;
    private long _circuitBreakerState;

    /// <summary>
    /// Initializes a new instance of the BusinessMetrics class
    /// </summary>
    /// <param name="configuration">Application configuration</param>
    public BusinessMetrics(IConfiguration configuration)
    {
        _serviceName = "country-service";
        _environment = configuration["Environment"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? "development";

        _meter = new Meter(_serviceName, "1.0.0");

        // Initialize counters
        _cacheHits = _meter.CreateCounter<long>(
            "country.cache.hits",
            description: "Total number of cache hits");

        _cacheMisses = _meter.CreateCounter<long>(
            "country.cache.misses",
            description: "Total number of cache misses");

        _createOperations = _meter.CreateCounter<long>(
            "country.create.operations",
            description: "Total number of country create operations");

        _updateOperations = _meter.CreateCounter<long>(
            "country.update.operations",
            description: "Total number of country update operations");

        _deleteOperations = _meter.CreateCounter<long>(
            "country.delete.operations",
            description: "Total number of country delete operations");

        _bulkImportJobs = _meter.CreateCounter<long>(
            "country.bulk.import.jobs",
            description: "Total number of bulk import jobs");

        // Initialize histograms
        _requestDuration = _meter.CreateHistogram<double>(
            "country.request.duration",
            unit: "s",
            description: "Request duration in seconds");

        _bulkImportDuration = _meter.CreateHistogram<double>(
            "country.bulk.import.duration",
            unit: "s",
            description: "Bulk import job duration in seconds");

        // Initialize observable gauges
        _meter.CreateObservableGauge(
            "country.active.total",
            () => new Measurement<long>(_activeCountryCount, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Total number of active countries in the database");

        _meter.CreateObservableGauge(
            "country.circuit.breaker.state",
            () => new Measurement<long>(_circuitBreakerState, new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment)),
            description: "Circuit breaker state (0=Closed, 1=Open, 2=Half-Open)");
    }

    // Cache metrics
    public void RecordCacheHit(string cacheType)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    public void RecordCacheMiss(string cacheType)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    // Request duration
    public void RecordRequestDuration(double durationSeconds, string endpoint, string method, string statusCode)
    {
        _requestDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status_code", statusCode),
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("environment", _environment));
    }

    // Operation metrics
    public void RecordCreateOperation(string status)
    {
        _createOperations.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    public void RecordUpdateOperation(string status)
    {
        _updateOperations.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    public void RecordDeleteOperation(string status, string type)
    {
        _deleteOperations.Add(1,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("environment", _environment));
    }

    // Gauge setters
    public void SetActiveCountryCount(long count)
    {
        Interlocked.Exchange(ref _activeCountryCount, count);
    }

    public void SetCircuitBreakerState(long state, string dependency)
    {
        // Note: dependency label not supported in current ObservableGauge implementation
        // Would need separate gauge per dependency or restructure
        Interlocked.Exchange(ref _circuitBreakerState, state);
    }

    // Bulk import metrics
    public void RecordBulkImportJob(string status)
    {
        _bulkImportJobs.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    public void RecordBulkImportDuration(double durationSeconds, string status)
    {
        _bulkImportDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("environment", _environment));
    }

    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
