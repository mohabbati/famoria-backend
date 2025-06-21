namespace Famoria.Api.Extensions;

public static class HttpResponseExtensions
{
    private const string CookieName = "ACCESS_TOKEN";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(1);

    public static void AppendAccessToken(
        this HttpResponse response,
        string token,
        TimeSpan? expires = null)
    {
        response.Cookies.Append(
            CookieName,
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.Add(expires ?? DefaultExpiry)
            });
    }
}
