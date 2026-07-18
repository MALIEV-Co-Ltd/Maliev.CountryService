# Quickstart: IAM Integration

## Configuration

To enable permission-based authorization, update your `appsettings.json` or environment variables:

```json
{
  "Features": {
    "PermissionBasedAuthEnabled": true
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080",
      "ServiceAccountToken": "your-token-here"
    }
  }
}
```

## Running Locally

1. Ensure the IAM Service (or a mock) is running at the configured `BaseUrl`.
2. Start the CountryService.
3. Check the logs for `Registered 14 permissions and 4 roles with IAM`.

## Testing Authorization

### Admin Access
To call `POST /api/v1/admin/countries`, you must provide a JWT with the `country.countries.create` permission.

```bash
curl -X POST http://localhost:8080/api/v1/admin/countries \
  -H "Authorization: Bearer <JWT_WITH_PERMISSION>" \
  -H "Content-Type: application/json" \
  -d '{"name": "New Country", "iso2": "NC"}'
```

### Public Access
Public endpoints remain accessible without a token:

```bash
curl http://localhost:8080/api/v1/countries
```
