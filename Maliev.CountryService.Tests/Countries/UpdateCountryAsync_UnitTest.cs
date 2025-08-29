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
    /// Unit tests for the UpdateCountryAsync method in CountriesController.
    /// </summary>
    public class UpdateCountryAsync_UnitTest
    {
        /// <summary>
        /// Tests that an existing country returns no content on successful update.
        /// </summary>
        [Fact]
        public async Task CountryExist_ShouldReturnNoContent()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            mockCountryService.Setup(s => s.UpdateCountryAsync(It.IsAny<int>(), It.IsAny<UpdateCountryRequest>())).ReturnsAsync(true);
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            var request = new UpdateCountryRequest
            {
                Id = 1,
                Name = "UpdatedCountry",
                Continent = "UpdatedContinent",
                CountryCode = "UC",
                Iso2 = "U2",
                Iso3 = "U3"
            };

            // Act
            var actionResult = await controller.UpdateCountryAsync(request.Id, request);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }

        /// <summary>
        /// Tests that a non-existing country returns not found on update attempt.
        /// </summary>
        [Fact]
        public async Task CountryNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            mockCountryService.Setup(s => s.UpdateCountryAsync(It.IsAny<int>(), It.IsAny<UpdateCountryRequest>())).ReturnsAsync(false);
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            var request = new UpdateCountryRequest
            {
                Id = int.MaxValue,
                Name = "NonExistentCountry",
                Continent = "NonExistentContinent",
                CountryCode = "NE",
                Iso2 = "N2",
                Iso3 = "N3"
            };

            // Act
            var actionResult = await controller.UpdateCountryAsync(request.Id, request);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }

        /// <summary>
        /// Tests that invalid country data returns a bad request.
        /// </summary>
        [Fact]
        public async Task InvalidCountryData_ShouldReturnBadRequest()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);
            controller.ModelState.AddModelError("Name", "Name is required");

            // Act
                        // Act
            var actionResult = await controller.UpdateCountryAsync(1, new UpdateCountryRequest { Id = 1, Name = "", Continent = "", CountryCode = "", Iso2 = "", Iso3 = "" });

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        /// <summary>
        /// Tests that an invalid country identifier returns a bad request.
        /// </summary>
        [Fact]
        public async Task InvalidCountryId_ShouldReturnBadRequest()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);
            controller.ModelState.AddModelError("Id", "Id is invalid");

            // Act
            // Act
            var actionResult = await controller.UpdateCountryAsync(0, new UpdateCountryRequest { Id = 0, Name = "", Continent = "", CountryCode = "", Iso2 = "", Iso3 = "" });

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }
    }
}