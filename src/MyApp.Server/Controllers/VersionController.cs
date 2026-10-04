using Microsoft.AspNetCore.Mvc;

namespace MyApp.Server.Controllers;

[ApiController]
[Route("[controller]")]
public sealed class VersionController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0";
        return Ok(new { version });
    }
}
