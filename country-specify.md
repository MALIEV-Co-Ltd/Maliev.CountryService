# CountryService Specification - Permission-Based Authorization Migration

## Overview
CountryService manages country reference data including ISO codes, names, phone codes, flags, and coordinates. Provides both public read-only access and administrative operations for managing country data.

## Permissions to Define

### Country Read Operations (Public)
```
country.countries.read          - Read country details (public access)
country.countries.list          - List all countries (public access)
country.countries.search        - Search countries (public access)
```

### Country Admin Operations
```
country.countries.create        - Create new countries
country.countries.update        - Update country information
country.countries.delete        - Soft delete countries
country.countries.hard-delete   - Permanently delete countries (critical)
country.countries.restore       - Restore soft-deleted countries
```

### Bulk Import Operations
```
country.import.upload           - Upload bulk import file
country.import.execute          - Execute bulk import
country.import.status           - View import job status
country.import.cancel           - Cancel running import jobs
country.import.history          - View import history
```

### System Operations
```
country.system.rebuild-cache    - Rebuild country cache
country.system.export           - Export all country data
country.system.view-stats       - View service statistics
```

## Predefined Roles

### country-admin
**Description**: Full control over country data
**Permissions**: All country.* permissions

### country-manager
**Description**: Manage country data except hard delete
**Permissions**:
- countries.read, countries.list, countries.search
- countries.create, countries.update, countries.delete, countries.restore
- import.* (all import operations)
- system.rebuild-cache, system.export, system.view-stats

### country-importer
**Description**: Execute bulk imports
**Permissions**:
- countries.read, countries.list
- import.upload, import.execute, import.status, import.history
- system.view-stats

### country-viewer
**Description**: Read-only access (default for public)
**Permissions**:
- countries.read, countries.list, countries.search

## Authorization Rules

### Public Access
Most read operations should be publicly accessible:
- GET /countries (list)
- GET /countries/{id}
- GET /countries/iso2/{code}
- GET /countries/iso3/{code}
- GET /countries/search

These endpoints can use `[AllowAnonymous]` or require minimal authentication.

### Admin-Only Operations
- All create/update/delete operations
- Bulk import management
- Hard delete (critical permission)
- System maintenance operations

### Critical Permissions
- `country.countries.hard-delete` - Permanent data loss

## Current Authorization Issues

The CountryService currently has authorization gaps:
1. **No admin protection** - AdminCountriesController lacks authorization
2. **Bulk import exposed** - Anyone can trigger bulk imports
3. **Hard delete unprotected** - No distinction between soft and hard delete permissions
4. **No cache management control** - System operations lack protection

## Migration Strategy

### Phase 1: Define Permissions & Roles (1.5 hours)
Create:
- `CountryPermissions.cs` - Define all 14 permissions
- `CountryPredefinedRoles.cs` - Define 4 predefined roles

### Phase 2: IAM Registration (1.5 hours)
- Create `CountryIAMRegistrationService.cs` (IHostedService)
- Register permissions and roles on startup

### Phase 3: Update Controllers (3 hours)
- **CountriesController**: Keep `[AllowAnonymous]` for public read operations
- **AdminCountriesController**: Add `[RequirePermission]` to all endpoints
- **BulkImportController**: Add `[RequirePermission]` for admin operations

### Phase 4: Update Tests (3 hours)
- Update integration tests to use `.WithTestAuth(CountryPermissions.X)`
- Add tests for public vs admin access
- Add tests for permission enforcement

### Phase 5: Deploy & Verify (1 hour)
- Deploy with feature flag `PermissionBasedAuthEnabled=false`
- Run smoke tests
- Enable feature flag
- Verify public access still works

## Feature Flag Configuration

```json
{
  "Features": {
    "PermissionBasedAuthEnabled": false
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080",
      "ServiceAccountToken": "<secret-from-vault>",
      "Timeout": 5000
    }
  }
}
```

## Success Criteria

- [ ] 14 permissions registered with IAM
- [ ] 4 predefined roles registered
- [ ] Admin controllers use `[RequirePermission]` attribute
- [ ] Public read operations remain accessible (anonymous or minimal auth)
- [ ] Hard delete requires critical permission
- [ ] Integration tests updated and passing
- [ ] Service registers permissions on startup

## Rollback Plan

1. Set `PermissionBasedAuthEnabled=false` in configuration
2. Restart service
3. Public access continues to work
4. No database changes required

## Estimated Effort

**Total**: ~10 hours (~1.5 days)
- Phase 1: 1.5 hours
- Phase 2: 1.5 hours
- Phase 3: 3 hours
- Phase 4: 3 hours
- Phase 5: 1 hour

## Dependencies

- IAM Service deployed and operational
- ServiceDefaults with RequirePermissionAttribute available
- JWT tokens include permissions claim

## Special Considerations

### Public API Design
CountryService provides reference data that should be widely accessible. Consider:
- Keep read operations with `[AllowAnonymous]`
- Only protect admin operations with permissions
- Use rate limiting for public endpoints instead of authentication
- Cache aggressively to reduce load

### ETags and Caching
The service uses ETags for caching. Permission checks should not interfere with ETag functionality.
