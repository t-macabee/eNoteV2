namespace eNote.Application.Features.Auth.Services.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, IList<string> roles);
    }
}
