using AdminService.Domain.Common;

namespace AdminService.Domain.Entities;

public class Hint : Entity
{
    public Guid NodeId { get; private set; }
    public string Content { get; private set; }

    internal Hint(Guid id, Guid nodeId, string content)
    {
        Id = id;
        NodeId = nodeId;
        Content = content;
    }
}