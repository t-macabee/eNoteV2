using eNote.Application.Features.Auth.DTOs;
using eNote.Application.Features.Auth.Services.Interfaces;
using eNote.Application.Features.Auth.Requests;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers.Auth
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService _authService = authService;

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest model)
        {
            var (response, error) = await _authService.Login(model);

            if (response is null)
                return Unauthorized(new { message = error });

            return Ok(response);
        }
    }
}
