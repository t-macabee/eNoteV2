namespace eNote.Application.Interfaces.Identity
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, IList<string> roles);
    }
}
