using Microsoft.AspNetCore.Mvc;

namespace Rmms.Api.Controllers;

/// <summary>Trivial endpoint to verify the scaffold builds and serves requests.</summary>
[ApiController]
[Route("api/v1/[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        data = new
        {
            status = "ok",
            service = "rmms-api",
            timeUtc = DateTimeOffset.UtcNow,
        },
    });
}
