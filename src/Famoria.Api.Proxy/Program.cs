var builder = WebApplication.CreateBuilder(args);

builder.Services.AddReverseProxy()
    .LoadFromMemory(new[]
    {
        new Yarp.ReverseProxy.Configuration.RouteConfig
        {
            RouteId = "api",
            Match = new() { Path = "/{**catch-all}" },
            ClusterId = "apiCluster"
        }
    },
    new[]
    {
        new Yarp.ReverseProxy.Configuration.ClusterConfig
        {
            ClusterId = "apiCluster",
            Destinations = new Dictionary<string, Yarp.ReverseProxy.Configuration.DestinationConfig>
            {
                { "api", new() { Address = "https://localhost:7001/" } }
            }
        }
    });

var app = builder.Build();

app.MapReverseProxy();

app.Run();
