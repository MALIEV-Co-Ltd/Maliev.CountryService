using Microsoft.Extensions.Logging; // Changed from Serilog.Context

namespace Maliev.CountryService.Api.Middleware;

/// <summary>
/// Middleware that extracts or generates a correlation ID for each request to enable distributed tracing.
/// </summary>
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger; // Injected ILogger

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationIdMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger instance.</param> // Added ILogger
    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger; // Initialized ILogger
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request and add correlation ID to the response.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = GetOrCreateCorrelationId(context);
        
        // Use ILogger.BeginScope to add CorrelationId to the log scope
        using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
        {
            context.Response.Headers.Append(CorrelationIdHeader, correlationId);
            await _next(context);
        }
    }

    /// <summary>
    /// Extracts the correlation ID from the request header or generates a new one if not present.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID string.</returns>
    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var correlationId))
        {
            return correlationId.FirstOrDefault() ?? Guid.NewGuid().ToString();
        }

        return Guid.NewGuid().ToString();
    }
}

/// <summary>
/// Extension methods for registering the CorrelationIdMiddleware.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    /// <summary>
    /// Adds the CorrelationIdMiddleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CorrelationIdMiddleware>();
    }
}
