using MediatR;
using SessionManagement.Application.Dtos;

namespace SessionManagement.Application.Teams.Queries.GetTeamById;

public sealed record GetTeamByIdQuery(Guid TeamId) : IRequest<TeamDetailsDto>;
