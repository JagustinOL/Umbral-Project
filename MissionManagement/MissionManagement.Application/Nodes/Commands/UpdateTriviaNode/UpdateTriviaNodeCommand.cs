using MediatR;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Nodes.Commands.UpdateTriviaNode;

public sealed record UpdateTriviaNodeCommand(
    Guid MissionId,
    Guid NodeId,
    List<TriviaQuestion> Questions
) : IRequest;

