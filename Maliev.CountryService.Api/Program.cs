using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using HealthChecks.UI.Client;
using Maliev.CountryService.Api.Configurations;
using Maliev.CountryService.Api.HealthChecks;
using Maliev.CountryService.Api.Middleware;
using Maliev.CountryService.Api.Models;
using Maliev.CountryService.Api.Services;
using Maliev.CountryService.Data.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Filters;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .Enrich.WithProcessId()
    .Enrich.WithThreadId()
    .Filter.ByExcluding(Matching.WithProperty<string>("RequestPath", path =>
        path.StartsWith("/health") || path.StartsWith("/metrics")))
    .WriteTo.Console(outputTemplate:
        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/country-service-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 31,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {CorrelationId} {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Maliev Country Service");

    // Load secrets.yaml
    builder.Configuration.AddYamlFile("secrets.yaml", optional: true, reloadOnChange: true);

    // Load secrets from mounted volume in GKE
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
    }

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi();
    
    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
    builder.Services.AddSwaggerGen();

    // Configure strongly-typed configuration options with validation
    builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection(RateLimitOptions.SectionName));
    builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.SectionName));

    // Configure JWT options only if available (to allow local development without secrets)
    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    if (!string.IsNullOrEmpty(jwtSection["Issuer"]) && !builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.Configure<JwtOptions>(jwtSection);
        builder.Services.AddOptions<JwtOptions>()
            .Bind(jwtSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    builder.Services.AddOptions<RateLimitOptions>()
        .Bind(builder.Configuration.GetSection(RateLimitOptions.SectionName))
        .ValidateDataAnnotations();

    builder.Services.AddOptions<CacheOptions>()
        .Bind(builder.Configuration.GetSection(CacheOptions.SectionName))
        .ValidateDataAnnotations();

    // Configure Country DbContext
    if (builder.Environment.IsEnvironment("Testing"))
    {
        builder.Services.AddDbContext<CountryDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
    }
    else
    {
        builder.Services.AddDbContext<CountryDbContext>(options =>
        {
            options.UseNpgsql(builder.Configuration.GetConnectionString("CountryDbContext"));
        });
    }

    builder.Services.AddDatabaseDeveloperPageExceptionFilter();

    // Configure Memory Cache
    builder.Services.AddMemoryCache(options =>
    {
        var cacheOptions = new CacheOptions();
        builder.Configuration.GetSection(CacheOptions.SectionName).Bind(cacheOptions);
        options.SizeLimit = cacheOptions.MaxCacheSize;
    });

    // Register application services
    builder.Services.AddScoped<ICountryService, Maliev.CountryService.Api.Services.CountryService>();

    // Configure Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        var rateLimitOptions = new RateLimitOptions();
        builder.Configuration.GetSection(RateLimitOptions.SectionName).Bind(rateLimitOptions);

        // Country endpoint rate limiting
        options.AddPolicy("CountryPolicy", context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.CountryEndpoint.PermitLimit,
                    Window = rateLimitOptions.CountryEndpoint.Window,
                    SegmentsPerWindow = 2,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitOptions.CountryEndpoint.QueueLimit
                }));

        // Global rate limiting
        options.AddPolicy("GlobalPolicy", context =>
            RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitOptions.Global.PermitLimit,
                    Window = rateLimitOptions.Global.Window,
                    SegmentsPerWindow = 4,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitOptions.Global.QueueLimit
                }));

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429;
            await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
        };
    });

    // Configure CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(
            policy =>
            {
                policy.WithOrigins(
                    "https://www.maliev.com",
                    "https://intranet.maliev.com",
                    "https://api.maliev.com")
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
    });

    // JWT Bearer authentication configuration (skip in Testing environment)
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        // Check if JWT configuration is available from Google Secret Manager
        var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName);
        var hasJwtConfig = !string.IsNullOrEmpty(jwtConfig["Issuer"]) && 
                          !string.IsNullOrEmpty(jwtConfig["Audience"]) && 
                          !string.IsNullOrEmpty(jwtConfig["SecurityKey"]);

        if (hasJwtConfig)
        {
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options =>
            {
                var jwtOptions = new JwtOptions
                {
                    Issuer = "default-issuer",
                    Audience = "default-audience", 
                    SecurityKey = "default-key"
                };
                jwtConfig.Bind(jwtOptions);

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecurityKey))
                };
            });
        }
        else
        {
            // Log warning that JWT is not configured for local development
            Log.Warning("JWT configuration not found - API will start but authentication will not work. Configure JWT secrets for full functionality.");
        }
    }

    builder.Services.AddAuthorization();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CountryDbContext>("CountryDbContext", tags: new[] { "readiness" })
        .AddCheck<DatabaseHealthCheck>("Database Health Check", tags: new[] { "readiness" });

    var app = builder.Build();

    app.UseForwardedHeaders();

    // Add correlation ID middleware early in pipeline
    app.UseCorrelationId();

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
        foreach (var description in provider.ApiVersionDescriptions)
        {
            c.SwaggerEndpoint($"./{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
        }
        c.RoutePrefix = "countries/swagger";
    });

    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseHttpsRedirection();

    app.UseRateLimiter();
    app.UseCors();
    
    // JWT Authentication & Authorization (only if configured and not in Testing environment)
    if (!app.Environment.IsEnvironment("Testing"))
    {
        var jwtMiddlewareConfig = app.Configuration.GetSection(JwtOptions.SectionName);
        var hasJwtConfig = !string.IsNullOrEmpty(jwtMiddlewareConfig["Issuer"]) && 
                          !string.IsNullOrEmpty(jwtMiddlewareConfig["Audience"]) && 
                          !string.IsNullOrEmpty(jwtMiddlewareConfig["SecurityKey"]);

        if (hasJwtConfig)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }
    }

    // Health check endpoints (allow anonymous access for monitoring)
    app.MapGet("/countries/liveness", () => "Healthy").AllowAnonymous();

    app.MapHealthChecks("/countries/readiness", new HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program
{ }