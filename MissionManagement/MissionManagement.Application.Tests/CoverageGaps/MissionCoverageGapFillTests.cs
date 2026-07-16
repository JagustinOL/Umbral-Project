using FluentAssertions;
using MissionManagement.Application.Exceptions;
using Xunit;

namespace MissionManagement.Application.Tests.CoverageGaps;

public sealed class MissionCoverageGapFillTests
{
    [Fact]
    public void ExternalDependencyException_WithInner_Preserves()
    {
        var inner = new Exception("x");
        var ex = new ExternalDependencyException("dep", inner);
        ex.Message.Should().Be("dep");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void UnauthorizedException_StoresMessage()
    {
        var ex = new UnauthorizedException("nope");
        ex.Message.Should().Be("nope");
    }
}
