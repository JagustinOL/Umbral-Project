using FluentAssertions;
using SessionManagement.Domain.Entities;
using Xunit;

namespace SessionManagement.Domain.Tests.Common;

public sealed class EntityEqualityTests
{
    [Fact]
    public void Equals_WhenSameIdAndType_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = TeamMember.Create(Guid.NewGuid(), "A", TeamMemberRole.Member);
        var b = TeamMember.Create(Guid.NewGuid(), "B", TeamMemberRole.Member);
        typeof(SessionManagement.Domain.Common.Entity).GetProperty(nameof(TeamMember.Id))!
            .SetValue(a, id);
        typeof(SessionManagement.Domain.Common.Entity).GetProperty(nameof(TeamMember.Id))!
            .SetValue(b, id);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
    }

    [Fact]
    public void OperatorEquals_WhenOneNull_ReturnsFalse()
    {
        TeamMember? left = null;
        var right = TeamMember.Create(Guid.NewGuid(), "R");
        (left == right).Should().BeFalse();
        (right == left).Should().BeFalse();
    }
}
