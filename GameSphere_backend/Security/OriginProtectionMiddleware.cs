namespace GameSphere_backend.Security;

public sealed class OriginProtectionMiddleware
{
    private static readonly HashSet<string> UnsafeMethods = ["POST", "PUT", "PATCH", "DELETE"];
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _allowedOrigins;

    public OriginProtectionMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _allowedOrigins = configuration.GetSection("Cors:AllowedOrigins")
            .GetChildren()
            .Select(section => section.Value?.TrimEnd('/'))
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Cast<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (_allowedOrigins.Count == 0)
        {
            _allowedOrigins.UnionWith(["http://localhost:5173", "http://127.0.0.1:5173"]);
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault()?.TrimEnd('/');
        if (context.Request.Path.StartsWithSegments("/api") &&
            UnsafeMethods.Contains(context.Request.Method) &&
            !string.IsNullOrWhiteSpace(origin) &&
            !_allowedOrigins.Contains(origin))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return;
        }

        await _next(context);
    }
}
