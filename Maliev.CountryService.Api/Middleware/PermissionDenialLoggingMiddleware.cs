using System.Security.Claims;
using Maliev.CountryService.Data;
using Maliev.CountryService.Data.Entities;

namespace Maliev.CountryService.Api.Middleware;

/// <summary>
/// Middleware to log unauthorized access attempts to an audit log.
/// </summary>
public class PermissionDenialLoggingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    public async Task InvokeAsync(HttpContext context, CountryDbContext dbContext, ILogger<PermissionDenialLoggingMiddleware> logger)
    {
        await next(context);

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? context.User.FindFirst("sub")?.Value
                         ?? "anonymous";

            var path = context.Request.Path;
            var method = context.Request.Method;

            logger.LogWarning("Access DENIED for user {UserId} to {Method} {Path}", userId, method, path);

            // Log to database audit log
            var auditLog = new AuditLog
            {
                Action = "ACCESS_DENIED",
                UserId = userId,
                TimestampUtc = DateTime.UtcNow,
                Changes = $"Denied {method} request to {path}",
                IpAddress = context.Connection.RemoteIpAddress?.ToString()
            };

            try
            {
                dbContext.AuditLogs.Add(auditLog);
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to write access denial to audit log");
            }
        }
    }
}
