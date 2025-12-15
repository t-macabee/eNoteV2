namespace eNote.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(int userId, string username, IList<string> roles);
    }
}
