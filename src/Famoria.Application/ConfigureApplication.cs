using Microsoft.Extensions.Hosting;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        return builder;
    }
}
