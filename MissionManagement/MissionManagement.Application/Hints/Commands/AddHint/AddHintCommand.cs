using MediatR;
using Microsoft.AspNetCore.Http;

namespace MissionManagement.Application.Hints.Commands.AddHint;

public sealed record AddHintCommand(
    Guid MissionId,
    Guid NodeId,
    string Content,
    IFormFile Attachment
) : IRequest<Guid>;

