using Maliev.CountryService.Api.Exceptions;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Maliev.CountryService.Api.Middleware;

/// <summary>
/// Middleware that catches and handles unhandled exceptions, converting them to appropriate HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to catch and handle exceptions during request processing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    /// <summary>
    /// Handles an exception by converting it to an appropriate HTTP error response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="exception">The exception to handle.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message) = exception switch
        {
            ArgumentNullException => (HttpStatusCode.BadRequest, "Invalid request data"),
            ArgumentException => (HttpStatusCode.BadRequest, "Invalid argument provided"),
            InvalidOperationException => (HttpStatusCode.BadRequest, "Invalid operation"),
            DbUpdateException => (HttpStatusCode.Conflict, "Database constraint violation"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized access"),
            NotImplementedException => (HttpStatusCode.NotImplemented, "Feature not implemented"),
            TimeoutException => (HttpStatusCode.RequestTimeout, "Request timeout"),
            DuplicateCountryException => (HttpStatusCode.Conflict, "A country with the same name, ISO code, or country code already exists."),
            CountryServiceException => (HttpStatusCode.BadRequest, exception.Message),
            Npgsql.NpgsqlException => (HttpStatusCode.ServiceUnavailable, "Database temporarily unavailable - please retry later"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred while processing your request")
        };

        context.Response.StatusCode = (int)statusCode;

        // T123: Add Retry-After header for 503 responses (based on circuit breaker duration)
        if (statusCode == HttpStatusCode.ServiceUnavailable)
        {
            context.Response.Headers["Retry-After"] = "60"; // Circuit breaker duration in seconds
        }

        var response = new
        {
            error = new
            {
                message,
                statusCode = (int)statusCode,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            }
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}
