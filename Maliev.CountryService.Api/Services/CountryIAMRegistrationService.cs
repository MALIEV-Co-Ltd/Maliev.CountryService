using System.Net.Http.Json;
using Maliev.CountryService.Api.Authorization;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Background service to register permissions and roles with the IAM service on startup.
/// </summary>
public class CountryIAMRegistrationService(
    IHttpClientFactory httpClientFactory,
    ILogger<CountryIAMRegistrationService> logger,
    IConfiguration configuration) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var iamEnabled = configuration.GetValue<bool>("Features:PermissionBasedAuthEnabled");
        if (!iamEnabled)
        {
            logger.LogInformation("IAM registration skipped (PermissionBasedAuthEnabled=false)");
            return;
        }

        try
        {
            var client = httpClientFactory.CreateClient("IAMService");

            // Register permissions using idempotent merge strategy
            var permissionResponse = await client.PostAsJsonAsync("/api/v1/permissions/register", new
            {
                ServiceName = "CountryService",
                Permissions = CountryPermissions.All.Select(p => new
                {
                    PermissionId = p,
                    Description = $"Permission for {p}",
                    IsCritical = p == CountryPermissions.CountriesHardDelete
                })
            }, cancellationToken);

            if (!permissionResponse.IsSuccessStatusCode)
            {
                var error = await permissionResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to register permissions with IAM. Status: {Status}, Error: {Error}", 
                    permissionResponse.StatusCode, error);
                // We continue to try roles even if permissions failed, or handle as degraded mode
            }

            // Register roles using idempotent merge strategy
            var roleResponse = await client.PostAsJsonAsync("/api/v1/roles/register", new
            {
                ServiceName = "CountryService",
                Roles = CountryPredefinedRoles.All.Select(r => new
                {
                    r.RoleId,
                    r.RoleName,
                    r.Description,
                    r.Permissions
                })
            }, cancellationToken);

            if (!roleResponse.IsSuccessStatusCode)
            {
                var error = await roleResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError("Failed to register roles with IAM. Status: {Status}, Error: {Error}", 
                    roleResponse.StatusCode, error);
            }

            if (permissionResponse.IsSuccessStatusCode && roleResponse.IsSuccessStatusCode)
            {
                logger.LogInformation("Successfully registered {Count} permissions and {RoleCount} roles with IAM",
                    CountryPermissions.All.Length, CountryPredefinedRoles.All.Length);
            }
        }
        catch (Exception ex)
        {
            // Degraded mode: Log the error and continue startup
            logger.LogCritical(ex, "IAM registration failed. Service will start in DEGRADED mode.");
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
