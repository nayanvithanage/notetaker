using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Notetaker.Api.Data;

namespace Notetaker.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly NotetakerDbContext _context;

    public HealthController(NotetakerDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test database connectivity
            await _context.Database.CanConnectAsync();
            
            return Ok(new { 
                status = "healthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = "connected"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(503, new { 
                status = "unhealthy", 
                timestamp = DateTime.UtcNow,
                version = "1.0.0",
                database = "disconnected",
                error = ex.Message
            });
        }
    }
}
