using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SessionManagement.Application.Hints;
using SessionManagement.WebApi.Contracts.Routes;

namespace SessionManagement.WebApi.Controllers;

[ApiController]
[Route("api/v1/live-sessions/{sessionId:guid}/teams/{teamId:guid}/nodes/{nodeId:guid}/hints")]
[Authorize(Roles = "player,operator,admin")]
public sealed class PlayerHintsController : ControllerBase
{
    private readonly IPlayerHintPanelService _hintPanelService;

    public PlayerHintsController(IPlayerHintPanelService hintPanelService)
    {
        _hintPanelService = hintPanelService;
    }

    [HttpGet]
    public async Task<IActionResult> GetReleasedHints(
        [FromRoute] LiveSessionTeamNodeRoute route,
        CancellationToken cancellationToken)
    {
        var hints = await _hintPanelService.GetReleasedHintsForTeamAsync(
            route.SessionId, route.TeamId, route.NodeId, cancellationToken);
        return Ok(hints);
    }
}
