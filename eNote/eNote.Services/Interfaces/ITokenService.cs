using eNote.Infrastructure.Identity;

namespace eNote.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateToken(AppUser user, IList<string> roles);
    }
}
