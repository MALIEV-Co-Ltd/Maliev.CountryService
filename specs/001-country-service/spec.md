# Feature Specification: Country WebAPI Service

**Feature Branch**: `001-country-service`
**Created**: 2025-10-31
**Status**: Draft
**Input**: User description: "Country WebAPI service optimized for minimal resource use, very fast read responses, and strong caching. The service exposes a stable base path /countries/v1 and serves as the canonical read/write store for country canonical data used by other services. Public, read-optimized endpoints must allow fast retrieval of the country list and single-country lookups by internal ID or ISO code. Administrative endpoints must permit create, update, patch, and soft-delete operations and be protected by role-based authentication. The prompt should explicitly state that the service will not publish create/update/delete events to a message bus because country data changes are infrequent and downstream systems can poll or request snapshots on demand."

## Clarifications

### Session 2025-10-31

- Q: When downstream services need to obtain a full snapshot of country data (rather than polling individual records), how should the Country service expose this capability? → A: Expose the existing GET /countries/v1/countries?includeInactive=all endpoint with pagination as the snapshot mechanism - no separate endpoint needed
- Q: FR-034 specifies cache-warming should pre-load "top 50 most accessed countries" on startup. How should the system determine which 50 countries to prioritize when the cache is cold (e.g., after initial deployment or complete restart)? → A: Pre-configure a static list of the 50 most populous countries as a sensible default that will cover the majority of real-world use cases
- Q: When a bulk import contains records with duplicate ISO codes (either within the import batch itself, or conflicting with existing active records in the database), how should the system handle this conflict? → A: Reject the entire bulk import with detailed validation errors listing all duplicate ISO codes - require administrator to resolve conflicts before resubmission

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Fast Country Lookup by ISO Code (Priority: P1)

Client applications (web apps, mobile apps, other microservices) need to quickly retrieve country information by ISO 2-letter or ISO 3-letter code to populate dropdowns, validate addresses, display localized content, or enforce geo-restrictions. This is the most common operation representing 90%+ of traffic.

**Why this priority**: This is the core value proposition of the service. Without fast, reliable read access, the service cannot serve its primary purpose as a canonical country data source.

**Independent Test**: Can be fully tested by making HTTP GET requests to the country lookup endpoints with various ISO codes and verifying sub-50ms p95 response times with correct data returned.

**Acceptance Scenarios**:

1. **Given** the service is running with cached country data, **When** a client requests country by ISO2 code (e.g., "TH"), **Then** the service returns complete country information in under 50ms with HTTP 200 and ETag header
2. **Given** the service has country data cached, **When** a client requests country by ISO3 code (e.g., "THA"), **Then** the service returns the same country information with consistent ETag
3. **Given** a client has cached data with ETag, **When** the client sends conditional GET with If-None-Match header, **Then** the service returns HTTP 304 Not Modified without response body
4. **Given** an invalid ISO code is requested, **When** the client queries for a non-existent code, **Then** the service returns HTTP 404 with clear error message in under 50ms

---

### User Story 2 - Retrieve Complete Country List (Priority: P1)

Client applications need to retrieve the complete list of active countries to populate selection interfaces (dropdowns, autocomplete fields, country pickers) or to synchronize local caches. Downstream services also use this endpoint with includeInactive=all parameter to obtain full snapshots of country data for periodic synchronization.

**Why this priority**: Essential for any application that needs country selection functionality. This is a critical read operation alongside single-country lookup. Also serves as the snapshot mechanism for downstream services.

**Independent Test**: Can be fully tested by requesting the country list endpoint and verifying all active countries are returned with proper pagination support and cache headers.

**Acceptance Scenarios**:

1. **Given** the service contains 250 active countries, **When** a client requests the full country list, **Then** the service returns all active countries with HTTP 200, ETag, and Last-Modified headers in under 100ms
2. **Given** the country list is cached, **When** a client sends conditional GET with If-Modified-Since header, **Then** the service returns HTTP 304 if data hasn't changed
3. **Given** some countries are soft-deleted (active=false), **When** a client requests the country list, **Then** only active countries are returned by default
4. **Given** a downstream service needs a complete snapshot, **When** the service requests with includeInactive=all query parameter, **Then** all countries (both active and inactive) are returned with pagination support and appropriate cache headers

---

### User Story 3 - Administrative Country Management (Priority: P2)

System administrators need to create, update, and soft-delete country records to maintain data accuracy as geopolitical changes occur (new countries, name changes, code reassignments).

**Why this priority**: While less frequent than reads, administrative operations are essential for data maintenance. Protected by authentication ensures only authorized personnel can modify canonical data.

**Independent Test**: Can be fully tested by authenticating as admin user, performing CRUD operations, and verifying data persistence, cache invalidation, and audit trails.

**Acceptance Scenarios**:

1. **Given** an authenticated admin user, **When** the admin creates a new country with valid ISO codes and data, **Then** the service persists the country, returns HTTP 201 with Location header, and invalidates cached country lists
2. **Given** an existing country record, **When** an admin updates the country with If-Match header matching current version, **Then** the service updates the record, increments version, returns HTTP 200, and invalidates relevant caches
3. **Given** an admin attempts to update with stale version, **When** the If-Match header doesn't match current version, **Then** the service returns HTTP 412 Precondition Failed with current version information
4. **Given** an existing country, **When** an admin soft-deletes the country, **Then** the service sets active=false and deletedAt timestamp, preserves data, invalidates caches, and returns HTTP 204
5. **Given** unauthenticated or unauthorized user, **When** attempting any admin operation, **Then** the service returns HTTP 401 or HTTP 403 respectively

---

### User Story 4 - Optimistic Concurrency Control (Priority: P2)

When multiple administrators are working simultaneously, the system must prevent data loss from concurrent modifications using version-based optimistic locking.

**Why this priority**: Critical for data integrity in multi-user administrative scenarios. Prevents silent data overwrites that could corrupt canonical country information.

**Independent Test**: Can be tested by simulating concurrent update attempts from two admin clients and verifying the second update fails with appropriate conflict response.

**Acceptance Scenarios**:

1. **Given** two admins retrieve the same country record (version 5), **When** admin A updates and commits successfully (version becomes 6), **Then** admin B's update attempt with version 5 fails with HTTP 412 Precondition Failed
2. **Given** a client reads a country record, **When** the client later attempts update, **Then** the service requires If-Match header with ETag or version number
3. **Given** an update attempt without If-Match header, **When** processing the request, **Then** the service returns HTTP 428 Precondition Required

---

### User Story 5 - Bulk Country Data Import (Priority: P3)

Data administrators need to perform bulk imports or updates from authoritative sources (ISO standards updates, government databases) without disrupting read traffic or saturating the cache.

**Why this priority**: Infrequent operation (quarterly or annually) but necessary for comprehensive data updates from authoritative sources.

**Independent Test**: Can be tested by uploading a CSV/JSON file with country updates, verifying background job processing, staged updates, and atomic cache refresh.

**Acceptance Scenarios**:

1. **Given** an authenticated admin with bulk import permissions, **When** submitting a bulk import file, **Then** the service accepts the file, returns HTTP 202 Accepted with job ID, and processes asynchronously
2. **Given** a bulk import is processing, **When** querying the job status endpoint, **Then** the service returns progress information (processed/total, errors, warnings)
3. **Given** a bulk import completes successfully, **When** the staging data is validated, **Then** the service atomically applies changes to production tables and performs coordinated cache invalidation
4. **Given** a bulk import contains validation errors (including duplicate ISO codes), **When** validation runs, **Then** the entire import is rejected with detailed error listing all validation failures, and no records are persisted
5. **Given** a bulk import contains duplicate ISO codes (within the batch or conflicting with existing active records), **When** validation detects duplicates, **Then** the entire batch is rejected with errors listing each duplicate ISO code and conflicting record details
6. **Given** a bulk import contains validation errors, **When** dry-run mode is enabled, **Then** the service returns all validation errors without persisting data
7. **Given** a large bulk import (10k+ records), **When** processing, **Then** the service uses pagination and rate limiting to avoid saturating the main read cache

---

### User Story 6 - Service Degradation and Resilience (Priority: P2)

When infrastructure failures occur (database unavailable, cache failures), the service must continue serving read traffic from cache and gracefully degrade while clearly communicating system status.

**Why this priority**: Ensures high availability for read operations which represent 99%+ of traffic. Critical for downstream service reliability.

**Independent Test**: Can be tested by simulating database or cache failures and verifying the service continues serving stale data with appropriate staleness indicators.

**Acceptance Scenarios**:

1. **Given** the primary database becomes unavailable, **When** a client requests cached country data, **Then** the service returns data from cache with HTTP 200 and X-Served-From-Cache: stale header
2. **Given** the database is unavailable, **When** a client attempts write operation, **Then** the service returns HTTP 503 Service Unavailable with Retry-After header
3. **Given** the Redis cache becomes unavailable, **When** requests arrive, **Then** the service falls back to in-memory cache and continues serving with degraded performance
4. **Given** both database and cache are unavailable, **When** the service has in-memory cache, **Then** the service serves from memory with X-Degraded-Mode: true header
5. **Given** all data sources are unavailable, **When** health check is queried, **Then** readiness endpoint returns unhealthy while liveness remains healthy

---

### Edge Cases

- What happens when an ISO code is reassigned to a different country? (System maintains historical records through versioning, soft-delete old assignment, create new record with effective date metadata)
- How does the system handle concurrent soft-delete and update operations? (Soft-delete takes precedence if it wins the version race; update attempt on deleted record returns 404)
- What happens when cache is completely cold after restart? (Cache-warming job pre-loads the 50 most populous countries on startup using a pre-configured static list; first requests may take 100-200ms until cache fully warms)
- How does the system handle malformed UTF-8 in country names? (Strict input validation rejects invalid UTF-8 with HTTP 400; storage uses UTF-8 encoding with validation)
- What happens when Redis is slow but not down? (Circuit breaker pattern degrades to in-memory cache if Redis latency exceeds threshold for 3 consecutive requests)
- How does system handle very large bulk imports (100k+ records)? (Reject with HTTP 413 Payload Too Large; recommend splitting into multiple batches with max 10k records per batch)
- What happens when a bulk import contains duplicate ISO codes? (Entire batch is rejected with detailed validation errors listing all duplicate ISO codes, row numbers, and conflicting records; administrator must resolve conflicts and resubmit; no partial imports allowed to maintain data integrity)

## Requirements *(mandatory)*

### Functional Requirements

#### Core Read Operations
- **FR-001**: System MUST provide endpoint to retrieve single country by internal UUID identifier
- **FR-002**: System MUST provide endpoint to retrieve single country by ISO 3166-1 alpha-2 code (2-letter code)
- **FR-003**: System MUST provide endpoint to retrieve single country by ISO 3166-1 alpha-3 code (3-letter code)
- **FR-004**: System MUST provide endpoint to retrieve list of all active countries
- **FR-005**: System MUST support filtering country list by active status (active, inactive, or all)
- **FR-006**: System MUST support pagination on country list endpoint with configurable page size (default 50, max 500)
- **FR-007**: System MUST return HTTP 404 for non-existent country lookups with clear error message

#### Administrative Write Operations
- **FR-008**: System MUST provide endpoint to create new country record with complete validation
- **FR-009**: System MUST provide endpoint to update existing country record (full replacement)
- **FR-010**: System MUST provide endpoint to partially update country record (PATCH semantics for specific fields)
- **FR-011**: System MUST provide endpoint to soft-delete country record (set active=false, populate deletedAt)
- **FR-012**: System MUST restrict hard delete to super-admin role only with explicit audit logging
- **FR-013**: System MUST return HTTP 201 Created with Location header on successful country creation

#### Data Validation and Integrity
- **FR-014**: System MUST validate ISO 3166-1 alpha-2 code format (exactly 2 uppercase letters)
- **FR-015**: System MUST validate ISO 3166-1 alpha-3 code format (exactly 3 uppercase letters) when provided
- **FR-016**: System MUST validate ISO 3166-1 numeric code format (3-digit string) when provided
- **FR-017**: System MUST enforce uniqueness constraint on ISO 3166-1 alpha-2 code across active records
- **FR-018**: System MUST enforce uniqueness constraint on ISO 3166-1 alpha-3 code across active records when provided
- **FR-019**: System MUST validate phone calling code format (1-4 digit string with optional + prefix)
- **FR-020**: System MUST validate currency code format (ISO 4217 3-letter uppercase code)
- **FR-021**: System MUST validate timezones array contains valid IANA timezone identifiers
- **FR-022**: System MUST reject requests with invalid UTF-8 encoding with HTTP 400

#### Optimistic Concurrency Control
- **FR-023**: System MUST include version field (integer) for optimistic concurrency control
- **FR-024**: System MUST include ETag header in all GET responses based on version and data hash
- **FR-025**: System MUST require If-Match header with current ETag or version for all update operations
- **FR-026**: System MUST return HTTP 412 Precondition Failed when If-Match version doesn't match current version
- **FR-027**: System MUST return HTTP 428 Precondition Required when If-Match header is missing on update/delete operations
- **FR-028**: System MUST increment version field on every successful update

#### Caching and Performance
- **FR-029**: System MUST implement in-process LRU cache per instance for most frequently accessed countries
- **FR-030**: System MUST implement distributed cache layer (Redis) for shared caching across instances
- **FR-031**: System MUST configure default cache TTL of 24 hours for country data
- **FR-032**: System MUST support stale-while-revalidate pattern with 1-hour grace period
- **FR-033**: System MUST invalidate affected cache keys immediately upon successful write operations
- **FR-034**: System MUST implement cache-warming on service startup to pre-load the 50 most populous countries (using a pre-configured static list as a sensible proxy for real-world access patterns)
- **FR-035**: System MUST include ETag header in all cacheable responses
- **FR-036**: System MUST include Last-Modified header in all country responses
- **FR-037**: System MUST support conditional GET requests with If-None-Match header (return 304 if ETag matches)
- **FR-038**: System MUST support conditional GET requests with If-Modified-Since header (return 304 if not modified)
- **FR-039**: System MUST compress responses with gzip or deflate encoding when client accepts compression
- **FR-040**: System MUST achieve p95 read latency under 50ms for cached country lookups on small instances
- **FR-041**: System MUST achieve p95 read latency under 100ms for country list retrieval with default pagination

#### Bulk Operations
- **FR-042**: System MUST provide endpoint for bulk country import accepting JSON or CSV format
- **FR-043**: System MUST process bulk imports asynchronously as background jobs
- **FR-044**: System MUST support dry-run mode for bulk imports returning validation results without persisting
- **FR-045**: System MUST limit bulk import batch size to maximum 10,000 records per request
- **FR-046**: System MUST return HTTP 202 Accepted with job identifier for accepted bulk import requests
- **FR-047**: System MUST provide endpoint to query bulk import job status with progress information
- **FR-048**: System MUST use staging area for bulk imports with atomic swap or incremental application
- **FR-049**: System MUST perform coordinated cache invalidation after successful bulk import completion
- **FR-050**: System MUST implement rate limiting on bulk import processing to avoid saturating read cache
- **FR-051**: System MUST validate all records in bulk import batch before persisting any records (all-or-nothing validation)
- **FR-052**: System MUST reject entire bulk import if any record contains duplicate ISO codes (within batch or conflicting with existing active records)
- **FR-053**: System MUST return detailed validation error report listing all duplicate ISO codes with row numbers and conflicting record identifiers when bulk import is rejected

#### Security and Authorization
- **FR-054**: System MUST enforce HTTPS-only for all endpoints (reject HTTP with 301 redirect or block)
- **FR-055**: System MUST allow anonymous access to read endpoints (GET /countries, GET /countries/:id, GET /countries/iso2/:code, GET /countries/iso3/:code)
- **FR-056**: System MUST require JWT authentication for all administrative write endpoints
- **FR-057**: System MUST enforce role-based access control with minimum "CountryAdmin" role for write operations
- **FR-058**: System MUST require "SuperAdmin" role for hard delete operations
- **FR-059**: System MUST sanitize all input data to prevent injection attacks
- **FR-060**: System MUST mask sensitive fields in application logs (exclude createdBy/updatedBy emails, tokens)
- **FR-061**: System MUST implement rate limiting on public read endpoints (default 100 requests/minute per IP)
- **FR-062**: System MUST implement stricter rate limiting on admin endpoints (default 20 requests/minute per authenticated user)

#### Observability and Health
- **FR-063**: System MUST provide liveness endpoint at /countries/v1/liveness returning 200 when process is running
- **FR-064**: System MUST provide readiness endpoint at /countries/v1/readiness returning 200 only when database and cache are accessible
- **FR-065**: System MUST expose Prometheus metrics endpoint at /countries/v1/metrics
- **FR-066**: System MUST emit metrics for cache hit/miss rates by cache layer (in-memory, Redis)
- **FR-067**: System MUST emit metrics for request latency percentiles (p50, p95, p99) by endpoint
- **FR-068**: System MUST emit metrics for request volume by endpoint and HTTP status code
- **FR-069**: System MUST emit metrics for database connection pool usage
- **FR-070**: System MUST log all administrative write operations with user identity, timestamp, and affected resource

#### Failure Handling and Resilience
- **FR-071**: System MUST continue serving read requests from cache when primary database is unavailable
- **FR-072**: System MUST return HTTP 503 Service Unavailable for write operations when database is unavailable
- **FR-073**: System MUST include Retry-After header in 503 responses with estimated recovery time
- **FR-074**: System MUST degrade to in-memory cache when Redis cache is unavailable
- **FR-075**: System MUST include X-Served-From-Cache header indicating cache source (memory, redis) when serving from cache
- **FR-076**: System MUST include X-Cache-Stale: true header when serving stale cached data due to backend failures
- **FR-077**: System MUST implement circuit breaker pattern for Redis connections with 3-failure threshold
- **FR-078**: System MUST return HTTP 503 when both database and all cache layers are unavailable
- **FR-079**: System MUST maintain readiness as healthy when serving from cache during database outage (liveness healthy, readiness degraded)

#### API Versioning and Stability
- **FR-080**: System MUST serve all endpoints under base path /countries/v1
- **FR-081**: System MUST maintain API compatibility within v1 (no breaking changes)
- **FR-082**: System MUST include API version in response headers (X-API-Version: v1)

#### Event Publishing
- **FR-083**: System MUST NOT publish create/update/delete events to message bus (downstream systems poll individual records or request full snapshots via GET /countries/v1/countries?includeInactive=all with pagination)

#### Audit and History
- **FR-084**: System MUST maintain audit log table for all write operations retaining 24 months of history
- **FR-085**: System MUST record user identity (createdBy, updatedBy) for all mutations
- **FR-086**: System MUST record timestamps (createdAt, updatedAt, deletedAt) for all records
- **FR-087**: System MUST include before/after snapshots in audit log for update operations

### Key Entities

- **Country**: Represents a sovereign nation or territory with standardized identification codes
  - Internal unique identifier (UUID)
  - ISO 3166-1 alpha-2 code (2-letter, mandatory, unique among active records)
  - ISO 3166-1 alpha-3 code (3-letter, optional, unique among active records when provided)
  - ISO 3166-1 numeric code (3-digit string, optional)
  - English name (official country name in English)
  - Local name (country name in primary local language, optional)
  - Region (geographic/political region: Africa, Americas, Asia, Europe, Oceania)
  - Subregion (more specific geographic area: Western Europe, Southeast Asia, etc.)
  - Phone calling code (international dialing code, e.g., "+66" for Thailand)
  - Currency code (ISO 4217 3-letter code, e.g., "THB")
  - Capital city name
  - Timezones array (list of IANA timezone identifiers, e.g., ["Asia/Bangkok"])
  - Active flag (boolean, indicates if country is currently active/valid)
  - Version (integer for optimistic concurrency control, increments on each update)
  - Metadata (optional JSON object for extensibility, e.g., flag emoji, geo coordinates, language codes)
  - Created by (user identifier who created the record)
  - Created at (timestamp of creation)
  - Updated by (user identifier who last updated the record)
  - Updated at (timestamp of last update)
  - Deleted at (timestamp of soft deletion, null if active)

- **AuditLog**: Captures history of all mutations to Country records
  - Unique identifier
  - Country identifier (foreign key to Country)
  - Operation type (CREATE, UPDATE, DELETE, HARD_DELETE)
  - User identity (who performed the operation)
  - Timestamp (when operation occurred)
  - Before snapshot (JSON representation of country state before change, null for CREATE)
  - After snapshot (JSON representation of country state after change, null for DELETE)
  - IP address (source of request, optional)
  - Retention period (12-24 months per regulatory requirements)

- **BulkImportJob**: Tracks asynchronous bulk import operations
  - Job identifier (UUID)
  - Submitted by (user who initiated import)
  - Submitted at (timestamp)
  - Status (PENDING, PROCESSING, COMPLETED, FAILED)
  - Total records (count in import file)
  - Processed records (count successfully processed; 0 if validation failed, all if succeeded due to all-or-nothing validation)
  - Failed records (count that failed validation; equals total if any validation errors occurred)
  - Error summary (JSON array of validation errors with row numbers, including duplicate ISO code conflicts with details)
  - Started at (timestamp processing began, optional)
  - Completed at (timestamp processing finished, optional)
  - Dry run flag (boolean indicating if this was a validation-only run)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Client applications can retrieve country data by ISO code with p95 latency under 50 milliseconds on standard small instance sizes
- **SC-002**: Client applications can retrieve full country list with p95 latency under 100 milliseconds on standard small instance sizes
- **SC-003**: Service achieves 99%+ cache hit rate for read operations under normal traffic patterns
- **SC-004**: Service maintains 99.9% availability for read operations during database outages by serving from cache
- **SC-005**: Service successfully handles 10,000 concurrent read requests without performance degradation
- **SC-006**: Service operates with minimal resource footprint (2 replicas on small VM/container sizes) handling typical country lookup traffic
- **SC-007**: Administrative users can complete country creation or update in under 2 seconds including validation
- **SC-008**: System prevents 100% of concurrent update conflicts through optimistic concurrency control (zero data loss from race conditions)
- **SC-009**: Bulk imports of 5,000 country records complete within 10 minutes without saturating read cache
- **SC-010**: Service response payload size reduced by 60%+ through compression for country list responses
- **SC-011**: Client applications can rely on ETag-based caching to reduce bandwidth consumption by 70%+ for repeated requests
- **SC-012**: Zero unauthorized access to administrative endpoints (100% enforcement of role-based access control)
- **SC-013**: All administrative operations audited with 100% traceability (user, timestamp, before/after state)
- **SC-014**: Service scales horizontally to handle 10x traffic increase by adding cache-capable replicas without increasing per-instance memory
- **SC-015**: System continues serving 95%+ of read requests from cache during planned database maintenance windows

## Assumptions

1. **Traffic patterns**: Read operations represent 99%+ of total traffic; writes are infrequent (few per day typically)
2. **Data volume**: Total country dataset is small (~250 active countries, <1 MB total uncompressed)
3. **Change frequency**: Country data changes rarely (quarterly or less frequently) except for minor corrections
4. **Downstream systems**: Consuming services can tolerate eventual consistency and will implement their own caching or periodic polling; snapshots are obtained via the standard country list endpoint with includeInactive=all parameter
5. **Authentication**: JWT authentication infrastructure already exists in the ecosystem (auth service, token validation)
6. **Infrastructure**: Redis or equivalent distributed cache is available in deployment environment
7. **Database**: PostgreSQL is used as primary data store (per Maliev project standards)
8. **Network**: All service-to-service communication occurs over private network with low latency (<10ms)
9. **Compliance**: No PII or sensitive data in country records; minimal regulatory constraints
10. **Deployment**: Service deployed to Kubernetes with standard health check and metrics integration (per Maliev GitOps patterns)
11. **Time zones**: IANA timezone database is available for validation
12. **ISO standards**: Service follows ISO 3166-1 (2013) standard for country codes and structure
13. **Default region/subregion values**: UN M49 standard regions used when not specified (assumption: standard geographic classifications)
14. **Bulk import format**: CSV and JSON are sufficient for administrative bulk operations; no Excel or XML support initially
15. **Cache eviction**: LRU eviction policy is appropriate for in-memory cache (assumption: access patterns show locality)
16. **Cache warming list**: The 50 most populous countries serve as a reasonable proxy for most-accessed countries; this static list is maintained in configuration and updated infrequently (e.g., annually based on UN population data)
