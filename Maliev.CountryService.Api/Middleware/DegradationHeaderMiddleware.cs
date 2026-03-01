using Maliev.CountryService.Application.Interfaces;

namespace Maliev.CountryService.Api.Middleware;

/// <summary>
/// T122: Middleware to set X-Degraded-Mode header when service is operating in degraded mode.
/// </summary>
public class DegradationHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DegradationHeaderMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DegradationHeaderMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="logger">The logger.</param>
    public DegradationHeaderMiddleware(RequestDelegate next, ILogger<DegradationHeaderMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to add degradation headers to the response if service is in degraded mode.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <param name="degradationContext">The degradation context containing degradation state.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, IDegradationContext degradationContext)
    {
        await _next(context);

        // T122: Add X-Degraded-Mode header if service operated in degraded mode
        if (degradationContext.IsDegraded)
        {
            context.Response.Headers["X-Degraded-Mode"] = "true";

            if (!string.IsNullOrEmpty(degradationContext.DegradationReason))
            {
                context.Response.Headers["X-Degradation-Reason"] = degradationContext.DegradationReason;
            }

            // T126: Log degradation event
            _logger.LogInformation(
                "Response served in degraded mode: {Path} {StatusCode} - {Reason}",
                context.Request.Path,
                context.Response.StatusCode,
                degradationContext.DegradationReason ?? "Unknown");
        }
    }
}
