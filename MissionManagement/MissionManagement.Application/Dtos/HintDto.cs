namespace MissionManagement.Application.Dtos;

public sealed record HintDto(
    Guid Id,
    int Order,
    string Content,
    int PenaltyPoints
);

