using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.CountryService.Api.Authorization;
using Maliev.CountryService.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Maliev.CountryService.Tests.Unit;

/// <summary>
/// Tests CountryService's outbound workload authentication boundary.
/// </summary>
public sealed class ServiceAuthenticationWiringTests
{
    private const string ExpectedToken = "centrally-issued-country-token";

    /// <summary>
    /// CountryService startup should opt into AuthService exchange and the central IAM client only.
    /// </summary>
    [Fact]
    public void Program_RegistersCountryExchangeWithoutLegacySigner()
    {
        var source = ReadRepositoryFile("Maliev.CountryService.Api", "Program.cs");

        Assert.Contains("builder.AddAuthServiceTokenExchange(\"CountryService\");", source, StringComparison.Ordinal);
        Assert.Contains("builder.AddAuthServiceIAMClient();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddIAMServiceClient", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// The process identity should be exact and no local-signing services should resolve.
    /// </summary>
    [Fact]
    public void AuthServiceIamClient_RegistersExactIdentityWithoutLegacySigningServices()
    {
        var builder = CreateConfiguredBuilder();

        builder.AddAuthServiceTokenExchange("CountryService");
        builder.AddAuthServiceIAMClient();

        using var provider = builder.Services.BuildServiceProvider();
        var identity = provider.GetRequiredService<ServiceProcessIdentity>();

        Assert.Equal("CountryService", identity.ServiceName);
        Assert.Single(provider.GetServices<IIamServiceClient>());
        Assert.Null(provider.GetService<IServiceAccountTokenProvider>());
        Assert.Null(provider.GetService<ServiceAccountAuthenticationHandler>());
    }

    /// <summary>
    /// IAM permission checks should use the bearer token supplied by AuthService exchange.
    /// </summary>
    [Fact]
    public async Task IamPermissionCheck_UsesAuthServiceExchangedBearerTokenOnExactRoute()
    {
        var builder = CreateConfiguredBuilder();
        var capture = new AuthorizationCaptureHandler();
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
            new CapturingPrimaryHandlerFilter(capture));

        builder.AddAuthServiceTokenExchange("CountryService");
        builder.Services.AddSingleton<IAuthServiceTokenProvider>(new StubTokenProvider());
        builder.AddAuthServiceIAMClient();

        await using var provider = builder.Services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var iamClient = scope.ServiceProvider.GetRequiredService<IIamServiceClient>();

        var allowed = await iamClient.CheckPermissionAsync(
            $"country-test-{Guid.NewGuid():N}",
            CountryPermissions.CountriesRead,
            cancellationToken: CancellationToken.None);

        Assert.True(allowed);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", ExpectedToken), capture.Authorization);
        Assert.Equal(HttpMethod.Post, capture.Method);
        Assert.Equal(new Uri("https://iam.test/iam/v1/auth/check-permission"), capture.RequestUri);
    }

    /// <summary>
    /// Missing or malformed workload credentials should fail options validation.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("service-country-service", "short")]
    public async Task AuthServiceExchange_InvalidCredentials_FailsClosedAtHostStartup(
        string? clientId,
        string? clientSecret)
    {
        var builder = CreateConfiguredBuilder(clientId, clientSecret);
        builder.AddAuthServiceTokenExchange("CountryService");

        using var host = builder.Build();

        await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());
    }

    /// <summary>
    /// CI should consume the exact published ServiceDefaults version containing central exchange support.
    /// </summary>
    [Fact]
    public void ServiceDefaultsDependency_PinsPublishedCentralExchangeVersion()
    {
        var source = ReadRepositoryFile("Directory.Build.props");

        Assert.Contains(
            "<ServiceDefaultsVersion Condition=\"'$(ServiceDefaultsVersion)' == ''\">1.0.89-alpha</ServiceDefaultsVersion>",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "<ServiceDefaultsVersion Condition=\"'$(ServiceDefaultsVersion)' == ''\">1.0.*",
            source,
            StringComparison.Ordinal);

        foreach (var project in new[]
                 {
                     "Maliev.CountryService.Api/Maliev.CountryService.Api.csproj",
                     "Maliev.CountryService.Application/Maliev.CountryService.Application.csproj",
                     "Maliev.CountryService.Infrastructure/Maliev.CountryService.Infrastructure.csproj",
                     "Maliev.CountryService.Tests/Maliev.CountryService.Tests.csproj"
                 })
        {
            var projectSource = ReadRepositoryFile(project.Split('/'));
            Assert.Contains(
                "<PackageReference Include=\"Maliev.Aspire.ServiceDefaults\" Version=\"$(ServiceDefaultsVersion)\" />",
                projectSource,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The Docker restore layer must include the shared version property before restoring package-mode projects.
    /// </summary>
    [Fact]
    public void Dockerfile_CopiesSharedVersionPropertiesBeforePackageRestore()
    {
        var source = ReadRepositoryFile("Maliev.CountryService.Api", "Dockerfile");
        var propertiesCopy = source.IndexOf(
            "COPY [\"Directory.Build.props\", \".\"]",
            StringComparison.Ordinal);
        var restore = source.IndexOf(
            "dotnet restore \"./Maliev.CountryService.Api/Maliev.CountryService.Api.csproj\"",
            StringComparison.Ordinal);

        Assert.True(propertiesCopy >= 0, "Dockerfile must copy Directory.Build.props into the restore layer.");
        Assert.True(restore > propertiesCopy, "Directory.Build.props must be available before dotnet restore.");
    }

    /// <summary>
    /// Country routes and permission policies are part of the service contract and must remain unchanged.
    /// </summary>
    [Fact]
    public void CountryEndpoints_RetainVersionedRoutesAndPermissionPolicies()
    {
        AssertControllerRoute<CountriesController>("country/v{version:apiVersion}/countries");
        AssertEndpoint<CountriesController>(nameof(CountriesController.GetById), "{id:guid}", "GET", CountryPermissions.CountriesRead);
        AssertEndpoint<CountriesController>(nameof(CountriesController.GetByIso2), "iso2/{iso2}", "GET", CountryPermissions.CountriesRead);
        AssertEndpoint<CountriesController>(nameof(CountriesController.GetByIso3), "iso3/{iso3}", "GET", CountryPermissions.CountriesRead);
        AssertEndpoint<CountriesController>(nameof(CountriesController.List), null, "GET", CountryPermissions.CountriesList);
        AssertEndpoint<CountriesController>(nameof(CountriesController.Search), "search", "GET", CountryPermissions.CountriesSearch);

        AssertControllerRoute<AdminCountriesController>("country/v{version:apiVersion}/admin/countries");
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.Create), null, "POST", CountryPermissions.CountriesCreate);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.Update), "{id:guid}", "PUT", CountryPermissions.CountriesUpdate);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.Patch), "{id:guid}", "PATCH", CountryPermissions.CountriesUpdate);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.SoftDelete), "{id:guid}", "DELETE", CountryPermissions.CountriesDelete);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.HardDelete), "{id:guid}/hard-delete", "DELETE", CountryPermissions.CountriesHardDelete);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.Restore), "{id:guid}/restore", "POST", CountryPermissions.CountriesRestore);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.RebuildCache), "rebuild-cache", "POST", CountryPermissions.SystemRebuildCache);
        AssertEndpoint<AdminCountriesController>(nameof(AdminCountriesController.ExportAll), "export", "GET", CountryPermissions.SystemExport);

        AssertControllerRoute<BulkImportController>("country/v{version:apiVersion}/admin/bulk-import");
        AssertEndpoint<BulkImportController>(nameof(BulkImportController.SubmitBulkImport), null, "POST", CountryPermissions.ImportUpload);
        AssertEndpoint<BulkImportController>(nameof(BulkImportController.GetJobStatus), "{jobId:guid}", "GET", CountryPermissions.ImportStatus);
        AssertEndpoint<BulkImportController>(nameof(BulkImportController.ProcessJob), "{jobId:guid}/process", "POST", CountryPermissions.ImportExecute);
    }

    /// <summary>
    /// Opting the IAM client into exchange must not attach bearer authentication to unrelated clients.
    /// </summary>
    [Fact]
    public async Task AuthServiceExchange_DoesNotContaminateUnrelatedHttpClients()
    {
        var builder = CreateConfiguredBuilder();
        var capture = new AuthorizationCaptureHandler();
        var tokenProvider = new CountingTokenProvider();
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
            new CapturingPrimaryHandlerFilter(capture));
        builder.Services.AddHttpClient("CountryUnrelated", client =>
            client.BaseAddress = new Uri("https://unrelated.test"));

        builder.AddAuthServiceTokenExchange("CountryService");
        builder.Services.AddSingleton<IAuthServiceTokenProvider>(tokenProvider);
        builder.AddAuthServiceIAMClient();

        await using var provider = builder.Services.BuildServiceProvider();
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("CountryUnrelated");
        using var response = await client.GetAsync("/probe", CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null(capture.Authorization);
        Assert.Equal(0, tokenProvider.CallCount);
        Assert.Equal(new Uri("https://unrelated.test/probe"), capture.RequestUri);
    }

    private static HostApplicationBuilder CreateConfiguredBuilder(
        string? clientId = "service-country-service",
        string? clientSecret = "country-test-secret-with-at-least-32-bytes")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Testing"
        });

        using var rsa = RSA.Create(2048);
        builder.Configuration["ServiceAuthentication:ClientId"] = clientId;
        builder.Configuration["ServiceAuthentication:ClientSecret"] = clientSecret;
        builder.Configuration["Services:AuthService:BaseUrl"] = "https://auth.test";
        builder.Configuration["Services:IAMService:BaseUrl"] = "https://iam.test";
        builder.Configuration["Jwt:PublicKey"] = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(rsa.ExportSubjectPublicKeyInfoPem()));
        builder.Configuration["Jwt:Issuer"] = "https://api.maliev.com";
        builder.Configuration["Jwt:Audience"] = "https://api.maliev.com";

        return builder;
    }

    private static void AssertControllerRoute<TController>(string expectedTemplate)
    {
        var controller = typeof(TController);
        Assert.NotNull(controller.GetCustomAttribute<ApiVersionAttribute>());
        Assert.Equal(expectedTemplate, controller.GetCustomAttribute<RouteAttribute>()?.Template);
    }

    private static void AssertEndpoint<TController>(
        string methodName,
        string? expectedTemplate,
        string expectedVerb,
        string expectedPermission)
    {
        var method = typeof(TController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var route = method.GetCustomAttributes<HttpMethodAttribute>().Single();
        Assert.Equal(expectedTemplate, route.Template);
        Assert.Contains(expectedVerb, route.HttpMethods);
        Assert.Equal(expectedPermission, method.GetCustomAttribute<RequirePermissionAttribute>()?.Permission);
    }

    private static string ReadRepositoryFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            Path.Combine(segments)));

        Assert.True(File.Exists(path), $"Could not find source file: {path}");
        return File.ReadAllText(path);
    }

    private sealed class StubTokenProvider : IAuthServiceTokenProvider
    {
        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ExpectedToken);
    }

    private sealed class CountingTokenProvider : IAuthServiceTokenProvider
    {
        public int CallCount { get; private set; }

        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(ExpectedToken);
        }
    }

    private sealed class AuthorizationCaptureHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            Method = request.Method;
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"allowed\":true}", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingPrimaryHandlerFilter(HttpMessageHandler primaryHandler)
        : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
        {
            next(builder);
            for (var index = builder.AdditionalHandlers.Count - 1; index >= 0; index--)
            {
                if (builder.AdditionalHandlers[index].GetType().FullName?.Contains(
                        "ServiceDiscovery",
                        StringComparison.Ordinal) == true ||
                    builder.AdditionalHandlers[index].GetType().FullName?.Contains(
                        "ResolvingHttpDelegatingHandler",
                        StringComparison.Ordinal) == true)
                {
                    builder.AdditionalHandlers.RemoveAt(index);
                }
            }

            builder.PrimaryHandler = primaryHandler;
        };
    }
}
