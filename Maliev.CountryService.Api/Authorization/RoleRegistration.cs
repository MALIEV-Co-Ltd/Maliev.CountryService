namespace Maliev.CountryService.Api.Authorization;

/// <summary>
/// Represents a role registration with associated permissions.
/// </summary>
public class RoleRegistration
{
    /// <summary>Unique identifier for the role.</summary>
    public string RoleId { get; set; } = string.Empty;
    /// <summary>Display name of the role.</summary>
    public string RoleName { get; set; } = string.Empty;
    /// <summary>Description of the role's purpose.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>List of permission IDs assigned to this role.</summary>
    public string[] Permissions { get; set; } = [];
}