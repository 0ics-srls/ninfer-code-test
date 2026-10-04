using Microsoft.AspNetCore.Mvc;

namespace MyApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
}
