namespace MissionManagement.Application.Abstractions;

public interface IClock
{
    DateTime UtcNow { get; }
}

