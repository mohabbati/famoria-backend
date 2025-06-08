namespace Famoria.Api.Controllers;

[ApiController]
[Route("[controller]")]
public abstract class CustomControllerBase(IMediator mediator) : ControllerBase
{
    protected IMediator Mediator { get; } = mediator;
}
