namespace Maliev.CountryService.Api.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // HSTS - HTTP Strict Transport Security
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        
        // CSP - Content Security Policy
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';";
        
        // X-Frame-Options - Clickjacking protection
        context.Response.Headers["X-Frame-Options"] = "DENY";
        
        // X-Content-Type-Options - MIME type sniffing protection
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        
        // X-XSS-Protection - XSS protection (legacy, but still useful)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
        
        // Referrer-Policy - Control referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        
        // Permissions-Policy - Control browser features
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

        await _next(context);
    }
}
