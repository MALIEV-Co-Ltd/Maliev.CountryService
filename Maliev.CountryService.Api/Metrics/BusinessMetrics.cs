using Prometheus;

namespace Maliev.CountryService.Api.Metrics;

public static class BusinessMetrics
{
    // Cache metrics
    public static readonly Counter CacheHits = Prometheus.Metrics.CreateCounter(
        "country_cache_hits_total",
        "Total number of cache hits",
        new CounterConfiguration { LabelNames = new[] { "cache_type" } });

    public static readonly Counter CacheMisses = Prometheus.Metrics.CreateCounter(
        "country_cache_misses_total",
        "Total number of cache misses",
        new CounterConfiguration { LabelNames = new[] { "cache_type" } });

    // Request duration metrics
    public static readonly Histogram RequestDuration = Prometheus.Metrics.CreateHistogram(
        "country_request_duration_seconds",
        "Request duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "endpoint", "method", "status_code" },
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
        });

    // CRUD operation metrics
    public static readonly Counter CreateOperations = Prometheus.Metrics.CreateCounter(
        "country_create_operations_total",
        "Total number of country create operations",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    public static readonly Counter UpdateOperations = Prometheus.Metrics.CreateCounter(
        "country_update_operations_total",
        "Total number of country update operations",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    public static readonly Counter DeleteOperations = Prometheus.Metrics.CreateCounter(
        "country_delete_operations_total",
        "Total number of country delete operations",
        new CounterConfiguration { LabelNames = new[] { "status", "type" } });

    // Active country count gauge
    public static readonly Gauge ActiveCountryCount = Prometheus.Metrics.CreateGauge(
        "country_active_total",
        "Total number of active countries in the database");

    // Circuit breaker state
    public static readonly Gauge CircuitBreakerState = Prometheus.Metrics.CreateGauge(
        "country_circuit_breaker_state",
        "Circuit breaker state (0=Closed, 1=Open, 2=Half-Open)",
        new GaugeConfiguration { LabelNames = new[] { "dependency" } });

    // Bulk import metrics
    public static readonly Counter BulkImportJobs = Prometheus.Metrics.CreateCounter(
        "country_bulk_import_jobs_total",
        "Total number of bulk import jobs",
        new CounterConfiguration { LabelNames = new[] { "status" } });

    public static readonly Histogram BulkImportDuration = Prometheus.Metrics.CreateHistogram(
        "country_bulk_import_duration_seconds",
        "Bulk import job duration in seconds",
        new HistogramConfiguration
        {
            LabelNames = new[] { "status" },
            Buckets = Histogram.ExponentialBuckets(1, 2, 10) // 1s to ~1000s
        });
}
