using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Hints;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/sessions/{sessionId:guid}/teams/{teamId:guid}/hints")]
[Authorize(Roles = "player")]
public sealed class TeamReleasedHintsController : ControllerBase
{
    private readonly IPlayerHintPanelService _hintPanelService;

    public TeamReleasedHintsController(IPlayerHintPanelService hintPanelService)
    {
        _hintPanelService = hintPanelService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReleasedHints(
        [FromRoute] Guid sessionId,
        [FromRoute] Guid teamId,
        CancellationToken cancellationToken)
    {
        var hints = await _hintPanelService.GetAllReleasedHintsForTeamAsync(
            sessionId, teamId, cancellationToken);
        return Ok(hints);
    }
}
