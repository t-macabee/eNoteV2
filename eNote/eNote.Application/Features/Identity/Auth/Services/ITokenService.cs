namespace eNote.Application.Features.Identity.Auth.Services;

public interface ITokenService
{
    string GenerateToken(int userId, string username, IList<string> roles, bool isManager = false);
}
