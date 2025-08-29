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
    /// Unit tests for the GetCountryAsync method in CountriesController.
    /// </summary>
    public class GetCountryAsync_UnitTest
    {
        /// <summary>
        /// Tests that an existing country is returned successfully.
        /// </summary>
        [Fact]
        public async Task CountryExist_ShouldReturnCountry()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            var countryDto = new CountryDto { Id = 1, Name = "Maliev", Continent = "Asia", CountryCode = "M1", Iso2 = "M1", Iso3 = "M1" };

            mockCountryService.Setup(s => s.GetCountryAsync(It.IsAny<int>()))
                              .ReturnsAsync(countryDto);

            // Act
            var actionResult = await controller.GetCountryAsync(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedCountry = Assert.IsType<CountryDto>(okResult.Value);
            Assert.Equal(countryDto.Id, returnedCountry.Id);
        }

        /// <summary>
        /// Tests that a not found result is returned when the country does not exist.
        /// </summary>
        [Fact]
        public async Task CountryNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            mockCountryService.Setup(s => s.GetCountryAsync(It.IsAny<int>()))
                              .ReturnsAsync((CountryDto)null);

            // Act
            var actionResult = await controller.GetCountryAsync(int.MaxValue);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }
    }
}