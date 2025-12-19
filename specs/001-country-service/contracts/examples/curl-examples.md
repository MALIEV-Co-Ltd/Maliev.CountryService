# Country Service API - cURL Examples

This document provides practical cURL examples for all Country Service API endpoints.

**Base URL**: `http://localhost:5000` (local development)

**Production URL**: `https://api.maliev.com` (replace in examples as needed)

---

## Table of Contents

1. [Public Read Endpoints](#public-read-endpoints)
2. [Admin Endpoints](#admin-endpoints)
3. [Bulk Import Endpoints](#bulk-import-endpoints)
4. [Health & Monitoring](#health--monitoring)

---

## Public Read Endpoints

### 1. Get Country by ID

```bash
curl -X GET "http://localhost:5000/country/v1/countries/1" \
  -H "Accept: application/json"
```

**Response**:
```json
{
  "id": 1,
  "iso2": "US",
  "iso3": "USA",
  "name": "United States",
  "region": "Americas",
  "subregion": "Northern America",
  "population": 331002651,
  "area": 9833517.0,
  "capital": "Washington, D.C.",
  "timezones": ["UTC-12:00", "UTC-11:00", "UTC-10:00", "UTC-09:00", "UTC-08:00", "UTC-07:00", "UTC-06:00", "UTC-05:00", "UTC-04:00", "UTC+10:00", "UTC+12:00"],
  "currencies": ["USD"],
  "languages": ["en"],
  "isActive": true,
  "createdAt": "2025-11-01T12:00:00Z",
  "lastModifiedAt": "2025-11-01T12:00:00Z"
}
```

**With ETag Support** (Conditional GET):
```bash
# First request - get ETag
ETAG=$(curl -sI "http://localhost:5000/country/v1/countries/1" | grep -i etag | cut -d' ' -f2 | tr -d '\r')

# Subsequent request - use If-None-Match
curl -X GET "http://localhost:5000/country/v1/countries/1" \
  -H "Accept: application/json" \
  -H "If-None-Match: $ETAG" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Expected**: `304 Not Modified` if data unchanged, `200 OK` with new ETag if data changed.

---

### 2. Get Country by ISO2 Code

```bash
curl -X GET "http://localhost:5000/country/v1/countries/iso2/US" \
  -H "Accept: application/json"
```

**Common ISO2 Codes**:
- `US` - United States
- `GB` - United Kingdom
- `FR` - France
- `DE` - Germany
- `JP` - Japan
- `CN` - China
- `IN` - India
- `BR` - Brazil
- `CA` - Canada
- `AU` - Australia

**Error Handling**:
```bash
# Invalid ISO2 code (wrong format)
curl -X GET "http://localhost:5000/country/v1/countries/iso2/USA" \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 400 Bad Request - "ISO2 must be exactly 2 uppercase letters"

# Valid format but non-existent country
curl -X GET "http://localhost:5000/country/v1/countries/iso2/ZZ" \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 404 Not Found
```

---

### 3. Get Country by ISO3 Code

```bash
curl -X GET "http://localhost:5000/country/v1/countries/iso3/USA" \
  -H "Accept: application/json"
```

**Common ISO3 Codes**:
- `USA` - United States
- `GBR` - United Kingdom
- `FRA` - France
- `DEU` - Germany
- `JPN` - Japan
- `CHN` - China
- `IND` - India
- `BRA` - Brazil
- `CAN` - Canada
- `AUS` - Australia

---

### 4. List Countries (Paginated)

**Basic List** (defaults: page=1, pageSize=10, sortBy=name, sortOrder=asc):
```bash
curl -X GET "http://localhost:5000/country/v1/countries" \
  -H "Accept: application/json"
```

**With Pagination**:
```bash
curl -X GET "http://localhost:5000/country/v1/countries?page=2&pageSize=20" \
  -H "Accept: application/json"
```

**With Sorting**:
```bash
# Sort by population (descending)
curl -X GET "http://localhost:5000/country/v1/countries?sortBy=population&sortOrder=desc&pageSize=10" \
  -H "Accept: application/json"

# Sort by name (ascending - default)
curl -X GET "http://localhost:5000/country/v1/countries?sortBy=name&sortOrder=asc" \
  -H "Accept: application/json"

# Sort by ISO2 code
curl -X GET "http://localhost:5000/country/v1/countries?sortBy=iso2&sortOrder=asc" \
  -H "Accept: application/json"
```

**With Filtering**:
```bash
# Filter by region
curl -X GET "http://localhost:5000/country/v1/countries?region=Europe&pageSize=50" \
  -H "Accept: application/json"

# Filter by region and subregion
curl -X GET "http://localhost:5000/country/v1/countries?region=Europe&subregion=Western%20Europe" \
  -H "Accept: application/json"
```

**Response Headers** (pagination metadata):
```bash
curl -I "http://localhost:5000/country/v1/countries?page=1&pageSize=20" | grep -E "X-Total-Count|X-Page-Size"
# X-Total-Count: 195
# X-Page-Size: 20
```

**Response Body**:
```json
{
  "data": [
    {
      "id": 1,
      "iso2": "AF",
      "iso3": "AFG",
      "name": "Afghanistan",
      "region": "Asia",
      "subregion": "Southern Asia",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 195,
  "totalPages": 10
}
```

---

### 5. Search Countries

**Basic Search**:
```bash
curl -X GET "http://localhost:5000/country/v1/countries/search?q=united" \
  -H "Accept: application/json"
```

**Paginated Search**:
```bash
curl -X GET "http://localhost:5000/country/v1/countries/search?q=island&page=1&pageSize=10" \
  -H "Accept: application/json"
```

**Search Examples**:
```bash
# Search for "Republic"
curl -X GET "http://localhost:5000/country/v1/countries/search?q=republic" \
  -H "Accept: application/json"

# Search for "Kingdom"
curl -X GET "http://localhost:5000/country/v1/countries/search?q=kingdom" \
  -H "Accept: application/json"

# Search for partial matches
curl -X GET "http://localhost:5000/country/v1/countries/search?q=stan" \
  -H "Accept: application/json"
```

**Response**:
```json
{
  "data": [
    {
      "id": 233,
      "iso2": "GB",
      "iso3": "GBR",
      "name": "United Kingdom",
      "region": "Europe",
      "subregion": "Northern Europe",
      "isActive": true
    },
    {
      "id": 234,
      "iso2": "US",
      "iso3": "USA",
      "name": "United States",
      "region": "Americas",
      "subregion": "Northern America",
      "isActive": true
    }
  ],
  "page": 1,
  "pageSize": 10,
  "totalCount": 2,
  "totalPages": 1
}
```

---

## Admin Endpoints

**Prerequisites**: All admin endpoints require JWT authentication with appropriate roles.

### Setting Up Authentication

```bash
# Set your JWT token (replace with actual token)
export JWT_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

# Or use inline (replace in examples below)
AUTHORIZATION_HEADER="Authorization: Bearer YOUR_JWT_TOKEN_HERE"
```

**Required Policies**:
- **CountryAdmin**: Can create, update, and soft-delete countries
- **SuperAdmin**: Can hard-delete countries and perform all admin operations

---

### 6. Create Country

**Request**:
```bash
curl -X POST "http://localhost:5000/country/v1/admin/countries" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d @create-country-request.json
```

**Request Body** (`create-country-request.json`):
```json
{
  "iso2": "XX",
  "iso3": "XXX",
  "name": "Example Country",
  "region": "Europe",
  "subregion": "Western Europe",
  "population": 10000000,
  "area": 50000.0,
  "capital": "Example Capital",
  "timezones": ["UTC+01:00"],
  "currencies": ["EUR"],
  "languages": ["en", "fr"]
}
```

**Inline JSON Example**:
```bash
curl -X POST "http://localhost:5000/country/v1/admin/countries" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "iso2": "XY",
    "iso3": "XYZ",
    "name": "Test Country",
    "region": "Asia",
    "subregion": "Eastern Asia"
  }'
```

**Response** (201 Created):
```json
{
  "id": 196,
  "iso2": "XX",
  "iso3": "XXX",
  "name": "Example Country",
  "region": "Europe",
  "subregion": "Western Europe",
  "population": 10000000,
  "area": 50000.0,
  "capital": "Example Capital",
  "timezones": ["UTC+01:00"],
  "currencies": ["EUR"],
  "languages": ["en", "fr"],
  "isActive": true,
  "createdAt": "2025-11-02T10:30:00Z",
  "lastModifiedAt": "2025-11-02T10:30:00Z"
}
```

**Error Handling**:
```bash
# Duplicate ISO2 code
curl -X POST "http://localhost:5000/country/v1/admin/countries" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"iso2": "US", "iso3": "XXX", "name": "Duplicate"}' \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 409 Conflict - "Country with ISO2 'US' already exists"

# Invalid data
curl -X POST "http://localhost:5000/country/v1/admin/countries" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"iso2": "ABC", "iso3": "XXX", "name": "Invalid"}' \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 400 Bad Request - validation errors
```

---

### 7. Update Country (Full Update)

**Request** (requires `If-Match` header with ETag):
```bash
# Get current ETag
ETAG=$(curl -sI "http://localhost:5000/country/v1/countries/196" | grep -i etag | cut -d' ' -f2 | tr -d '\r')

# Update country
curl -X PUT "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: $ETAG" \
  -d '{
    "iso2": "XX",
    "iso3": "XXX",
    "name": "Updated Example Country",
    "region": "Europe",
    "subregion": "Western Europe",
    "population": 11000000,
    "area": 55000.0,
    "capital": "Updated Capital",
    "timezones": ["UTC+01:00", "UTC+02:00"],
    "currencies": ["EUR"],
    "languages": ["en", "fr", "de"]
  }'
```

**Optimistic Concurrency Conflict**:
```bash
# Attempt update with stale ETag
curl -X PUT "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: \"stale-etag-value\"" \
  -d '{"iso2": "XX", "iso3": "XXX", "name": "Updated"}' \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 412 Precondition Failed - "The country has been modified. Please refresh and try again."
```

**Missing If-Match Header**:
```bash
curl -X PUT "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"iso2": "XX", "iso3": "XXX", "name": "Updated"}' \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 428 Precondition Required - "If-Match header is required for updates"
```

---

### 8. Partial Update Country (PATCH)

**Request**:
```bash
# Get current ETag
ETAG=$(curl -sI "http://localhost:5000/country/v1/countries/196" | grep -i etag | cut -d' ' -f2 | tr -d '\r')

# Update only population and capital
curl -X PATCH "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: $ETAG" \
  -d '{
    "population": 12000000,
    "capital": "New Capital"
  }'
```

**Examples**:
```bash
# Update only name
curl -X PATCH "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: $ETAG" \
  -d '{"name": "New Name"}'

# Update region and subregion
curl -X PATCH "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: $ETAG" \
  -d '{"region": "Asia", "subregion": "Southern Asia"}'

# Update multiple fields
curl -X PATCH "http://localhost:5000/country/v1/admin/countries/196" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -H "If-Match: $ETAG" \
  -d '{
    "population": 15000000,
    "area": 60000.0,
    "timezones": ["UTC+01:00"],
    "currencies": ["EUR", "USD"]
  }'
```

---

### 9. Soft Delete Country

**Request** (CountryAdmin role required):
```bash
curl -X DELETE "http://localhost:5000/country/v1/admin/countries/196?hard=false" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Response**: `204 No Content`

**Behavior**:
- Sets `IsActive = false`
- Country remains in database
- Excluded from public read endpoints (by default)
- Can be restored by updating `IsActive = true`

**Verify Soft Delete**:
```bash
# Country should return 404 or be excluded from public endpoints
curl -X GET "http://localhost:5000/country/v1/countries/196" \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 404 Not Found (or 200 with isActive=false depending on implementation)
```

---

### 10. Hard Delete Country

**Request** (SuperAdmin role required):
```bash
curl -X DELETE "http://localhost:5000/country/v1/admin/countries/196?hard=true" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Response**: `204 No Content`

**Behavior**:
- Permanently removes country from database
- **Cannot be undone**
- Returns 404 on subsequent GET requests

**Permission Denied**:
```bash
# CountryAdmin attempting hard delete (only SuperAdmin allowed)
curl -X DELETE "http://localhost:5000/country/v1/admin/countries/196?hard=true" \
  -H "Authorization: Bearer $JWT_TOKEN_COUNTRY_ADMIN" \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 403 Forbidden - "SuperAdmin role required for hard delete"
```

---

## Bulk Import Endpoints

### 11. Submit Bulk Import Job

**Request**:
```bash
curl -X POST "http://localhost:5000/country/v1/admin/bulk-import" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d @bulk-import-request.json
```

**Request Body** (`bulk-import-request.json` - max 1,000 countries):
```json
{
  "countries": [
    {
      "iso2": "AA",
      "iso3": "AAA",
      "name": "Test Country 1",
      "region": "Europe",
      "subregion": "Western Europe"
    },
    {
      "iso2": "BB",
      "iso3": "BBB",
      "name": "Test Country 2",
      "region": "Asia",
      "subregion": "Eastern Asia"
    }
  ]
}
```

**Response** (202 Accepted):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "totalRecords": 2,
  "createdAt": "2025-11-02T11:00:00Z"
}
```

**Error - Too Many Records**:
```bash
# Attempt to import > 1,000 countries
curl -X POST "http://localhost:5000/country/v1/admin/bulk-import" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"countries": [...]}' \
  -w "\nHTTP Status: %{http_code}\n"
# Returns: 400 Bad Request - "Cannot import more than 1,000 countries at once"
```

---

### 12. Get Bulk Import Job Status

**Request**:
```bash
export JOB_ID="550e8400-e29b-41d4-a716-446655440000"

curl -X GET "http://localhost:5000/country/v1/admin/bulk-import/$JOB_ID" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -H "Accept: application/json"
```

**Response** (Pending):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Pending",
  "totalRecords": 2,
  "processedRecords": 0,
  "failedRecords": 0,
  "validationErrors": [],
  "createdAt": "2025-11-02T11:00:00Z",
  "startedAt": null,
  "completedAt": null
}
```

**Response** (In Progress):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "InProgress",
  "totalRecords": 2,
  "processedRecords": 1,
  "failedRecords": 0,
  "validationErrors": [],
  "createdAt": "2025-11-02T11:00:00Z",
  "startedAt": "2025-11-02T11:05:00Z",
  "completedAt": null
}
```

**Response** (Completed):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Completed",
  "totalRecords": 2,
  "processedRecords": 2,
  "failedRecords": 0,
  "validationErrors": [],
  "createdAt": "2025-11-02T11:00:00Z",
  "startedAt": "2025-11-02T11:05:00Z",
  "completedAt": "2025-11-02T11:10:00Z"
}
```

**Response** (Failed with Validation Errors):
```json
{
  "jobId": "550e8400-e29b-41d4-a716-446655440000",
  "status": "Failed",
  "totalRecords": 2,
  "processedRecords": 0,
  "failedRecords": 2,
  "validationErrors": [
    {
      "recordIndex": 0,
      "field": "iso2",
      "error": "ISO2 'AA' already exists for country ID 123"
    },
    {
      "recordIndex": 1,
      "field": "iso3",
      "error": "ISO3 must be exactly 3 uppercase letters"
    }
  ],
  "createdAt": "2025-11-02T11:00:00Z",
  "startedAt": "2025-11-02T11:05:00Z",
  "completedAt": "2025-11-02T11:05:30Z"
}
```

---

### 13. Process Bulk Import Job

**Request**:
```bash
curl -X POST "http://localhost:5000/country/v1/admin/bulk-import/$JOB_ID/process" \
  -H "Authorization: Bearer $JWT_TOKEN" \
  -w "\nHTTP Status: %{http_code}\n"
```

**Response**: `202 Accepted`

**Behavior**:
- Starts background processing of validated job
- Returns immediately (async processing)
- Use "Get Job Status" endpoint to poll for completion
- Processing time: ~100 records/second

**Polling Example**:
```bash
#!/bin/bash
JOB_ID="550e8400-e29b-41d4-a716-446655440000"

while true; do
  STATUS=$(curl -s "http://localhost:5000/country/v1/admin/bulk-import/$JOB_ID" \
    -H "Authorization: Bearer $JWT_TOKEN" | jq -r '.status')

  echo "Job status: $STATUS"

  if [[ "$STATUS" == "Completed" || "$STATUS" == "Failed" ]]; then
    echo "Job finished!"
    curl -s "http://localhost:5000/country/v1/admin/bulk-import/$JOB_ID" \
      -H "Authorization: Bearer $JWT_TOKEN" | jq
    break
  fi

  sleep 5
done
```

---

## Health & Monitoring

### 14. Liveness Probe

**Request**:
```bash
curl -X GET "http://localhost:5000/country/v1/liveness"
```

**Response**: `200 OK` with body `"Healthy"`

**Purpose**: Indicates if the service is running and responsive (used by Kubernetes for liveness probe).

---

### 15. Readiness Probe

**Request**:
```bash
curl -X GET "http://localhost:5000/country/v1/readiness" \
  -H "Accept: application/json"
```

**Response - Healthy** (200 OK):
```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456",
  "entries": {
    "Maliev.CountryService.Data.CountryServiceDbContext": {
      "status": "Healthy",
      "duration": "00:00:00.0100000"
    }
  }
}
```

**Response - Degraded** (200 OK with degraded status):
```json
{
  "status": "Degraded",
  "totalDuration": "00:00:01.5000000",
  "entries": {
    "Maliev.CountryService.Data.CountryServiceDbContext": {
      "status": "Degraded",
      "description": "Database connection slow (latency > 1s)",
      "duration": "00:00:01.5000000"
    }
  }
}
```

**Response - Unhealthy** (503 Service Unavailable):
```json
{
  "status": "Unhealthy",
  "totalDuration": "00:00:05.0000000",
  "entries": {
    "Maliev.CountryService.Data.CountryServiceDbContext": {
      "status": "Unhealthy",
      "description": "Database connection failed: timeout expired",
      "duration": "00:00:05.0000000",
      "exception": "Npgsql.NpgsqlException: Connection timeout"
    }
  }
}
```

**Headers**:
```bash
curl -I "http://localhost:5000/country/v1/readiness"
# X-Degraded-Mode: true (if serving from stale cache during DB outage)
```

---

### 16. Prometheus Metrics

**Request**:
```bash
curl -X GET "http://localhost:5000/metrics"
```

**Response** (Prometheus exposition format):
```
# HELP country_cache_hits_total Total number of cache hits
# TYPE country_cache_hits_total counter
country_cache_hits_total{cache_type="redis"} 12345
country_cache_hits_total{cache_type="memory"} 6789

# HELP country_cache_misses_total Total number of cache misses
# TYPE country_cache_misses_total counter
country_cache_misses_total{cache_type="redis"} 123

# HELP country_request_duration_seconds Request duration in seconds
# TYPE country_request_duration_seconds histogram
country_request_duration_seconds_bucket{le="0.01"} 1000
country_request_duration_seconds_bucket{le="0.05"} 4500
country_request_duration_seconds_bucket{le="0.1"} 4800
country_request_duration_seconds_bucket{le="0.5"} 5000
country_request_duration_seconds_sum 125.5
country_request_duration_seconds_count 5000

# HELP country_circuit_breaker_state Circuit breaker state (0=Closed, 1=Open, 2=Half-Open)
# TYPE country_circuit_breaker_state gauge
country_circuit_breaker_state{service="redis"} 0

# HELP country_active_total Total number of active countries
# TYPE country_active_total gauge
country_active_total 195

# HELP country_create_operations_total Total country create operations
# TYPE country_create_operations_total counter
country_create_operations_total{status="success"} 10
country_create_operations_total{status="failure"} 2

# HELP country_bulk_import_jobs_total Total bulk import jobs
# TYPE country_bulk_import_jobs_total counter
country_bulk_import_jobs_total{status="completed"} 5
country_bulk_import_jobs_total{status="failed"} 1

# Standard HTTP metrics
http_requests_received_total{code="200",method="GET",route="/country/v1/countries/{id}"} 10000
http_requests_received_total{code="404",method="GET",route="/country/v1/countries/{id}"} 50
http_request_duration_seconds_bucket{le="0.05",route="/country/v1/countries/iso2/{iso2}"} 9500
```

**Filtering Metrics**:
```bash
# Only country-specific metrics
curl -s "http://localhost:5000/metrics" | grep "^country_"

# Only HTTP metrics
curl -s "http://localhost:5000/metrics" | grep "^http_"
```

---

## Rate Limiting

All endpoints have rate limits applied:

**Read Endpoints** (100 requests/minute per IP):
```bash
# Exceeding rate limit
for i in {1..101}; do
  curl -s -w "%{http_code}\n" "http://localhost:5000/country/v1/countries/1" > /dev/null
done
# First 100: 200 OK
# 101st: 429 Too Many Requests

# Check rate limit headers
curl -I "http://localhost:5000/country/v1/countries/1" | grep -E "X-RateLimit|Retry-After"
# X-RateLimit-Limit: 100
# X-RateLimit-Remaining: 95
# X-RateLimit-Reset: 1699012800
```

**Admin Endpoints** (20 requests/minute per user):
```bash
curl -I "http://localhost:5000/country/v1/admin/countries" \
  -H "Authorization: Bearer $JWT_TOKEN" | grep "X-RateLimit"
# X-RateLimit-Limit: 20
# X-RateLimit-Remaining: 19
```

**Response** (429 Too Many Requests):
```json
{
  "error": "Rate limit exceeded",
  "retryAfter": 42
}
```

**Headers**:
- `Retry-After: 42` (seconds until rate limit resets)
- `X-RateLimit-Limit`: Maximum requests allowed
- `X-RateLimit-Remaining`: Requests remaining in current window
- `X-RateLimit-Reset`: Unix timestamp when rate limit resets

---

## Error Response Format

All error responses follow a consistent structure:

**400 Bad Request** (Validation Error):
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Iso2": ["ISO2 must be exactly 2 uppercase letters"],
    "Name": ["Name is required"]
  }
}
```

**401 Unauthorized**:
```json
{
  "error": "Unauthorized",
  "message": "Valid JWT token required"
}
```

**403 Forbidden**:
```json
{
  "error": "Forbidden",
  "message": "CountryAdmin role required for this operation"
}
```

**404 Not Found**:
```json
{
  "error": "NotFound",
  "message": "Country with ID 999 not found"
}
```

**409 Conflict**:
```json
{
  "error": "Conflict",
  "message": "Country with ISO2 'US' already exists"
}
```

**412 Precondition Failed** (Optimistic Concurrency):
```json
{
  "error": "PreconditionFailed",
  "message": "The country has been modified by another user. Please refresh and try again.",
  "currentETag": "\"abc123\""
}
```

**428 Precondition Required**:
```json
{
  "error": "PreconditionRequired",
  "message": "If-Match header is required for update operations"
}
```

**429 Too Many Requests**:
```json
{
  "error": "TooManyRequests",
  "message": "Rate limit exceeded. Try again in 42 seconds.",
  "retryAfter": 42
}
```

**500 Internal Server Error**:
```json
{
  "error": "InternalServerError",
  "message": "An unexpected error occurred. Please try again later."
}
```

**503 Service Unavailable** (Circuit Breaker Open):
```json
{
  "error": "ServiceUnavailable",
  "message": "Service temporarily unavailable due to high failure rate. Please try again in 60 seconds.",
  "retryAfter": 60
}
```

---

## Advanced Usage

### Cache Busting

Force cache refresh by setting `Cache-Control: no-cache`:
```bash
curl -X GET "http://localhost:5000/country/v1/countries/iso2/US" \
  -H "Cache-Control: no-cache" \
  -H "Accept: application/json"
```

### Debugging

**Verbose Output**:
```bash
curl -v "http://localhost:5000/country/v1/countries/1"
```

**Timing Information**:
```bash
curl -w "\nTime: %{time_total}s\n" "http://localhost:5000/country/v1/countries/iso2/US"
```

**Save Response to File**:
```bash
curl "http://localhost:5000/country/v1/countries?pageSize=100" -o countries.json
```

### Scripting Examples

**Batch Create Countries**:
```bash
#!/bin/bash
for i in {1..10}; do
  curl -X POST "http://localhost:5000/country/v1/admin/countries" \
    -H "Authorization: Bearer $JWT_TOKEN" \
    -H "Content-Type: application/json" \
    -d "{\"iso2\":\"Z$i\",\"iso3\":\"Z0$i\",\"name\":\"Test Country $i\",\"region\":\"Test\"}"
  echo ""
done
```

**Download All Countries**:
```bash
#!/bin/bash
PAGE=1
while true; do
  RESPONSE=$(curl -s "http://localhost:5000/country/v1/countries?page=$PAGE&pageSize=100")
  TOTAL_PAGES=$(echo $RESPONSE | jq -r '.totalPages')

  echo $RESPONSE | jq '.data[]' >> all_countries.json

  if [ $PAGE -ge $TOTAL_PAGES ]; then
    break
  fi

  PAGE=$((PAGE + 1))
done
```

---

## Notes

- Replace `http://localhost:5000` with your actual service URL
- Replace `$JWT_TOKEN` with your actual JWT bearer token
- ISO2 codes are **2 uppercase letters** (e.g., `US`, `GB`)
- ISO3 codes are **3 uppercase letters** (e.g., `USA`, `GBR`)
- All admin endpoints require authentication
- ETag-based optimistic concurrency control is **mandatory** for updates
- Rate limits are **per IP** for read endpoints and **per user** for admin endpoints
- Circuit breaker opens after 50% failure rate, stays open for 60 seconds

---

## See Also

- [API Specification (OpenAPI)](../openapi.yaml)
- [Quick Start Guide](../../quickstart.md)
- [README](../../../README.md)
