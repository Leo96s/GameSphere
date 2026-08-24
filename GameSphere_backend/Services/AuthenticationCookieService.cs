using GameSphere_backend.ServicesResponses;

namespace GameSphere_backend.Services;

public sealed class AuthenticationCookieService
{
    private const string AccessCookieName = "gamesphere_access_token";
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _environment;

    public AuthenticationCookieService(IConfiguration configuration, IWebHostEnvironment environment)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public void AppendAccessCookie(HttpResponse response, LoginResponse loginResponse)
    {
        var expirationMinutes = _configuration.GetValue("JwtSettings:ExpirationMinutes", 60);
        response.Cookies.Append(AccessCookieName, loginResponse.token, new CookieOptions
        {
            HttpOnly = true,
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
        });
    }

    public void DeleteAccessCookie(HttpResponse response)
    {
        response.Cookies.Delete(AccessCookieName, new CookieOptions
        {
            Path = "/",
            Secure = !_environment.IsDevelopment(),
            SameSite = SameSiteMode.Lax,
            HttpOnly = true
        });
    }
}
