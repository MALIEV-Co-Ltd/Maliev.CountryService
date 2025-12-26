# Data Model: Authorization and Auditing

## Entities

### AuditLog (Updated)
Tracks all modifications and access denials.

| Field | Type | Description |
|-------|------|-------------|
| Id | long | Primary Key |
| CountryId | long? | Reference to Country (null for non-entity actions) |
| Action | string | Action performed (CREATE, UPDATE, DELETE, ACCESS_DENIED, etc.) |
| UserId | string | Identity of the user performing the action |
| TimestampUtc | DateTime | When the action occurred |
| Changes | string (JSON) | Details of changes or denied request context |
| IpAddress | string? | Source IP of the requester |

### Permission (Domain Object)
Not persisted in CountryService database, but used for IAM registration.

| Field | Type | Description |
|-------|------|-------------|
| PermissionId | string | Unique identifier (e.g., `country.countries.read`) |
| Description | string | Human-readable description |
| IsCritical | bool | True if the permission allows permanent data loss |

### Role (Domain Object)
Not persisted in CountryService database, but used for IAM registration.

| Field | Type | Description |
|-------|------|-------------|
| RoleId | string | Unique identifier (e.g., `country-admin`) |
| RoleName | string | Display name |
| Description | string | Role purpose |
| Permissions | string[] | List of permission IDs assigned to this role |
