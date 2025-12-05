using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Maliev.CountryService.Data;
using Maliev.CountryService.Tests.Fixtures;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Maliev.CountryService.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly TestDatabaseFixture _db = new();
    
    public RSA TestRsaKey { get; } = RSA.Create(2048);
    public string TestIssuer { get; } = "https://test.maliev.com";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:redis", _db.RedisConnectionString);
        // Clear Jwt:PublicKey so AddJwtAuthentication uses Testing environment path
        builder.UseSetting("Jwt:PublicKey", "");
        
        builder.ConfigureTestServices(services =>
        {
            // Remove the app's DbContextOptions registration
            services.RemoveAll(typeof(DbContextOptions<CountryServiceDbContext>));
            // Add DbContextOptions pointing to the Testcontainers PostgreSQL
            services.AddDbContext<CountryServiceDbContext>(options =>
                options.UseNpgsql(_db.ConnectionString));
            
            // Remove the app's IDistributedCache registration
            services.RemoveAll(typeof(IDistributedCache));
            // Add a Redis Distributed Cache using the Testcontainers Redis
            services.AddSingleton<IDistributedCache>(sp => new RedisCache(Options.Create(new RedisCacheOptions
            {
                Configuration = _db.RedisConnectionString
            })));

            // Override JWT Bearer options with test RSA key via PostConfigureAll
            services.PostConfigureAll<JwtBearerOptions>(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = TestIssuer,
                    ValidateAudience = true,
                    ValidAudience = TestIssuer,
                    ValidateLifetime = true,
                    IssuerSigningKey = new RsaSecurityKey(TestRsaKey),
                    ValidateIssuerSigningKey = true
                };
            });
        });
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync() => await _db.InitializeAsync();
    async Task IAsyncLifetime.DisposeAsync()
    {
        await _db.DisposeAsync();
        TestRsaKey.Dispose();
    }

    public string GenerateTestToken(string userId, string role = "CountryAdmin")
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Role, role)
        };
        
        var token = new JwtSecurityToken(
            issuer: TestIssuer,
            audience: TestIssuer,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(
                new RsaSecurityKey(TestRsaKey), 
                SecurityAlgorithms.RsaSha256)
        );
        
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}