using Maliev.CountryService.Api.Models.BulkImport;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Data;
using Maliev.CountryService.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// T108: Bulk import service implementation with two-phase validation and processing.
/// </summary>
public partial class BulkImportService : IBulkImportService
{
    private readonly CountryDbContext _context;
    private readonly ICountryService _countryService;
    private readonly ILogger<BulkImportService> _logger;

    [GeneratedRegex("^[A-Z]{2}$")]
    private static partial Regex Iso2Regex();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex Iso3Regex();

    [GeneratedRegex("^[0-9]{3}$")]
    private static partial Regex NumericCodeRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkImportService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="countryService">The country service for CRUD operations.</param>
    /// <param name="logger">The logger instance.</param>
    public BulkImportService(
        CountryDbContext context,
        ICountryService countryService,
        ILogger<BulkImportService> logger)
    {
        _context = context;
        _countryService = countryService;
        _logger = logger;
    }

    private List<string> ValidateCountryRequest(CreateCountryRequest country)
    {
        var errors = new List<string>();

        // ISO2 validation
        if (string.IsNullOrWhiteSpace(country.Iso2))
            errors.Add("ISO2 code is required");
        else if (country.Iso2.Length != 2)
            errors.Add("ISO2 code must be exactly 2 characters");
        else if (!Iso2Regex().IsMatch(country.Iso2))
            errors.Add("ISO2 code must be uppercase letters only");

        // ISO3 validation
        if (string.IsNullOrWhiteSpace(country.Iso3))
            errors.Add("ISO3 code is required");
        else if (country.Iso3.Length != 3)
            errors.Add("ISO3 code must be exactly 3 characters");
        else if (!Iso3Regex().IsMatch(country.Iso3))
            errors.Add("ISO3 code must be uppercase letters only");

        // Name validation
        if (string.IsNullOrWhiteSpace(country.Name))
            errors.Add("Country name is required");
        else if (country.Name.Length > 100)
            errors.Add("Country name must not exceed 100 characters");

        // Optional field validations
        if (!string.IsNullOrWhiteSpace(country.OfficialName) && country.OfficialName.Length > 200)
            errors.Add("Official name must not exceed 200 characters");

        if (!string.IsNullOrWhiteSpace(country.NumericCode) &&
            (country.NumericCode.Length != 3 || !NumericCodeRegex().IsMatch(country.NumericCode)))
            errors.Add("Numeric code must be exactly 3 digits");

        if (country.Latitude.HasValue && (country.Latitude.Value < -90 || country.Latitude.Value > 90))
            errors.Add("Latitude must be between -90 and 90");

        if (country.Longitude.HasValue && (country.Longitude.Value < -180 || country.Longitude.Value > 180))
            errors.Add("Longitude must be between -180 and 180");

        if (country.AreaKm2.HasValue && country.AreaKm2.Value <= 0)
            errors.Add("Area must be greater than 0");

        if (country.Population.HasValue && country.Population.Value < 0)
            errors.Add("Population cannot be negative");

        if (country.GiniCoefficient.HasValue && (country.GiniCoefficient.Value < 0 || country.GiniCoefficient.Value > 100))
            errors.Add("Gini coefficient must be between 0 and 100");

        return errors;
    }

    /// <summary>
    /// T109: Validate import - check duplicates within batch and against database.
    /// T111: Duplicate detection using HashSet for within-batch and database query for existing.
    /// </summary>
    public async Task<BulkImportStatusResponse> ValidateImportAsync(BulkImportRequest request, string userId, CancellationToken cancellationToken = default)
    {
        // T108: Create BulkImportJob entity
        var job = new BulkImportJob
        {
            Status = "Validating",
            TotalRecords = request.Countries.Count,
            ProcessedRecords = 0,
            CreatedBy = userId,
            CreatedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow,
            ValidationErrors = "[]",
            PayloadData = JsonSerializer.Serialize(request) // Persist data for async processing
        };

        _context.BulkImportJobs.Add(job);
        await _context.SaveChangesAsync(cancellationToken);

        var errors = new List<ValidationErrorResponse>();

        // T111: Track ISO codes within batch for duplicate detection
        var iso2Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var iso3Seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // T111: Get all existing ISO codes from database in one query
        var existingIso2Codes = await _context.Countries
            .Select(c => c.Iso2.ToUpper())
            .ToListAsync(cancellationToken);

        var existingIso3Codes = await _context.Countries
            .Select(c => (c.Iso3 ?? "").ToUpper())
            .ToListAsync(cancellationToken);

        var existingIso2Set = new HashSet<string>(existingIso2Codes, StringComparer.OrdinalIgnoreCase);
        var existingIso3Set = new HashSet<string>(existingIso3Codes, StringComparer.OrdinalIgnoreCase);

        // T109: Validate each record
        for (int i = 0; i < request.Countries.Count; i++)
        {
            var country = request.Countries[i];
            if (country == null) continue;

            var rowNumber = i + 1;

            // Manual validation check
            var validationErrors = ValidateCountryRequest(country);
            if (validationErrors.Any())
            {
                foreach (var error in validationErrors)
                {
                    errors.Add(new ValidationErrorResponse
                    {
                        RowNumber = rowNumber,
                        Field = "Request",
                        Message = error
                    });
                }
                continue; // Skip duplicate checks if basic validation failed
            }

            // T111: Check for within-batch duplicates
            var iso2Upper = (country.Iso2 ?? "").ToUpperInvariant();
            var iso3Upper = (country.Iso3 ?? "").ToUpperInvariant();

            if (iso2Seen.Contains(iso2Upper))
            {
                errors.Add(new ValidationErrorResponse
                {
                    RowNumber = rowNumber,
                    Field = "Iso2",
                    Message = $"Duplicate ISO2 code '{country.Iso2}' within batch"
                });
            }
            else
            {
                iso2Seen.Add(iso2Upper);
            }

            if (iso3Seen.Contains(iso3Upper))
            {
                errors.Add(new ValidationErrorResponse
                {
                    RowNumber = rowNumber,
                    Field = "Iso3",
                    Message = $"Duplicate ISO3 code '{country.Iso3}' within batch"
                });
            }
            else
            {
                iso3Seen.Add(iso3Upper);
            }

            // T111: Check for database duplicates
            if (existingIso2Set.Contains(iso2Upper))
            {
                errors.Add(new ValidationErrorResponse
                {
                    RowNumber = rowNumber,
                    Field = "Iso2",
                    Message = $"Country with ISO2 code '{country.Iso2}' already exists in database"
                });
            }

            if (existingIso3Set.Contains(iso3Upper))
            {
                errors.Add(new ValidationErrorResponse
                {
                    RowNumber = rowNumber,
                    Field = "Iso3",
                    Message = $"Country with ISO3 code '{country.Iso3}' already exists in database"
                });
            }
        }

        // Update job status
        if (errors.Any())
        {
            job.Status = "ValidationFailed";
            job.ValidationErrors = JsonSerializer.Serialize(errors);
            job.FailedRecords = errors.Select(e => e.RowNumber).Distinct().Count();
        }
        else
        {
            job.Status = "Validated";
        }

        // T226: Do NOT set CompletedAtUtc here as the job is only validated, not finished.
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Bulk import validation completed: JobId {JobId}, Status {Status}, Errors {ErrorCount}",
            job.Id, job.Status, errors.Count);

        return MapToStatusResponse(job, errors);
    }

    /// <summary>
    /// T110: Process validated job with atomic transaction.
    /// T112: Atomic cache invalidation after bulk import.
    /// </summary>
    public async Task<BulkImportStatusResponse> ProcessImportAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var longId = GetLongIdFromGuid(jobId);
        var job = await _context.BulkImportJobs
            .FirstOrDefaultAsync(j => j.Id == longId, cancellationToken);

        if (job == null)
        {
            throw new KeyNotFoundException($"Bulk import job {jobId} not found");
        }

        if (job.Status != "Validated" && job.Status != "Processing")
        {
            throw new InvalidOperationException($"Job {jobId} is in status '{job.Status}', cannot process. Only 'Validated' jobs can be processed.");
        }

        if (job.Status == "Validated")
        {
            job.Status = "Processing";
            job.StartedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        try
        {
            if (string.IsNullOrEmpty(job.PayloadData))
            {
                throw new InvalidOperationException("Job payload data is missing");
            }

            var request = JsonSerializer.Deserialize<BulkImportRequest>(job.PayloadData);
            if (request == null || request.Countries == null)
            {
                throw new InvalidOperationException("Failed to deserialize job payload data");
            }

            // T110: Process validated job with atomic transaction using execution strategy
            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                // T110: Begin transaction for atomic insert
                using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var countriesToInsert = request.Countries.Select(c => new Data.Entities.Country
                    {
                        Iso2 = c.Iso2.ToUpperInvariant(),
                        Iso3 = c.Iso3?.ToUpperInvariant(),
                        Name = c.Name,
                        OfficialName = c.OfficialName,
                        NumericCode = c.NumericCode,
                        Capital = c.Capital,
                        Region = c.Region,
                        Subregion = c.Subregion,
                        Latitude = (double?)c.Latitude,
                        Longitude = (double?)c.Longitude,
                        Demonym = c.Demonym,
                        AreaKm2 = (double?)c.AreaKm2,
                        Population = c.Population,
                        GiniCoefficient = (double?)c.GiniCoefficient,
                        Timezones = c.Timezones ?? "[]",
                        Borders = c.Borders ?? "[]",
                        CallingCodes = c.CallingCodes ?? "[]",
                        TopLevelDomains = c.TopLevelDomains ?? "[]",
                        Currencies = c.Currencies ?? "{}",
                        Languages = c.Languages ?? "{}",
                        Translations = c.Translations ?? "{}",
                        Flags = c.Flags ?? "{}",
                        CoatOfArms = c.CoatOfArms ?? "{}",
                        Independent = c.Independent ?? false,
                        UnMember = c.UnMember ?? false,
                        Landlocked = c.Landlocked ?? false,
                        IsActive = true,
                        CreatedAtUtc = DateTime.UtcNow,
                        LastModifiedUtc = DateTime.UtcNow,
                        CreatedBy = job.CreatedBy,
                        UpdatedBy = job.CreatedBy,
                        Version = Guid.NewGuid()
                    }).ToList();

                    _context.Countries.AddRange(countriesToInsert);
                    await _context.SaveChangesAsync(cancellationToken);

                    job.ProcessedRecords = job.TotalRecords;
                    job.Status = "Completed";
                    job.CompletedAtUtc = DateTime.UtcNow;

                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    _logger.LogInformation("Bulk import processing completed: JobId {JobId}, Records {Count}",
                        job.Id, job.TotalRecords);

                    // T112: Invalidate all list and search caches
                    await _countryService.InvalidateListCachesAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bulk import processing failed: JobId {JobId}", job.Id);

            job.Status = "Failed";
            job.CompletedAtUtc = DateTime.UtcNow;
            job.ValidationErrors = JsonSerializer.Serialize(new[] { new { message = ex.Message } });
            await _context.SaveChangesAsync(cancellationToken);

            throw;
        }

        return MapToStatusResponse(job, new List<ValidationErrorResponse>());
    }

    /// <summary>
    /// Get status of a bulk import job.
    /// </summary>
    public async Task<BulkImportStatusResponse?> GetJobStatusAsync(Guid jobId, CancellationToken cancellationToken = default)
    {
        var longId = GetLongIdFromGuid(jobId);
        var job = await _context.BulkImportJobs
            .FirstOrDefaultAsync(j => j.Id == longId, cancellationToken);

        if (job == null)
        {
            return null;
        }

        var errors = string.IsNullOrEmpty(job.ValidationErrors) || job.ValidationErrors == "[]"
            ? new List<ValidationErrorResponse>()
            : JsonSerializer.Deserialize<List<ValidationErrorResponse>>(job.ValidationErrors) ?? new List<ValidationErrorResponse>();

        return MapToStatusResponse(job, errors);
    }

    private BulkImportStatusResponse MapToStatusResponse(BulkImportJob job, List<ValidationErrorResponse> errors)
    {
        // Calculate duration if both timestamps are available
        long? durationMs = null;
        if (job.CompletedAtUtc.HasValue && job.StartedAtUtc.HasValue)
        {
            durationMs = (long)(job.CompletedAtUtc.Value - job.StartedAtUtc.Value).TotalMilliseconds;
        }

        return new BulkImportStatusResponse
        {
            JobId = CreateGuidFromLongId(job.Id),
            Status = job.Status,
            TotalRecords = job.TotalRecords,
            ProcessedRecords = job.ProcessedRecords,
            ValidationErrors = errors,
            CreatedAtUtc = job.CreatedAtUtc,
            StartedAtUtc = job.StartedAtUtc,
            CompletedAtUtc = job.CompletedAtUtc,
            DurationMs = durationMs
        };
    }

    private Guid CreateGuidFromLongId(long id)
    {
        // Create deterministic GUID from long ID
        byte[] bytes = new byte[16];
        byte[] idBytes = BitConverter.GetBytes(id);
        Array.Copy(idBytes, 0, bytes, 0, idBytes.Length);
        return new Guid(bytes);
    }

    private long GetLongIdFromGuid(Guid guid)
    {
        // Extract long ID from GUID
        byte[] bytes = guid.ToByteArray();
        return BitConverter.ToInt64(bytes, 0);
    }
}
