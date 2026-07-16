using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScoringAudit.Application.Queries;
using ScoringAudit.WebApi.Auth;

namespace ScoringAudit.WebApi.Controllers;

[ApiController]
[Route("api/v1/audit/sessions")]
[Authorize(Roles = "admin,operator")]
public sealed class AuditController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUser _currentUser;

    public AuditController(IMediator mediator, ICurrentUser currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<HistoricalSessionsPageDto>> GetHistoricalSessions(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetHistoricalSessionsQuery(
                startDate,
                endDate,
                page,
                pageSize,
                _currentUser.IsInRole("admin"),
                _currentUser.UserId),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<SessionAuditDetailDto>> GetSessionAuditDetail(
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetSessionAuditDetailQuery(sessionId, _currentUser.IsInRole("admin"), _currentUser.UserId),
            cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }
}
