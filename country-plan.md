# CountryService Implementation Plan - Permission-Based Authorization

## Total Effort: ~10 hours (~1.5 days)

## Phase 1: Define Permissions & Roles (1.5 hours)

### Step 1.1: Create CountryPermissions.cs
**Location**: `Maliev.CountryService.Api/Authorization/CountryPermissions.cs`

```csharp
public static class CountryPermissions
{
    // Country Read Operations (Public)
    public const string CountriesRead = "country.countries.read";
    public const string CountriesList = "country.countries.list";
    public const string CountriesSearch = "country.countries.search";

    // Country Admin Operations
    public const string CountriesCreate = "country.countries.create";
    public const string CountriesUpdate = "country.countries.update";
    public const string CountriesDelete = "country.countries.delete";
    public const string CountriesHardDelete = "country.countries.hard-delete";
    public const string CountriesRestore = "country.countries.restore";

    // Bulk Import Operations
    public const string ImportUpload = "country.import.upload";
    public const string ImportExecute = "country.import.execute";
    public const string ImportStatus = "country.import.status";
    public const string ImportCancel = "country.import.cancel";
    public const string ImportHistory = "country.import.history";

    // System Operations
    public const string SystemRebuildCache = "country.system.rebuild-cache";
    public const string SystemExport = "country.system.export";
    public const string SystemViewStats = "country.system.view-stats";

    public static readonly string[] All = new[]
    {
        CountriesRead, CountriesList, CountriesSearch,
        CountriesCreate, CountriesUpdate, CountriesDelete, CountriesHardDelete, CountriesRestore,
        ImportUpload, ImportExecute, ImportStatus, ImportCancel, ImportHistory,
        SystemRebuildCache, SystemExport, SystemViewStats
    };
}
```

### Step 1.2: Create CountryPredefinedRoles.cs
**Location**: `Maliev.CountryService.Api/Authorization/CountryPredefinedRoles.cs`

```csharp
public static class CountryPredefinedRoles
{
    public static readonly RoleRegistration Admin = new()
    {
        RoleId = "country-admin",
        RoleName = "Country Administrator",
        Description = "Full control over country data",
        Permissions = CountryPermissions.All
    };

    public static readonly RoleRegistration Manager = new()
    {
        RoleId = "country-manager",
        RoleName = "Country Manager",
        Description = "Manage country data except hard delete",
        Permissions = new[]
        {
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.CountriesSearch,
            CountryPermissions.CountriesCreate,
            CountryPermissions.CountriesUpdate,
            CountryPermissions.CountriesDelete,
            CountryPermissions.CountriesRestore,
            CountryPermissions.ImportUpload,
            CountryPermissions.ImportExecute,
            CountryPermissions.ImportStatus,
            CountryPermissions.ImportCancel,
            CountryPermissions.ImportHistory,
            CountryPermissions.SystemRebuildCache,
            CountryPermissions.SystemExport,
            CountryPermissions.SystemViewStats
        }
    };

    public static readonly RoleRegistration Importer = new()
    {
        RoleId = "country-importer",
        RoleName = "Country Data Importer",
        Description = "Execute bulk imports",
        Permissions = new[]
        {
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.ImportUpload,
            CountryPermissions.ImportExecute,
            CountryPermissions.ImportStatus,
            CountryPermissions.ImportHistory,
            CountryPermissions.SystemViewStats
        }
    };

    public static readonly RoleRegistration Viewer = new()
    {
        RoleId = "country-viewer",
        RoleName = "Country Data Viewer",
        Description = "Read-only access to country data",
        Permissions = new[]
        {
            CountryPermissions.CountriesRead,
            CountryPermissions.CountriesList,
            CountryPermissions.CountriesSearch
        }
    };

    public static readonly RoleRegistration[] All = new[]
    {
        Admin, Manager, Importer, Viewer
    };
}
```

## Phase 2: IAM Registration (1.5 hours)

### Step 2.1: Create CountryIAMRegistrationService.cs
**Location**: `Maliev.CountryService.Api/Services/CountryIAMRegistrationService.cs`

```csharp
public class CountryIAMRegistrationService : IHostedService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CountryIAMRegistrationService> _logger;
    private readonly IConfiguration _configuration;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var iamEnabled = _configuration.GetValue<bool>("Features:PermissionBasedAuthEnabled");
        if (!iamEnabled)
        {
            _logger.LogInformation("IAM registration skipped (PermissionBasedAuthEnabled=false)");
            return;
        }

        var client = _httpClientFactory.CreateClient("IAMService");

        // Register permissions
        await client.PostAsJsonAsync("/api/v1/permissions/register", new
        {
            ServiceName = "CountryService",
            Permissions = CountryPermissions.All.Select(p => new
            {
                PermissionId = p,
                Description = $"Permission: {p}",
                IsCritical = p == CountryPermissions.CountriesHardDelete
            })
        }, cancellationToken);

        // Register roles
        await client.PostAsJsonAsync("/api/v1/roles/register", new
        {
            ServiceName = "CountryService",
            Roles = CountryPredefinedRoles.All
        }, cancellationToken);

        _logger.LogInformation("Registered {Count} permissions and {RoleCount} roles with IAM",
            CountryPermissions.All.Length, CountryPredefinedRoles.All.Length);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
```

### Step 2.2: Register Service in Program.cs
```csharp
builder.Services.AddHostedService<CountryIAMRegistrationService>();
```

## Phase 3: Update Controllers (3 hours)

### Step 3.1: CountriesController.cs
**Keep Public Read Access** - No changes needed if already using `[AllowAnonymous]`

```csharp
[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous] // Public API
public class CountriesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() { } // Public

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(long id) { } // Public

    [HttpGet("iso2/{iso2}")]
    public async Task<IActionResult> GetByIso2(string iso2) { } // Public

    [HttpGet("iso3/{iso3}")]
    public async Task<IActionResult> GetByIso3(string iso3) { } // Public

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q) { } // Public
}
```

### Step 3.2: AdminCountriesController.cs
**Add Permission Checks**

```csharp
[ApiController]
[Route("api/v1/admin/countries")]
public class AdminCountriesController : ControllerBase
{
    [HttpPost]
    [RequirePermission(CountryPermissions.CountriesCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCountryRequest request) { }

    [HttpPut("{id}")]
    [RequirePermission(CountryPermissions.CountriesUpdate)]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateCountryRequest request) { }

    [HttpDelete("{id}")]
    [RequirePermission(CountryPermissions.CountriesDelete)]
    public async Task<IActionResult> SoftDelete(long id) { }

    [HttpDelete("{id}/hard")]
    [RequirePermission(CountryPermissions.CountriesHardDelete)] // Critical permission
    public async Task<IActionResult> HardDelete(long id) { }

    [HttpPost("{id}/restore")]
    [RequirePermission(CountryPermissions.CountriesRestore)]
    public async Task<IActionResult> Restore(long id) { }

    [HttpPost("rebuild-cache")]
    [RequirePermission(CountryPermissions.SystemRebuildCache)]
    public async Task<IActionResult> RebuildCache() { }

    [HttpGet("export")]
    [RequirePermission(CountryPermissions.SystemExport)]
    public async Task<IActionResult> ExportAll() { }
}
```

### Step 3.3: BulkImportController.cs
**Add Permission Checks**

```csharp
[ApiController]
[Route("api/v1/bulk-import")]
public class BulkImportController : ControllerBase
{
    [HttpPost("upload")]
    [RequirePermission(CountryPermissions.ImportUpload)]
    public async Task<IActionResult> UploadFile(IFormFile file) { }

    [HttpPost("execute")]
    [RequirePermission(CountryPermissions.ImportExecute)]
    public async Task<IActionResult> ExecuteImport([FromBody] ExecuteImportRequest request) { }

    [HttpGet("jobs/{jobId}")]
    [RequirePermission(CountryPermissions.ImportStatus)]
    public async Task<IActionResult> GetJobStatus(Guid jobId) { }

    [HttpPost("jobs/{jobId}/cancel")]
    [RequirePermission(CountryPermissions.ImportCancel)]
    public async Task<IActionResult> CancelJob(Guid jobId) { }

    [HttpGet("history")]
    [RequirePermission(CountryPermissions.ImportHistory)]
    public async Task<IActionResult> GetImportHistory() { }
}
```

## Phase 4: Update Tests (3 hours)

### Step 4.1: Update Integration Tests
```csharp
[Fact]
public async Task GetCountries_Anonymous_ReturnsOk()
{
    // Public API should work without authentication
    var response = await _client.GetAsync("/api/v1/countries");
    response.StatusCode.Should().Be(HttpStatusCode.OK);
}

[Fact]
public async Task CreateCountry_WithPermission_ReturnsCreated()
{
    var response = await _client
        .WithTestAuth(CountryPermissions.CountriesCreate)
        .PostAsJsonAsync("/api/v1/admin/countries", new CreateCountryRequest { ... });

    response.StatusCode.Should().Be(HttpStatusCode.Created);
}

[Fact]
public async Task CreateCountry_WithoutPermission_ReturnsForbidden()
{
    var response = await _client
        .WithTestAuth() // No permissions
        .PostAsJsonAsync("/api/v1/admin/countries", new CreateCountryRequest { ... });

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task HardDelete_RequiresCriticalPermission()
{
    var response = await _client
        .WithTestAuth(CountryPermissions.CountriesDelete) // Wrong permission
        .DeleteAsync("/api/v1/admin/countries/1/hard");

    response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
}

[Fact]
public async Task BulkImport_WithPermission_ReturnsAccepted()
{
    var response = await _client
        .WithTestAuth(CountryPermissions.ImportExecute)
        .PostAsJsonAsync("/api/v1/bulk-import/execute", new ExecuteImportRequest { ... });

    response.StatusCode.Should().Be(HttpStatusCode.Accepted);
}
```

## Phase 5: Deploy & Verify (1 hour)

### Step 5.1: Deploy to Dev
1. Deploy CountryService with `PermissionBasedAuthEnabled=false`
2. Verify service starts successfully
3. Verify IAM registration skipped
4. Verify public API still works

### Step 5.2: Enable IAM
1. Set `PermissionBasedAuthEnabled=true`
2. Restart service
3. Verify permissions registered with IAM

### Step 5.3: Smoke Tests
- Test public access to countries (anonymous)
- Test admin operations with admin role
- Test bulk import with importer role
- Test permission denial for unauthorized operations

### Step 5.4: Monitor
- Watch for authorization errors in logs
- Verify public API performance unchanged
- Check hard delete protection works

## Critical Files

- `Maliev.CountryService.Api/Authorization/CountryPermissions.cs`
- `Maliev.CountryService.Api/Authorization/CountryPredefinedRoles.cs`
- `Maliev.CountryService.Api/Services/CountryIAMRegistrationService.cs`
- `Maliev.CountryService.Api/Controllers/AdminCountriesController.cs`
- `Maliev.CountryService.Api/Controllers/BulkImportController.cs`

## Success Checklist

- [ ] CountryPermissions.cs created with 14 permissions
- [ ] CountryPredefinedRoles.cs created with 4 roles
- [ ] Hard delete marked as critical permission
- [ ] CountryIAMRegistrationService.cs implemented
- [ ] AdminCountriesController updated with [RequirePermission]
- [ ] BulkImportController updated with [RequirePermission]
- [ ] Public CountriesController remains anonymous/open
- [ ] Integration tests updated and passing
- [ ] Feature flag configuration added
- [ ] Deployed to dev and verified
- [ ] Public API performance unchanged
- [ ] IAM registration successful
