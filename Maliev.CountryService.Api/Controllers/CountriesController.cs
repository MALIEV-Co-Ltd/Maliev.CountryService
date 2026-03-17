using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Application.Interfaces;
using Maliev.CountryService.Application.Models.Common;
using Maliev.CountryService.Application.Models.Countries;
using Microsoft.AspNetCore.Mvc;

namespace Maliev.CountryService.Api.Controllers;

/// <summary>
/// Public endpoints for querying country information (read-only operations).
/// Provides lookup by ID/ISO codes, pagination, and search capabilities with caching.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("country/v{version:apiVersion}/countries")]
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CountriesController"/> class.
    /// </summary>
    /// <param name="countryService">The country service instance.</param>
    public CountriesController(ICountryService countryService)
    {
        _countryService = countryService;
    }

    /// <summary>
    /// Gets a country by its unique ID.
    /// </summary>
    /// <param name="id">The unique identifier of the country.</param>
    /// <param name="ifNoneMatch">Optional. An ETag from a previous request. If the ETag matches, a 304 Not Modified response is returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A single country by ID.</returns>
    [HttpGet("{id:guid}")]
    [RequirePermission(CountryPermissions.CountriesRead)]
    [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromHeader(Name = "If-None-Match")] string? ifNoneMatch, CancellationToken cancellationToken)
    {
        var country = await _countryService.GetByIdAsync(id, cancellationToken);
        if (country == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == country.ETag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = country.ETag;
        Response.Headers.LastModified = country.LastModifiedUtc.ToString("R");
        if (country.XServedFromCache)
        {
            Response.Headers["X-Served-From-Cache"] = "true";
            if (country.XCacheStale)
            {
                Response.Headers["X-Cache-Stale"] = "true";
            }
        }
        return Ok(country);
    }

    /// <summary>
    /// Gets a country by its ISO 3166-1 alpha-2 code.
    /// </summary>
    /// <param name="iso2">The two-letter ISO code of the country.</param>
    /// <param name="ifNoneMatch">Optional. An ETag from a previous request. If the ETag matches, a 304 Not Modified response is returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A single country by ISO2 code.</returns>
    [HttpGet("iso2/{iso2}")]
    [RequirePermission(CountryPermissions.CountriesRead)]
    [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIso2(string iso2, [FromHeader(Name = "If-None-Match")] string? ifNoneMatch, CancellationToken cancellationToken)
    {
        var country = await _countryService.GetByIso2Async(iso2, cancellationToken);
        if (country == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == country.ETag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = country.ETag;
        Response.Headers.LastModified = country.LastModifiedUtc.ToString("R");
        if (country.XServedFromCache)
        {
            Response.Headers["X-Served-From-Cache"] = "true";
            if (country.XCacheStale)
            {
                Response.Headers["X-Cache-Stale"] = "true";
            }
        }
        return Ok(country);
    }

    /// <summary>
    /// Gets a country by its ISO 3166-1 alpha-3 code.
    /// </summary>
    /// <param name="iso3">The three-letter ISO code of the country.</param>
    /// <param name="ifNoneMatch">Optional. An ETag from a previous request. If the ETag matches, a 304 Not Modified response is returned.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A single country by ISO3 code.</returns>
    [HttpGet("iso3/{iso3}")]
    [RequirePermission(CountryPermissions.CountriesRead)]
    [ProducesResponseType(typeof(CountryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIso3(string iso3, [FromHeader(Name = "If-None-Match")] string? ifNoneMatch, CancellationToken cancellationToken)
    {
        var country = await _countryService.GetByIso3Async(iso3, cancellationToken);
        if (country == null)
        {
            return NotFound();
        }

        if (!string.IsNullOrEmpty(ifNoneMatch) && ifNoneMatch == country.ETag)
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Response.Headers.ETag = country.ETag;
        Response.Headers.LastModified = country.LastModifiedUtc.ToString("R");
        if (country.XServedFromCache)
        {
            Response.Headers["X-Served-From-Cache"] = "true";
            if (country.XCacheStale)
            {
                Response.Headers["X-Cache-Stale"] = "true";
            }
        }
        return Ok(country);
    }

    /// <summary>
    /// Gets a list of countries. Returns all countries by default, or paginated if parameters are provided.
    /// </summary>
    /// <param name="request">Query parameters for pagination, filtering by region/subregion, and sorting.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of countries.</returns>
    [HttpGet]
    [RequirePermission(CountryPermissions.CountriesList)]
    [ResponseCache(Duration = 3600, VaryByQueryKeys = ["*"])]
    [ProducesResponseType(typeof(PaginatedResponse<CountryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List([FromQuery] CountryListRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _countryService.ListAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Searches for countries by name.
    /// </summary>
    /// <param name="query">The search query string (e.g., "United").</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A paginated list of countries matching the search query.</returns>
    [HttpGet("search")]
    [RequirePermission(CountryPermissions.CountriesSearch)]
    [ProducesResponseType(typeof(PaginatedResponse<CountryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] string query, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required and cannot be empty" });
        }

        if (page < 1)
        {
            return BadRequest(new { error = "Page must be greater than 0" });
        }

        if (pageSize < 1 || pageSize > 1000)
        {
            return BadRequest(new { error = "Page size must be between 1 and 1000" });
        }

        var result = await _countryService.SearchAsync(query, page, pageSize, cancellationToken);
        return Ok(result);
    }

}
