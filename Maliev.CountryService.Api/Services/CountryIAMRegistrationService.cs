using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.CountryService.Api.Authorization;

namespace Maliev.CountryService.Api.Services;

/// <summary>
/// Background service to register permissions and roles with the IAM service on startup.
/// Uses the standard IAMRegistrationService base class.
/// </summary>
public class CountryIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CountryIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public CountryIAMRegistrationService(
        IHttpClientFactory httpClientFactory,
        ILogger<CountryIAMRegistrationService> logger)
        : base(httpClientFactory, logger, "country")
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return CountryPermissions.All.Select(p => new PermissionRegistration
        {
            PermissionId = p,
            Description = $"Permission for {p}"
        });
    }

    /// <inheritdoc/>
    protected override IEnumerable<Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration> GetPredefinedRoles()
    {
        return CountryPredefinedRoles.All.Select(r => new Maliev.Aspire.ServiceDefaults.IAM.RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}