# Feature Specification: Permission-Based Authorization Migration

**Feature Branch**: `002-iam-integration`  
**Created**: 2025-12-21  
**Status**: Draft  
**Input**: User description: "Permission-Based Authorization Migration" (based on country-specify.md)

## Clarifications

### Session 2025-12-21
- Q: How should the system handle synchronization if permissions or roles already exist in IAM with different definitions? → A: Ensure existence (Merge): Add missing permissions/roles; do not delete or modify existing ones.
- Q: Should "public access" be truly anonymous or require a valid JWT? → A: True Anonymous: No authentication (JWT) required for public read endpoints.
- Q: If the initial permission/role registration fails during service startup, how should the application behave? → A: Degraded Mode: Log the error and continue startup; public read access remains available while protected endpoints will naturally fail permission checks.
- Q: Should the system explicitly log an audit entry for every denied administrative attempt? → A: Log All (Audit): Create a persistent audit log entry for every denied attempt on a protected endpoint.
- Q: When the feature flag PermissionBasedAuthEnabled is set to false, how should protected administrative endpoints behave? → A: Legacy Mode (Bypass): Permission checks are skipped; administrative endpoints behave as they do today (unprotected).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Secure Administrative Data Management (Priority: P1)

As a Country Administrator, I want to manage country data (create, update, delete) securely so that only authorized personnel can modify critical reference data.

**Why this priority**: Protecting the integrity of the country data is the primary goal of this service. Unauthorized modifications could lead to system-wide failures in dependent services.

**Independent Test**: Can be tested by attempting to perform administrative actions (creating, updating, or deleting countries) with and without the required permissions.

**Acceptance Scenarios**:

1. **Given** a user with the permission to create countries, **When** they submit a request to create a country, **Then** the request is successful.
2. **Given** a user without administrative permissions, **When** they attempt to update or delete a country, **Then** the system denies the request.
3. **Given** a user with the permission to delete countries but NOT the permission to permanently (hard) delete them, **When** they attempt a permanent deletion, **Then** the operation is rejected.

---

### User Story 2 - Controlled Bulk Import Operations (Priority: P2)

As a Data Importer, I want to upload and execute bulk country data imports so that large datasets can be managed efficiently without exposing the import tools to unauthorized users.

**Why this priority**: Bulk imports are resource-intensive and can overwrite large amounts of data; they must be restricted to prevent accidental or malicious system load.

**Independent Test**: Verify that bulk import operations (uploading, triggering execution, viewing history) are restricted to users with the appropriate import permissions.

**Acceptance Scenarios**:

1. **Given** a user with permissions to upload and execute imports, **When** they trigger a bulk import, **Then** the import job starts successfully.
2. **Given** a user with only permission to view import status, **When** they try to cancel a running import, **Then** the cancellation is denied.

---

### User Story 3 - Unhindered Public Access (Priority: P3)

As a Public API User, I want to list and search for country data without needing special permissions so that the reference data remains easily accessible.

**Why this priority**: The service's primary consumer-facing value is providing reference data; requiring authentication for these basic operations would add unnecessary friction for many users.

**Independent Test**: Verify that all public data retrieval operations (listing, searching, lookup by code) remain accessible to anonymous or unauthenticated users.

**Acceptance Scenarios**:

1. **Given** an unauthenticated user, **When** they request the list of all countries, **Then** the system returns the data successfully.
2. **Given** an unauthenticated user, **When** they search for a country by name, **Then** the system returns matching results.

---

### User Story 4 - Restricted System Maintenance (Priority: P4)

As a System Operator, I want to refresh the system cache and view service statistics so that I can maintain service performance and monitor health securely.

**Why this priority**: Cache maintenance and detailed stats can reveal internal system details or cause temporary performance fluctuations, so they should be restricted to operators.

**Independent Test**: Verify that system maintenance and statistics operations are only accessible to users with specific system permissions.

**Acceptance Scenarios**:

1. **Given** a user with permission to rebuild the cache, **When** they request a cache refresh, **Then** the cache is refreshed successfully.
2. **Given** a user without system permissions, **When** they attempt to view detailed service statistics, **Then** access is denied.

---

### Edge Cases

- **Identity Service Downtime**: If the external identity management service is unreachable during startup, the system MUST log the error and continue starting up in a degraded mode; public read access remains available, but protected administrative and import endpoints will return errors for all users since permissions cannot be validated.
- **Stale Permissions**: Permissions MUST be refreshed from the identity token on every request. The IAM permission cache TTL MUST NOT exceed 5 minutes to ensure timely revocation.
- **Migration Transition**: During the transition period, the system should allow for a controlled "dry run" or toggle to enable enforcement without breaking existing workflows unexpectedly.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST register granular permissions (e.g., read, create, update, delete, import, system maintenance) with the IAM Service upon startup using an idempotent merge strategy (add missing, do not delete existing).
- **FR-002**: System MUST define predefined roles (e.g., Admin, Manager, Importer, Viewer) that map to specific sets of these permissions.
- **FR-003**: System MUST enforce permission checks on all data modification operations (create, update, delete, restore).
- **FR-004**: System MUST distinguish between standard (soft) deletion and permanent (hard) deletion with separate permission requirements.
- **FR-005**: System MUST enforce permission checks on all bulk import lifecycle operations.
- **FR-006**: System MUST enforce permission checks on system maintenance and monitoring operations.
- **FR-009**: System MUST create a persistent audit log record for every denied administrative or import operation attempt, capturing the user identity and the requested operation.
- **FR-007**: System MUST allow public, unauthenticated (anonymous) access to basic data retrieval operations (listing, searching, and looking up countries).
- **FR-008**: System MUST provide a mechanism to enable or disable permission enforcement via configuration (e.g., `PermissionBasedAuthEnabled`) to support a safe migration; when disabled, permission checks are bypassed (Legacy Mode).

### Key Entities *(include if feature involves data)*

- **Permission**: A granular identifier representing the right to perform a specific action within the service.
- **Role**: A collection of permissions that can be assigned to users to define their access level.
- **Authorization Context**: The set of permissions identified for the current user at the time of a request.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of administrative and bulk import operations are protected by permission checks when enforcement is enabled.
- **SC-002**: Public read operations (List, Search, Lookup) remain accessible without any required permissions.
- **SC-003**: Requests from users without sufficient permissions are blocked and receive a clear "unauthorized" or "forbidden" response.
- **SC-004**: All required permissions and roles are correctly synchronized with the identity provider on system startup.
- **SC-005**: Authorization checks introduce negligible latency (target < 5ms per request).
