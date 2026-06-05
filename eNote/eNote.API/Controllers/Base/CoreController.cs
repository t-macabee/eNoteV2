using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace eNote.API.Controllers.Base
{
    [ApiController]
    [Authorize]
    public abstract class CoreController : ControllerBase
    {
        protected int CurrentUserId
        {
            get
            {
                var id = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                      ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!int.TryParse(id, out var userId))
                    throw new InvalidOperationException("Authenticated user has no valid user id claim.");

                return userId;
            }
        }
    }
}
