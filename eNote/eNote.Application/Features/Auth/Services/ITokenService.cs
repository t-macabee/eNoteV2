namespace eNote.Application.Features.Auth.Services
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, IList<string> roles);
    }
}
