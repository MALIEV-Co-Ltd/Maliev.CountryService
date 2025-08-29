using Moq;
using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Maliev.CountryService.Tests.Countries
{
    /// <summary>
    /// Unit tests for the CreateCountryAsync method in CountriesController.
    /// </summary>
    public class CreateCountryAsync_UnitTest
    {
        /// <summary>
        /// Tests that an invalid item returns a bad request.
        /// </summary>
        [Fact]
        public async Task InvalidItem_ShouldReturnBadRequest()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);
            controller.ModelState.AddModelError("Name", "Name is required");

            // Act
            var actionResult = await controller.CreateCountryAsync(new CreateCountryRequest { Name = "", Continent = "", CountryCode = "", Iso2 = "", Iso3 = "" });

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        /// <summary>
        /// Tests that a valid item returns a created at route result.
        /// </summary>
        [Fact]
        public async Task ValidItem_ShouldReturnCreatedAtRoute()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            var request = new CreateCountryRequest
            {
                Name = "TestCountry",
                Continent = "TestContinent",
                CountryCode = "TC",
                Iso2 = "T2",
                Iso3 = "T3"
            };

            var countryDto = new CountryDto
            {
                Id = 1,
                Name = request.Name,
                Continent = request.Continent,
                CountryCode = request.CountryCode,
                Iso2 = request.Iso2,
                Iso3 = request.Iso3
            };

            mockCountryService.Setup(s => s.CreateCountryAsync(request))
                              .ReturnsAsync(countryDto);

            // Act
            var actionResult = await controller.CreateCountryAsync(request);

            // Assert
            var createdAtRouteResult = Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
            var returnedCountry = Assert.IsType<CountryDto>(createdAtRouteResult.Value);
            Assert.Equal(countryDto.Id, returnedCountry.Id);
            Assert.Equal(countryDto.Name, returnedCountry.Name);
        }
    }
}