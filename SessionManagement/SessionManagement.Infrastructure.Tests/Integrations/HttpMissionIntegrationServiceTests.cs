using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SessionManagement.Infrastructure.Integrations;
using Xunit;

namespace SessionManagement.Infrastructure.Tests.Integrations;

public sealed class HttpMissionIntegrationServiceTests
{
    [Fact]
    public async Task GetAssignedMissionsForOperatorAsync_ShouldFilterByOperatorId()
    {
        var operatorId = Guid.NewGuid();
        var otherOperatorId = Guid.NewGuid();

        var missions = new object[]
        {
            new { id = Guid.NewGuid(), title = "M1", operatorIds = new[] { operatorId } },
            new { id = Guid.NewGuid(), title = "M2", operatorIds = new[] { otherOperatorId } },
            new { id = Guid.NewGuid(), title = "M3", operatorIds = new[] { operatorId, otherOperatorId } }
        };

        var json = JsonSerializer.Serialize(missions);

        var handler = new StubHttpMessageHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri!.ToString().Should().EndWith("/api/v1/missions");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5260/") };
        var sut = new HttpMissionIntegrationService(client);

        var result = await sut.GetAssignedMissionsForOperatorAsync(operatorId, CancellationToken.None);

        result.Should().HaveCount(2);
        result.Select(x => x.OperatorId).Should().AllBeEquivalentTo(operatorId);
        result.Select(x => x.Title).Should().BeEquivalentTo(["M1", "M3"]);
    }

    [Fact]
    public async Task GetNodeValidationDataAsync_ShouldMapNodeValidations()
    {
        var missionId = Guid.NewGuid();
        var nodeId1 = Guid.NewGuid();
        var nodeId2 = Guid.NewGuid();

        var validations = new object[]
        {
            new { nodeId = nodeId1, nodeType = "Trivia", executionOrder = 1, baseScore = 100, expectedAnswers = new[] { "Bogota" } },
            new { nodeId = nodeId2, nodeType = "TreasureHunt", executionOrder = 2, baseScore = 150, expectedAnswers = new[] { "CODE-123" } }
        };

        var json = JsonSerializer.Serialize(validations);

        var handler = new StubHttpMessageHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri!.ToString().Should().EndWith($"/api/v1/missions/{missionId}/node-validations");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5260/") };
        var sut = new HttpMissionIntegrationService(client);

        var result = await sut.GetNodeValidationDataAsync(missionId, CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].NodeId.Should().Be(nodeId1);
        result[0].NodeType.Should().Be("Trivia");
        result[0].ExecutionOrder.Should().Be(1);
        result[0].BaseScore.Should().Be(100);
        result[0].ExpectedAnswers.Should().Equal("Bogota");
    }

    [Fact]
    public async Task GetMissionDifficultyMultiplierAsync_ShouldReturnMissionMultiplier()
    {
        var missionId = Guid.NewGuid();
        var mission = new
        {
            id = missionId,
            title = "M1",
            description = "Desc",
            status = "Draft",
            difficulty = "Medium",
            difficultyScoreMultiplier = 1.5m,
            maxDurationMinutes = 60,
            createdAtUtc = DateTime.UtcNow,
            lastModifiedAtUtc = (DateTime?)null,
            operatorIds = Array.Empty<Guid>()
        };

        var json = JsonSerializer.Serialize(mission);

        var handler = new StubHttpMessageHandler(req =>
        {
            req.Method.Should().Be(HttpMethod.Get);
            req.RequestUri!.ToString().Should().EndWith($"/api/v1/missions/{missionId}");
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:5260/") };
        var sut = new HttpMissionIntegrationService(client);

        var result = await sut.GetMissionDifficultyMultiplierAsync(missionId, CancellationToken.None);

        result.Should().Be(1.5m);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request));
    }
}

