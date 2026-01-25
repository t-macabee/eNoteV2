using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace eNote.API.Controllers.Base
{
    [ApiController]
    public abstract class CoreController : ControllerBase
    {
        protected int CurrentUserId
        {
            get
            {
                var userIdClaim = User.FindFirst("sub")?.Value
                               ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    throw new UnauthorizedAccessException("User ID not found in token.");

                return userId;
            }
        }
    }
}
