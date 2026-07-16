using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using UserService.Application.Common;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;
using UserService.Application.Operators.Commands.CreateOperator;
using UserService.Application.Operators.Commands.ResendOperatorActivation;
using Xunit;

namespace UserService.Application.Tests.Operators;

public sealed class CreateOperatorHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailSucceeds_ReturnsActivationEmailSentTrue()
    {
        var identity = new Mock<IIdentityService>();
        var mailer = new Mock<IOperatorActivationMailer>();
        identity
            .Setup(x => x.CreateOperatorAsync("Ana", "Perez", "ana@umbral.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorSetupCredentials(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "ana@umbral.test",
                "Ana",
                "Perez",
                "ABCD-EFGH",
                7));

        var handler = new CreateOperatorHandler(
            identity.Object,
            mailer.Object,
            NullLogger<CreateOperatorHandler>.Instance);

        var result = await handler.Handle(
            new CreateOperatorCommand("Ana", "Perez", "ana@umbral.test"),
            CancellationToken.None);

        result.ActivationEmailSent.Should().BeTrue();
        result.Email.Should().Be("ana@umbral.test");
        result.OperatorId.ToString().Should().Be("11111111-1111-1111-1111-111111111111");
        mailer.Verify(
            x => x.SendActivationCodeAsync(
                "ana@umbral.test",
                "Ana",
                "ABCD-EFGH",
                7,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailFails_ReturnsActivationEmailSentFalse()
    {
        var identity = new Mock<IIdentityService>();
        var mailer = new Mock<IOperatorActivationMailer>();
        identity
            .Setup(x => x.CreateOperatorAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorSetupCredentials(
                Guid.NewGuid(),
                "ana@umbral.test",
                "Ana",
                "Perez",
                "ABCD-EFGH",
                7));
        mailer
            .Setup(x => x.SendActivationCodeAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExternalDependencyException("SMTP down"));

        var handler = new CreateOperatorHandler(
            identity.Object,
            mailer.Object,
            NullLogger<CreateOperatorHandler>.Instance);

        var result = await handler.Handle(
            new CreateOperatorCommand("Ana", "Perez", "ana@umbral.test"),
            CancellationToken.None);

        result.ActivationEmailSent.Should().BeFalse();
    }
}

public sealed class ResendOperatorActivationHandlerTests
{
    [Fact]
    public async Task Handle_WhenEmailSucceeds_ReturnsActivationEmailSentTrue()
    {
        var operatorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var identity = new Mock<IIdentityService>();
        var mailer = new Mock<IOperatorActivationMailer>();
        identity
            .Setup(x => x.RegenerateOperatorSetupCodeAsync(operatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OperatorSetupCredentials(
                operatorId,
                "op@umbral.test",
                "Luis",
                "Diaz",
                "WXYZ-1234",
                7));

        var handler = new ResendOperatorActivationHandler(
            identity.Object,
            mailer.Object,
            NullLogger<ResendOperatorActivationHandler>.Instance);

        var result = await handler.Handle(
            new ResendOperatorActivationCommand(operatorId),
            CancellationToken.None);

        result.ActivationEmailSent.Should().BeTrue();
        result.Email.Should().Be("op@umbral.test");
        mailer.Verify(
            x => x.SendActivationCodeAsync(
                "op@umbral.test",
                "Luis",
                "WXYZ-1234",
                7,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
