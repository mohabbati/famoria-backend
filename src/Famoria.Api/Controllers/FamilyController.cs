using Microsoft.AspNetCore.Mvc;

namespace Famoria.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FamilyController : ControllerBase
{
    [HttpPost("init")]
    public IActionResult Init()
    {
        // Placeholder for family initialization logic
        return Ok();
    }
}

