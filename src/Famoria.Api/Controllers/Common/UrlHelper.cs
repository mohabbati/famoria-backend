using Microsoft.Extensions.Configuration;

namespace Famoria.Api.Controllers;

public static class UrlHelper
{
    public static string GetReturnUrl(IConfiguration config, string? returnUrl)
    {
        var fallback = config["Auth:FrontendUrl"] ?? string.Empty;
        if (string.IsNullOrEmpty(returnUrl))
            return fallback;

        if (Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            var allowed = new Uri(fallback).Host;
            if (uri.Host.Equals(allowed, StringComparison.OrdinalIgnoreCase))
                return returnUrl;
        }

        return fallback;
    }

    public static string GetOrigin(IConfiguration config, string? returnUrl)
    {
        var url = GetReturnUrl(config, returnUrl);
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return $"{uri.Scheme}://{uri.Authority}";
        return url;
    }
}
