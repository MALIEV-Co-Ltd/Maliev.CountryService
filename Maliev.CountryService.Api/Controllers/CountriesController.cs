using Asp.Versioning;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Maliev.CountryService.Api.Controllers;

[ApiController]
[Route("countries/v{version:apiVersion}")]
[ApiVersion("1.0")]
[EnableRateLimiting("CountryPolicy")]
[Authorize] // Require valid JWT token for all endpoints
public class CountriesController : ControllerBase
{
    private readonly ICountryService _countryService;
    private readonly ILogger<CountriesController> _logger;

    public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
    {
        _countryService = countryService;
        _logger = logger;
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CountryDto>> GetById(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting country with ID: {CountryId}", id);

        var country = await _countryService.GetByIdAsync(id, cancellationToken);
        
        if (country == null)
        {
            _logger.LogWarning("Country with ID {CountryId} not found", id);
            return NotFound();
        }

        return Ok(country);
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<CountryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<PagedResult<CountryDto>>> Search([FromQuery] CountrySearchRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching countries with request: {@SearchRequest}", request);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _countryService.SearchAsync(request, cancellationToken);
        
        _logger.LogInformation("Found {Count} countries out of {Total} total", result.Items.Count(), result.TotalCount);
        
        return Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CountryDto>> Create([FromBody] CreateCountryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating country: {CountryName}", request.Name);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check for duplicates
        if (await _countryService.ExistsByNameAsync(request.Name, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "A country with this name already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByIso2Async(request.ISO2, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(request.ISO2), "A country with this ISO2 code already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByIso3Async(request.ISO3, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(request.ISO3), "A country with this ISO3 code already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByCountryCodeAsync(request.CountryCode, cancellationToken: cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CountryCode), "A country with this country code already exists");
            return Conflict(ModelState);
        }

        var country = await _countryService.CreateAsync(request, cancellationToken);
        
        _logger.LogInformation("Country created successfully: {CountryName} with ID {CountryId}", country.Name, country.Id);
        
        return CreatedAtAction(nameof(GetById), new { id = country.Id }, country);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CountryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<CountryDto>> Update(int id, [FromBody] UpdateCountryRequest request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating country with ID: {CountryId}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if country exists
        if (!await _countryService.ExistsAsync(id, cancellationToken))
        {
            _logger.LogWarning("Country with ID {CountryId} not found for update", id);
            return NotFound();
        }

        // Check for duplicates (excluding current record)
        if (await _countryService.ExistsByNameAsync(request.Name, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.Name), "A country with this name already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByIso2Async(request.ISO2, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.ISO2), "A country with this ISO2 code already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByIso3Async(request.ISO3, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.ISO3), "A country with this ISO3 code already exists");
            return Conflict(ModelState);
        }

        if (await _countryService.ExistsByCountryCodeAsync(request.CountryCode, id, cancellationToken))
        {
            ModelState.AddModelError(nameof(request.CountryCode), "A country with this country code already exists");
            return Conflict(ModelState);
        }

        var country = await _countryService.UpdateAsync(id, request, cancellationToken);
        
        if (country == null)
        {
            _logger.LogWarning("Country with ID {CountryId} not found during update operation", id);
            return NotFound();
        }

        _logger.LogInformation("Country updated successfully: {CountryName} with ID {CountryId}", country.Name, country.Id);
        
        return Ok(country);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting country with ID: {CountryId}", id);

        var deleted = await _countryService.DeleteAsync(id, cancellationToken);
        
        if (!deleted)
        {
            _logger.LogWarning("Country with ID {CountryId} not found for deletion", id);
            return NotFound();
        }

        _logger.LogInformation("Country with ID {CountryId} deleted successfully", id);
        
        return NoContent();
    }

    [HttpGet("continents")]
    [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<IEnumerable<string>>> GetContinents(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting list of continents");

        var continents = await _countryService.GetContinentsAsync(cancellationToken);
        
        return Ok(continents);
    }
}