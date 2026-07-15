using System.Text.Json;
using ScoringAudit.Domain.ValueObjects;
using Xunit;

namespace ScoringAudit.Domain.Tests.ValueObjects;

public sealed class PenaltyReasonSerializationTests
{
    [Fact]
    public void JsonRoundTrip_DeserializesPrivateConstructorViaJsonConstructor()
    {
        var original = PenaltyReason.ForManualPenalty("POR FEO", Guid.NewGuid());
        var json = JsonSerializer.Serialize(original);
        var restored = JsonSerializer.Deserialize<PenaltyReason>(json);

        Assert.NotNull(restored);
        Assert.Equal(original.Description, restored!.Description);
        Assert.Equal(original.Category, restored.Category);
        Assert.Equal(original.AppliedByOperatorId, restored.AppliedByOperatorId);
    }

    [Fact]
    public void JsonDeserialize_ReadsPersistedManualPenaltyShape()
    {
        const string json =
            """{"Category":0,"Description":"porque me da la gana","AppliedByOperatorId":"75c84ff0-323c-4872-a5c3-fb7b58c755e1"}""";

        var restored = JsonSerializer.Deserialize<PenaltyReason>(json);

        Assert.NotNull(restored);
        Assert.Equal("porque me da la gana", restored!.Description);
        Assert.Equal(PenaltyCategory.ManualOperator, restored.Category);
        Assert.Equal(Guid.Parse("75c84ff0-323c-4872-a5c3-fb7b58c755e1"), restored.AppliedByOperatorId);
    }
}
