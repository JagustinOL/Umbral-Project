using FluentAssertions;
using Moq;
using UserService.Application.Auth.Queries.ValidateToken;
using UserService.Application.Common.Interfaces;
using UserService.Application.Exceptions;
using UserService.Domain.Aggregates;
using UserService.Domain.Enums;
using UserService.Domain.Repositories;

namespace UserService.Application.Tests.Auth.Queries.ValidateToken;

public sealed class ValidateTokenHandlerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock = new();
    private readonly Mock<IAccessTokenReader> _accessTokenReaderMock = new();
    private readonly ValidateTokenHandler _handler;

    public ValidateTokenHandlerTests()
    {
        _handler = new ValidateTokenHandler(_userRepositoryMock.Object, _accessTokenReaderMock.Object);
    }

    [Fact]
    public async Task Handle_WhenUserExists_ReturnsLowercaseRoleFromDatabase()
    {
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "admin@umbral.com", "UMBRAL", "Admin", UserRole.Admin, UserStatus.Active);

        _accessTokenReaderMock
            .Setup(reader => reader.Read("token"))
            .Returns((userId, new[] { "admin", "operator" }));
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _handler.Handle(new ValidateTokenQuery("token"), CancellationToken.None);

        result.Role.Should().Be("admin");
        result.Roles.Should().Equal("admin");
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();

        _accessTokenReaderMock
            .Setup(reader => reader.Read("token"))
            .Returns((userId, new[] { "admin" }));
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _handler.Handle(new ValidateTokenQuery("token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Usuario no registrado en UserService.");
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ThrowsUnauthorized()
    {
        var userId = Guid.NewGuid();
        var user = User.Create(userId, "op@umbral.com", "Ana", "Op", UserRole.Operator, UserStatus.Inactive);

        _accessTokenReaderMock
            .Setup(reader => reader.Read("token"))
            .Returns((userId, new[] { "operator" }));
        _userRepositoryMock
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => _handler.Handle(new ValidateTokenQuery("token"), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("El usuario está inactivo.");
    }

    [Fact]
    public async Task Handle_WhenTokenMissing_ThrowsUnauthorized()
    {
        var act = () => _handler.Handle(new ValidateTokenQuery("  "), CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Token de acceso requerido.");
    }
}
