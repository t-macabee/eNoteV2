using eNote.Application.Common.Exceptions;
using eNote.Application.Common.Interfaces;
using eNote.Application.Common.Localization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Services
{
    public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
    {
        public int UserId
        {
            get
            {
                var user = httpContextAccessor.HttpContext?.User;

                var id = user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(id, out var userId))
                {
                    throw new AuthenticationException(Messages.InvalidUserClaim);
                }

                return userId;
            }
        }

        public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}
