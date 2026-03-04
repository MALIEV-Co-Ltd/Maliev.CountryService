using Maliev.CountryService.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Api.BackgroundServices;

/// <summary>
/// Background worker service for processing validated bulk import jobs.
/// Polls for jobs in "Validated" status and processes them asynchronously.
/// </summary>
public class BulkImportWorkerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BulkImportWorkerService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _processingTimeout = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Initializes a new instance of the BulkImportWorkerService class.
    /// </summary>
    public BulkImportWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<BulkImportWorkerService> logger,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var pollSeconds = configuration.GetValue<int>("BulkImport:PollIntervalSeconds", 10);
        _pollInterval = TimeSpan.FromSeconds(pollSeconds);
    }

    /// <summary>
    /// Executes the background service to process bulk import jobs.
    /// </summary>
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
        var context = scope.ServiceProvider.GetRequiredService<ICountryDbContext>();
        var bulkImportService = scope.ServiceProvider.GetRequiredService<IBulkImportService>();

        var claimId = Guid.NewGuid();
        var claimedCount = await context.BulkImportJobs
            .Where(j => j.Status == "Validated")
            .OrderBy(j => j.CreatedAtUtc)
            .Take(1)
            .ExecuteUpdateAsync(s => s
                .SetProperty(j => j.Status, "Processing")
                .SetProperty(j => j.StartedAtUtc, DateTime.UtcNow)
                .SetProperty(j => j.ClaimedByWorkerId, claimId),
                cancellationToken);

        if (claimedCount == 0)
        {
            return;
        }

        // Retrieve the claimed job by claim ID
        var job = await context.BulkImportJobs
            .FirstOrDefaultAsync(j => j.ClaimedByWorkerId == claimId, cancellationToken);

        if (job == null)
        {
            return;
        }

        _logger.LogInformation("Claimed validated job {JobId} for processing", job.Id);

        try
        {
            using var timeoutCts = new CancellationTokenSource(_processingTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            _logger.LogInformation("Processing bulk import job {JobId}", job.Id);

            await bulkImportService.ProcessImportAsync(job.Id, linkedCts.Token);

            _logger.LogInformation("Successfully processed bulk import job {JobId}", job.Id);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Bulk import processing cancelled for job {JobId}", job.Id);
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Bulk import processing timed out for job {JobId} after {Timeout}", job.Id, _processingTimeout);

            job.Status = "Failed";
            job.ValidationErrors = System.Text.Json.JsonSerializer.Serialize(new[] { new { message = $"Processing timed out after {_processingTimeout.TotalMinutes} minutes" } });
            job.CompletedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk import job {JobId}", job.Id);
        }
    }
}
