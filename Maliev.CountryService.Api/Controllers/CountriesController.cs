using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.CountryService.Api.Controllers
{
    /// <summary>
    /// Controller.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.ControllerBase" />
    [Route("countries")]
    [ApiController]
    [ApiConventionType(typeof(DefaultApiConventions))]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CountriesController : ControllerBase
    {
        private readonly ICountryService _countryService;
        private readonly ILogger<CountriesController> _logger;

        public CountriesController(ICountryService countryService, ILogger<CountriesController> logger)
        {
            _countryService = countryService;
            _logger = logger;
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CountryDto>> CreateCountryAsync([FromBody] CreateCountryRequest request)
        {
            _logger.LogInformation("Attempting to create country with Name: {Name}", request.Name);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for CreateCountryAsync.");
                return BadRequest(ModelState);
            }

            var countryDto = await _countryService.CreateCountryAsync(request);

            _logger.LogInformation("Country created successfully with Id: {Id}", countryDto.Id);
            return CreatedAtRoute("GetCountry", new { id = countryDto.Id }, countryDto);
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteCountryAsync(int id)
        {
            _logger.LogInformation("Attempting to delete country with Id: {Id}", id);

            var deleted = await _countryService.DeleteCountryAsync(id);
            if (!deleted)
            {
                _logger.LogWarning("Country with Id: {Id} not found for deletion.", id);
                return NotFound();
            }

            _logger.LogInformation("Country with Id: {Id} deleted successfully.", id);
            return NoContent();
        }

        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<List<CountryDto>>> GetAllCountriesAsync()
        {
            _logger.LogInformation("Attempting to retrieve all countries.");

            var countries = await _countryService.GetAllCountriesAsync();
            if (countries == null || countries.Count == 0)
            {
                _logger.LogWarning("No countries found.");
                return NotFound();
            }

            _logger.LogInformation("Retrieved {Count} countries.", countries.Count);
            return Ok(countries);
        }

        [HttpGet("{id}", Name = "GetCountry")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CountryDto>> GetCountryAsync(int id)
        {
            _logger.LogInformation("Attempting to retrieve country with Id: {Id}", id);

            var country = await _countryService.GetCountryAsync(id);
            if (country == null)
            {
                _logger.LogWarning("Country with Id: {Id} not found.", id);
                return NotFound();
            }

            _logger.LogInformation("Retrieved country with Id: {Id}", id);
            return Ok(country);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateCountryAsync(int id, [FromBody] UpdateCountryRequest request)
        {
            _logger.LogInformation("Attempting to update country with Id: {Id}", id);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateCountryAsync for Id: {Id}", id);
                return BadRequest(ModelState);
            }

            var updated = await _countryService.UpdateCountryAsync(id, request);
            if (!updated)
            {
                _logger.LogWarning("Country with Id: {Id} not found for update.", id);
                return NotFound();
            }

            _logger.LogInformation("Country with Id: {Id} updated successfully.", id);
            return NoContent();
        }
    }
}