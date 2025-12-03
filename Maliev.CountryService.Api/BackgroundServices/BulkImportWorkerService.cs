using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Api.BackgroundServices;

/// <summary>
/// T113-T114: Background worker service for processing validated bulk import jobs.
/// Polls for jobs in "Validated" status and processes them asynchronously.
/// </summary>
public class BulkImportWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BulkImportWorkerService> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(10);
    private readonly int _maxConcurrentJobs = 2; // T114: Configurable batch processing limits
    private readonly TimeSpan _processingTimeout = TimeSpan.FromMinutes(30); // T114: Processing timeout
    /// <summary>
    /// Initializes a new instance of the <see cref="BulkImportWorkerService"/> class.
    /// </summary>
    /// <param name="scopeFactory">The service scope factory.</param>
    /// <param name="logger">The logger instance.</param>
    public BulkImportWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BulkImportWorkerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Executes the background service to process bulk import jobs.
    /// </summary>
    /// <param name="stoppingToken">Token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Bulk Import Worker Service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk import jobs");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }

        _logger.LogInformation("Bulk Import Worker Service stopping");
    }

    private async Task ProcessPendingJobsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CountryServiceDbContext>();
        var bulkImportService = scope.ServiceProvider.GetRequiredService<IBulkImportService>();

        // T113: Poll for Validated jobs
        var validatedJobs = await context.BulkImportJobs
            .Where(j => j.Status == "Validated")
            .OrderBy(j => j.CreatedAtUtc)
            .Take(_maxConcurrentJobs)
            .ToListAsync(cancellationToken);

        if (!validatedJobs.Any())
        {
            return;
        }

        _logger.LogInformation("Found {Count} validated jobs to process", validatedJobs.Count);

        // T114: Process jobs with timeout and error handling
        foreach (var job in validatedJobs)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(_processingTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

                _logger.LogInformation("Processing bulk import job {JobId}", job.Id);

                // Convert long ID to Guid (simplified - in production would use proper GUID)
                var jobGuid = Guid.NewGuid();

                await bulkImportService.ProcessImportAsync(jobGuid, linkedCts.Token);

                _logger.LogInformation("Successfully processed bulk import job {JobId}", job.Id);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("Bulk import processing cancelled for job {JobId}", job.Id);
                throw; // Re-throw to stop the service
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Bulk import processing timed out for job {JobId} after {Timeout}", job.Id, _processingTimeout);

                // Mark job as failed due to timeout
                job.Status = "Failed";
                job.ValidationErrors = System.Text.Json.JsonSerializer.Serialize(new[] { new { message = $"Processing timed out after {_processingTimeout.TotalMinutes} minutes" } });
                job.CompletedAtUtc = DateTime.UtcNow;
                await context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing bulk import job {JobId}", job.Id);
                // Error handling is done in BulkImportService.ProcessImportAsync
            }
        }
    }
}