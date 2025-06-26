using Famoria.Application.Models;

namespace Famoria.Api.Extensions;

public static class WebApplicationExtensions
{
    public static IHostApplicationBuilder AddApiServices(this IHostApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<JwtSettings>()
            .Bind(builder.Configuration.GetSection("Auth:Jwt"));

        builder.Services.AddTransient<IJwtService, JwtService>();
        builder.Services.AddTransient<ISignInService, SignInService>();
        
        return builder;
    }

    public static void UseCustomSwagger(this WebApplication app, IConfiguration configuration)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Famoria API V1");
            options.RoutePrefix = string.Empty;

            options.OAuthClientId(configuration["Auth:Google:ClientId"]);
            options.OAuthUsePkce();

            // Set default scopes that should be selected in the Swagger UI
            options.OAuthScopes(new[] { "openid", "email", "profile" });

            // Add custom script to handle cookie-based authentication
            options.HeadContent = @"
            <script>
                window.addEventListener('message', function(e) {
                    if (e.data && e.data.success) {
                        // After OAuth2 callback success, refresh the page to get the cookies
                        setTimeout(function() {
                            window.location.reload();
                        }, 500);
                    }
                });
            </script>";
        });
    }
}
