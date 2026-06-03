using MediatR;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Application.Nodes.Commands.AddTriviaNode;

public sealed record AddTriviaNodeCommand(
    Guid MissionId,
    Guid ParentNodeId,
    List<TriviaQuestion> Questions,
    int ExecutionOrder
) : IRequest<Guid>;

