using MediatR;
using MissionManagement.Application.Auth.Commands.AuthenticateUser;
using MissionManagement.Application.Common.Interfaces;
using MissionManagement.Application.Exceptions;
using Moq;

namespace MissionManagement.Application.Tests.Auth.Commands.AuthenticateUser;

public sealed class AuthenticateUserHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock = new();
    private readonly AuthenticateUserHandler _handler;

    public AuthenticateUserHandlerTests()
    {
        _handler = new AuthenticateUserHandler(_authServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsTokenResult()
    {
        var expected = new AuthTokenResult(
            AccessToken: "access-token",
            RefreshToken: "refresh-token",
            ExpiresIn: 300,
            UserId: Guid.NewGuid(),
            Roles: ["admin"]);

        _authServiceMock
            .Setup(service => service.AuthenticateAsync("admin@umbral.com", "Password1!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await _handler.Handle(
            new AuthenticateUserCommand("admin@umbral.com", "Password1!"),
            CancellationToken.None);

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task Handle_WithEmptyUsername_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new AuthenticateUserCommand("", "Password1!"), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithEmptyPassword_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(new AuthenticateUserCommand("admin@umbral.com", ""), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenAuthServiceThrowsUnauthorized_PropagatesException()
    {
        _authServiceMock
            .Setup(service => service.AuthenticateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedException("Credenciales inválidas o cuenta deshabilitada."));

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            _handler.Handle(new AuthenticateUserCommand("admin@umbral.com", "wrong"), CancellationToken.None));
    }
}
