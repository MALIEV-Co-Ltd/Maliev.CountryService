using Moq;
using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Maliev.CountryService.Tests.Countries
{
    /// <summary>
    /// Unit tests for the GetAllCountriesAsync method in CountriesController.
    /// </summary>
    public class GetAllCountriesAsync_UnitTest
    {
        /// <summary>
        /// Tests that existing countries are returned successfully.
        /// </summary>
        [Fact]
        public async Task CountryExist_ShouldReturnCountries()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            var countryDtoList = new List<CountryDto>
            {
                new CountryDto { Id = 1, Name = "Maliev1", Continent = "Asia", CountryCode = "M1", Iso2 = "M1", Iso3 = "M1" },
                new CountryDto { Id = 2, Name = "Maliev2", Continent = "Europe", CountryCode = "M2", Iso2 = "M2", Iso3 = "M2" },
                new CountryDto { Id = 3, Name = "Maliev3", Continent = "Africa", CountryCode = "M3", Iso2 = "M3", Iso3 = "M3" }
            };

            mockCountryService.Setup(s => s.GetAllCountriesAsync())
                              .ReturnsAsync(countryDtoList);

            // Act
            var actionResult = await controller.GetAllCountriesAsync();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedCountries = Assert.IsType<List<CountryDto>>(okResult.Value);
            Assert.Equal(3, returnedCountries.Count);
        }

        /// <summary>
        /// Tests that a not found result is returned when no countries exist.
        /// </summary>
        [Fact]
        public async Task CountryNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            mockCountryService.Setup(s => s.GetAllCountriesAsync())
                              .ReturnsAsync(new List<CountryDto>());

            // Act
            var actionResult = await controller.GetAllCountriesAsync();

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }
    }
}