using eNote.Application.Interfaces;
using eNote.Contracts.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace eNote.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController(IAuthService authService) : ControllerBase
    {
        private readonly IAuthService authService = authService;

        [HttpPost("login")]
        [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var (response, error) = await authService.Login(model);

            return response is not null ? Ok(response) : Unauthorized(new { message = error }); 
        }
    }
}
