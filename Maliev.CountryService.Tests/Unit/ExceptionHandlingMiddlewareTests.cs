using FluentAssertions;
using Maliev.CountryService.Api.Exceptions;
using Maliev.CountryService.Api.Middleware;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json;

namespace Maliev.CountryService.Tests.Unit;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task ExceptionHandlingMiddleware_WithDuplicateCountryException_Returns409()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new DuplicateCountryException("A country with the same name, ISO code, or country code already exists."),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseText);

        response.GetProperty("error").GetProperty("message").GetString().Should().Be("A country with the same name, ISO code, or country code already exists.");
        response.GetProperty("error").GetProperty("statusCode").GetInt32().Should().Be((int)HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_WithCountryServiceException_Returns400()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionHandlingMiddleware(
            next: (innerHttpContext) => throw new CountryServiceException("An error occurred in the country service."),
            logger: Microsoft.Extensions.Logging.Abstractions.NullLogger<ExceptionHandlingMiddleware>.Instance
        );

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        var response = JsonSerializer.Deserialize<JsonElement>(responseText);

        response.GetProperty("error").GetProperty("message").GetString().Should().Be("An error occurred in the country service.");
        response.GetProperty("error").GetProperty("statusCode").GetInt32().Should().Be((int)HttpStatusCode.BadRequest);
    }
}