using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;

public class App : ComponentBase
{
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        builder.OpenComponent<Router>(0);
        builder.AddAttribute(1, nameof(Router.AppAssembly), typeof(Program).Assembly);
        builder.AddAttribute(2, nameof(Router.Found), (RenderFragment<RouteData>)(routeData => builder2 =>
        {
            builder2.OpenComponent<RouteView>(0);
            builder2.AddAttribute(1, nameof(RouteView.RouteData), routeData);
            builder2.CloseComponent();
        }));
        builder.AddAttribute(3, nameof(Router.NotFound), (RenderFragment)(builder2 =>
        {
            builder2.AddContent(0, "Sorry, there's nothing at this address.");
        }));
        builder.CloseComponent();
    }
}
