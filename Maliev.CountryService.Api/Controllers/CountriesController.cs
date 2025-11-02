using Asp.Versioning;
using Maliev.CountryService.Api.Metrics;
using Maliev.CountryService.Api.Models.Countries;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// T061: CountriesController with base route /countries/v1
/// User Story 1: Fast country lookup by ISO code with sub-50ms latency
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("countries/v{version:apiVersion}/countries")]
[EnableRateLimiting("read-endpoints")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
    {
        _countryService = countryService;
        _logger = logger;
    }

    /// <summary>
    /// T062: GET /countries/v1/countries/{id}
    /// Returns 200 with ETag, 304 if If-None-Match matches, 404 if not found
    /// T065-T067: Add cache headers (X-Cache, X-Cache-Age, Last-Modified)
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(long id, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var country = await _countryService.GetByIdAsync(id, cancellationToken);

        if (country == null)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetById", "GET", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = $"Country with ID {id} not found" });
        }

        // T062: Check If-None-Match for 304 response
        var requestETag = Request.Headers["If-None-Match"].ToString();
        if (!string.IsNullOrEmpty(requestETag) && requestETag == country.ETag)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetById", "GET", "304").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        // T065: Add X-Cache header (HIT/MISS based on response time - heuristic)
        var cacheStatus = stopwatch.ElapsedMilliseconds < 10 ? "HIT" : "MISS";
        Response.Headers["X-Cache"] = cacheStatus;

        // T066: Add X-Cache-Age header (seconds since cached - estimated from last modified)
        var cacheAge = (int)(DateTime.UtcNow - country.LastModifiedUtc).TotalSeconds;
        Response.Headers["X-Cache-Age"] = cacheAge.ToString();

        // T067: Add Last-Modified header
        Response.Headers["Last-Modified"] = country.LastModifiedUtc.ToString("R");

        // Add ETag header
        Response.Headers["ETag"] = country.ETag;

        BusinessMetrics.RequestDuration.WithLabels("GetById", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
        return Ok(country);
    }

    /// <summary>
    /// T063: GET /countries/v1/countries/iso2/{iso2}
    /// Same behavior as GetById - validate ISO2 format
    /// </summary>
    [HttpGet("iso2/{iso2}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIso2(string iso2, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(iso2) || iso2.Length != 2)
        {
            return BadRequest(new { error = "INVALID_ISO2", message = "ISO2 code must be exactly 2 characters" });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var country = await _countryService.GetByIso2Async(iso2, cancellationToken);

        if (country == null)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetByIso2", "GET", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = $"Country with ISO2 code '{iso2}' not found" });
        }

        var requestETag = Request.Headers["If-None-Match"].ToString();
        if (!string.IsNullOrEmpty(requestETag) && requestETag == country.ETag)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetByIso2", "GET", "304").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var cacheStatus = stopwatch.ElapsedMilliseconds < 10 ? "HIT" : "MISS";
        Response.Headers["X-Cache"] = cacheStatus;
        Response.Headers["X-Cache-Age"] = ((int)(DateTime.UtcNow - country.LastModifiedUtc).TotalSeconds).ToString();
        Response.Headers["Last-Modified"] = country.LastModifiedUtc.ToString("R");
        Response.Headers["ETag"] = country.ETag;

        BusinessMetrics.RequestDuration.WithLabels("GetByIso2", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
        return Ok(country);
    }

    /// <summary>
    /// T064: GET /countries/v1/countries/iso3/{iso3}
    /// Same behavior as GetById - validate ISO3 format
    /// </summary>
    [HttpGet("iso3/{iso3}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIso3(string iso3, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(iso3) || iso3.Length != 3)
        {
            return BadRequest(new { error = "INVALID_ISO3", message = "ISO3 code must be exactly 3 characters" });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var country = await _countryService.GetByIso3Async(iso3, cancellationToken);

        if (country == null)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetByIso3", "GET", "404").Observe(stopwatch.Elapsed.TotalSeconds);
            return NotFound(new { error = "NOT_FOUND", message = $"Country with ISO3 code '{iso3}' not found" });
        }

        var requestETag = Request.Headers["If-None-Match"].ToString();
        if (!string.IsNullOrEmpty(requestETag) && requestETag == country.ETag)
        {
            BusinessMetrics.RequestDuration.WithLabels("GetByIso3", "GET", "304").Observe(stopwatch.Elapsed.TotalSeconds);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        var cacheStatus = stopwatch.ElapsedMilliseconds < 10 ? "HIT" : "MISS";
        Response.Headers["X-Cache"] = cacheStatus;
        Response.Headers["X-Cache-Age"] = ((int)(DateTime.UtcNow - country.LastModifiedUtc).TotalSeconds).ToString();
        Response.Headers["Last-Modified"] = country.LastModifiedUtc.ToString("R");
        Response.Headers["ETag"] = country.ETag;

        BusinessMetrics.RequestDuration.WithLabels("GetByIso3", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
        return Ok(country);
    }

    /// <summary>
    /// T072: GET /countries/v1/countries
    /// Returns paginated list of countries with filtering and sorting support.
    /// T073: Adds X-Total-Count header with total number of matching countries.
    /// T075: Supports If-Modified-Since conditional requests.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] CountryListRequest request, CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = await _countryService.ListAsync(request, cancellationToken);

        // T075: If-Modified-Since support - get max LastModifiedUtc from result set
        if (result.Data.Any())
        {
            var maxLastModified = result.Data.Max(c => c.LastModifiedUtc);

            if (Request.Headers.ContainsKey("If-Modified-Since"))
            {
                if (DateTime.TryParse(Request.Headers["If-Modified-Since"].ToString(), out var ifModifiedSince))
                {
                    if (maxLastModified <= ifModifiedSince)
                    {
                        BusinessMetrics.RequestDuration.WithLabels("List", "GET", "304").Observe(stopwatch.Elapsed.TotalSeconds);
                        return StatusCode(StatusCodes.Status304NotModified);
                    }
                }
            }

            Response.Headers["Last-Modified"] = maxLastModified.ToString("R");
        }

        // T073: Add X-Total-Count header
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();

        // Add cache headers
        var cacheStatus = stopwatch.ElapsedMilliseconds < 10 ? "HIT" : "MISS";
        Response.Headers["X-Cache"] = cacheStatus;

        BusinessMetrics.RequestDuration.WithLabels("List", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
        return Ok(result);
    }

    /// <summary>
    /// T074: GET /countries/v1/countries/search
    /// Full-text search on country names and ISO codes.
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        {
            return BadRequest(new { error = "INVALID_QUERY", message = "Search query must be at least 2 characters" });
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var result = await _countryService.SearchAsync(q, page, pageSize, cancellationToken);

        // Add headers
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        var cacheStatus = stopwatch.ElapsedMilliseconds < 10 ? "HIT" : "MISS";
        Response.Headers["X-Cache"] = cacheStatus;

        BusinessMetrics.RequestDuration.WithLabels("Search", "GET", "200").Observe(stopwatch.Elapsed.TotalSeconds);
        return Ok(result);
    }
}
