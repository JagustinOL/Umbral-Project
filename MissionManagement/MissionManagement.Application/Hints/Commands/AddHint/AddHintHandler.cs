using MediatR;
using Microsoft.AspNetCore.Http;
using MissionManagement.Application.Exceptions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.Repositories;

namespace MissionManagement.Application.Hints.Commands.AddHint;

public sealed class AddHintHandler : IRequestHandler<AddHintCommand, Guid>
{
    private const long MaxAttachmentBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png"
    };

    private readonly IMissionRepository _repository;

    public AddHintHandler(IMissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(AddHintCommand request, CancellationToken cancellationToken)
    {
        var mission = await _repository.GetByIdForUpdateAsync(request.MissionId, cancellationToken);
        if (mission is null)
            throw new NotFoundException($"No se encontró la misión con Id={request.MissionId}.");

        ValidateAttachment(request.Attachment);

        var node = mission.FindNodeById(request.NodeId);
        if (node is null)
            throw new NotFoundException($"No se encontró el nodo con Id={request.NodeId}.");

        var nextOrder = node.Hints.Count + 1;
        var hint = Hint.Create(
            missionNodeId: request.NodeId,
            order: nextOrder,
            content: request.Content,
            penaltyPoints: 0);

        mission.AddHintToNode(request.NodeId, hint);
        await _repository.SaveAsync(mission, cancellationToken);

        return hint.Id;
    }

    private static void ValidateAttachment(IFormFile? attachment)
    {
        if (attachment is null)
            return;

        if (attachment.Length <= 0)
            throw new ConflictException("El archivo adjunto está vacío.");

        if (attachment.Length > MaxAttachmentBytes)
            throw new ConflictException($"El archivo adjunto supera el tamaño máximo permitido ({MaxAttachmentBytes} bytes).");

        if (string.IsNullOrWhiteSpace(attachment.ContentType) ||
            !AllowedContentTypes.Contains(attachment.ContentType))
        {
            throw new ConflictException("Formato de archivo no válido. Solo se permiten JPG o PNG.");
        }
    }
}

