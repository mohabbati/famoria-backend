using Famoria.Application.Services;
using System.Security.Claims;

namespace Famoria.Api.Controllers;

public class FamilyController : CustomControllerBase
{
    private readonly FamilyService _creator;
    private readonly JwtService _jwt;

    public FamilyController(IMediator mediator, FamilyService creator, JwtService jwt) : base(mediator)
    {
        _creator = creator;
        _jwt = jwt;
    }

    public record ChildInput(string Name, List<string>? Tags);
    public record CreateFamilyRequest(string DisplayName, List<ChildInput>? Children);

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFamilyRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("DisplayName required");

        var children = request.Children?.Select(c => (c.Name, (IEnumerable<string>?)c.Tags));
        var familyId = await _creator.CreateAsync(User, request.DisplayName, children, ct);

        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value!;
        var email = User.FindFirst(ClaimTypes.Email)?.Value!;
        var token = _jwt.Sign(sub, email, familyId);
        return Ok(new { token, familyId });
    }
}
