namespace UserService.Application.Common.Interfaces;

public interface IAccessTokenReader
{
    (Guid UserId, IReadOnlyList<string> Roles) Read(string accessToken);
}
