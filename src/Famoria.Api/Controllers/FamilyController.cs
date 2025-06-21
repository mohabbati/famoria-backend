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
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest("DisplayName required.");

        var famoriaUserId = User.FamoriaUserId()!;

        var familyId = await _familyService.CreateAsync(famoriaUserId, request, cancellationToken);

        await _userService.AddFamilyToUserAsync(famoriaUserId, familyId, cancellationToken);

        var newToken = _jwtService.Sign(famoriaUserId, User.Email()!, familyId);

        Response.AppendAccessToken(newToken);

        return Ok(familyId);
    }
}
