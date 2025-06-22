using Famoria.Domain.Enums;

namespace Famoria.Api.Controllers;

public class FamilyController : CustomControllerBase
{
    private readonly IFamilyService _familyService;
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;

    public FamilyController(IMediator mediator, IFamilyService creator, IUserService userService, IJwtService jwtService) : base(mediator)
    {
        _familyService = creator;
        _userService = userService;
        _jwtService = jwtService;
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] FamilyDto request, CancellationToken cancellationToken)
    {
        var existingFamilyId = User.FamilyId()!;

        if (!string.IsNullOrEmpty(existingFamilyId))
            return BadRequest("User already has a family.");

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("DisplayName required.");

        var famoriaUserId = User.FamoriaUserId()!;

        request.Members.Add(new(User.Name()!, FamilyMemberRole.Parent, default, default, default));

        var familyId = await _familyService.CreateAsync(famoriaUserId, request, cancellationToken);

        var userDto = await _userService.AddFamilyToUserAsync(famoriaUserId, familyId, cancellationToken);

        var newToken = await _jwtService.SignAsync(userDto);

        Response.AppendAccessToken(newToken);

        return Ok(familyId);
    }
}
