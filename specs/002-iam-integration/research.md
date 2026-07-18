# Research: IAM Integration and Permission-Based Authorization

## Decision: IAM Registration Strategy
- **Choice**: Idempotent Merge strategy on service startup.
- **Rationale**: Ensures that the service's required permissions and roles are always present in the IAM service without overwriting manual adjustments or other services' data.
- **Implementation**: A background `IHostedService` (`CountryIAMRegistrationService`) will call the IAM registration endpoints.

## Decision: Authorization Attribute
- **Choice**: Use `[RequirePermission(string permission)]` attribute.
- **Rationale**: Aligns with existing project plans and provides a declarative way to protect endpoints.
- **Detail**: The attribute will be evaluated by an authorization filter or middleware that checks the user's JWT claims for the required permission string.

## Decision: Audit Logging for Denials
- **Choice**: Extend usage of `AuditLog` entity for access denials.
- **Rationale**: centralizes all security-relevant events. Denials will have `CountryId = null` and `Action = "ACCESS_DENIED"`.
- **Detail**: The `AuditLog` entity's `CountryId` property should be made nullable in the entity class to support this.

## Decision: Feature Flag
- **Choice**: `PermissionBasedAuthEnabled` configuration flag.
- **Rationale**: Allows for safe deployment and "Legacy Mode" bypass during migration.

## Alternatives Considered
- **Strict Sync**: Rejected because it could delete roles used by other services or manual overrides.
- **Manual Registration**: Rejected as it increases operational overhead and risk of desync between code and IAM.
- **In-Memory Auth**: Rejected per Constitution (requires real infrastructure/service integration).

## Open Questions (Resolved)
- **Attribute Source**: Verified that `RequirePermissionAttribute` is expected to be part of `Maliev.Aspire.ServiceDefaults`.
- **Audit Log Nullability**: Verified `CountryId` should be `long?` to support system-wide audit events.
