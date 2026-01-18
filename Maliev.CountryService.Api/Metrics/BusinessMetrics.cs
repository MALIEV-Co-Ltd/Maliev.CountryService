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
    /// <summary>
    /// Records a cache hit for the specified cache type.
    /// </summary>
    /// <param name="cacheType">The type of cache (e.g., "memory", "redis").</param>
    public void RecordCacheHit(string cacheType)
    {
        _cacheHits.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records a cache miss for the specified cache type.
    /// </summary>
    /// <param name="cacheType">The type of cache (e.g., "memory", "redis").</param>
    public void RecordCacheMiss(string cacheType)
    {
        _cacheMisses.Add(1, new KeyValuePair<string, object?>("cache_type", cacheType), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    // Request duration
    /// <summary>
    /// Records the duration of an HTTP request.
    /// </summary>
    /// <param name="durationSeconds">The duration in seconds.</param>
    /// <param name="endpoint">The API endpoint name.</param>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <param name="statusCode">The HTTP status code.</param>
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
    /// <summary>
    /// Records a country creation operation.
    /// </summary>
    /// <param name="status">The operation status (e.g., "success", "failure").</param>
    public void RecordCreateOperation(string status)
    {
        _createOperations.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records a country update operation.
    /// </summary>
    /// <param name="status">The operation status (e.g., "success", "failure").</param>
    public void RecordUpdateOperation(string status)
    {
        _updateOperations.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records a country deletion operation.
    /// </summary>
    /// <param name="status">The operation status (e.g., "success", "failure").</param>
    /// <param name="type">The deletion type ("soft" or "hard").</param>
    public void RecordDeleteOperation(string status, string type)
    {
        _deleteOperations.Add(1,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("type", type),
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("environment", _environment));
    }

    // Gauge setters
    /// <summary>
    /// Sets the current count of active countries in the database.
    /// </summary>
    /// <param name="count">The number of active countries.</param>
    public void SetActiveCountryCount(long count)
    {
        Interlocked.Exchange(ref _activeCountryCount, count);
    }

    /// <summary>
    /// Sets the circuit breaker state for the specified dependency.
    /// </summary>
    /// <param name="state">The circuit breaker state (0=Closed, 1=Open, 2=Half-Open).</param>
    /// <param name="dependency">The name of the dependency (e.g., "database", "redis").</param>
    public void SetCircuitBreakerState(long state, string dependency)
    {
        // Note: dependency label not supported in current ObservableGauge implementation
        // Would need separate gauge per dependency or restructure
        Interlocked.Exchange(ref _circuitBreakerState, state);
    }

    // Bulk import metrics
    /// <summary>
    /// Records a bulk import job execution.
    /// </summary>
    /// <param name="status">The job status (e.g., "success", "failure", "validation_error").</param>
    public void RecordBulkImportJob(string status)
    {
        _bulkImportJobs.Add(1, new KeyValuePair<string, object?>("status", status), new KeyValuePair<string, object?>("service", _serviceName), new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Records the duration of a bulk import job.
    /// </summary>
    /// <param name="durationSeconds">The duration in seconds.</param>
    /// <param name="status">The job status (e.g., "success", "failure").</param>
    public void RecordBulkImportDuration(double durationSeconds, string status)
    {
        _bulkImportDuration.Record(durationSeconds,
            new KeyValuePair<string, object?>("status", status),
            new KeyValuePair<string, object?>("service", _serviceName),
            new KeyValuePair<string, object?>("environment", _environment));
    }

    /// <summary>
    /// Disposes the metrics meter and releases resources.
    /// </summary>
    public void Dispose()
    {
        _meter?.Dispose();
        GC.SuppressFinalize(this);
    }
}
