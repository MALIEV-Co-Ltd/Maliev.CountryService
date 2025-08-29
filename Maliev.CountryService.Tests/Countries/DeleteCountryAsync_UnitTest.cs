using Moq;
using Maliev.CountryService.Api.Controllers;
using Maliev.CountryService.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Xunit;

namespace Maliev.CountryService.Tests.Countries
{
    /// <summary>
    /// Unit tests for the DeleteCountryAsync method in CountriesController.
    /// </summary>
    public class DeleteCountryAsync_UnitTest
    {
        /// <summary>
        /// Tests that an existing country returns no content on successful deletion.
        /// </summary>
        [Fact]
        public async Task CountryExist_ShouldReturnNoContent()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            mockCountryService.Setup(s => s.DeleteCountryAsync(It.IsAny<int>())).ReturnsAsync(true);
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            // Act
            var actionResult = await controller.DeleteCountryAsync(1);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }

        /// <summary>
        /// Tests that a non-existing country returns not found on deletion attempt.
        /// </summary>
        [Fact]
        public async Task CountryNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var mockCountryService = new Mock<ICountryService>();
            var mockLogger = new Mock<ILogger<CountriesController>>();
            mockCountryService.Setup(s => s.DeleteCountryAsync(It.IsAny<int>())).ReturnsAsync(false);
            var controller = new CountriesController(mockCountryService.Object, mockLogger.Object);

            // Act
            var actionResult = await controller.DeleteCountryAsync(int.MaxValue);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}