using Microsoft.AspNetCore.Mvc;

namespace LawCorp.Mcp.ExternalApi.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy", service = "LawCorp DMS External API" });
}
