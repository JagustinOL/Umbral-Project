using FluentAssertions;
using MissionManagement.Domain.Entities;
using MissionManagement.Domain.ValueObjects;

namespace MissionManagement.Domain.Tests.Common;

public sealed class EntityEqualityTests
{
    [Fact]
    public void Equals_WhenSameIdAndType_ReturnsTrue()
    {
        var id = Guid.NewGuid();
        var a = Hint.Create(Guid.NewGuid(), 1, "A", 5);
        var b = Hint.Create(Guid.NewGuid(), 1, "B", 5);
        typeof(MissionManagement.Domain.Common.Entity).GetProperty(nameof(Hint.Id))!
            .SetValue(a, id);
        typeof(MissionManagement.Domain.Common.Entity).GetProperty(nameof(Hint.Id))!
            .SetValue(b, id);

        (a == b).Should().BeTrue();
        a.Equals(b).Should().BeTrue();
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equals_WhenDifferentType_ReturnsFalse()
    {
        var node = MissionNode.Create("T", "D", MissionNodeType.Stage, 1, 10);
        var hint = Hint.Create(node.Id, 1, "H", 1);
        node.Equals(hint).Should().BeFalse();
        (node == hint).Should().BeFalse();
    }

    [Fact]
    public void OperatorEquals_WhenNull_HandlesCorrectly()
    {
        Hint? left = null;
        Hint? right = null;
        (left == right).Should().BeTrue();
        (left != right).Should().BeFalse();
        (left == Hint.Create(Guid.NewGuid(), 1, "H", 1)).Should().BeFalse();
    }

    [Fact]
    public void Equals_WhenSameReference_ReturnsTrue()
    {
        var hint = Hint.Create(Guid.NewGuid(), 1, "H", 1);
        object boxed = hint;
        hint.Equals(boxed).Should().BeTrue();
        ReferenceEquals(hint, hint).Should().BeTrue();
    }

    [Fact]
    public void Equals_WhenNullObject_ReturnsFalse()
    {
        var hint = Hint.Create(Guid.NewGuid(), 1, "H", 1);
        hint.Equals(null).Should().BeFalse();
        (hint != null).Should().BeTrue();
    }
}
