using MissionManagement.Application.Missions.Commands.ActivateMission;
using MissionManagement.Application.Missions.Commands.CreateMission;
using MissionManagement.Application.Missions.Commands.DeactivateMission;
using MissionManagement.Application.Missions.Commands.UpdateMissionDetails;
using MissionManagement.Application.Missions.Queries.GetMissionById;
using MissionManagement.Application.Missions.Queries.GetMissionNodeValidations;
using MissionManagement.WebApi.Contracts.Missions;
using MissionManagement.WebApi.Contracts.Routes;

namespace MissionManagement.WebApi.Mapping;

public static class MissionMappings
{
    public static CreateMissionCommand ToCommand(this CreateMissionRequest body) =>
        new(
            Title: body.Title,
            Description: body.Description,
            Difficulty: body.Difficulty,
            MaxDurationMinutes: body.MaxDurationMinutes);

    public static GetMissionByIdQuery ToQuery(this MissionRoute route) =>
        new(route.Id);

    public static GetMissionNodeValidationsQuery ToNodeValidationsQuery(this MissionRoute route) =>
        new(route.Id);

    public static UpdateMissionDetailsCommand ToCommand(
        this UpdateMissionDetailsRequest body,
        MissionRoute route) =>
        new(
            Id: route.Id,
            Title: body.Title,
            Description: body.Description,
            MaxDurationMinutes: body.MaxDurationMinutes);

    public static ActivateMissionCommand ToActivateCommand(this MissionRoute route) =>
        new(route.Id);

    public static DeactivateMissionCommand ToDeactivateCommand(this MissionRoute route) =>
        new(route.Id);
}
