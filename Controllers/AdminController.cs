using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SpecificationPatternDemo.Services;

namespace SpecificationPatternDemo.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")] // restrict to admin users
public class AdminController : ControllerBase
{
    private readonly RefreshTokenCleanupService _cleanupService;

    public AdminController(RefreshTokenCleanupService cleanupService)
    {
        _cleanupService = cleanupService;
    }

    [HttpPost("cleanup-refresh-tokens")]
    public async Task<IActionResult> RunCleanup(CancellationToken cancellationToken)
    {
        var removed = await _cleanupService.RunCleanupOnceAsync(cancellationToken).ConfigureAwait(false);
        return Ok(new { removed });
    }
}
