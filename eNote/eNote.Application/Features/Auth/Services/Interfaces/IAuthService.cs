using eNote.Application.Features.Auth.DTOs;
using eNote.Application.Features.Auth.Requests;

namespace eNote.Application.Features.Auth.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(AuthResponse? response, string? error)> Login(LoginRequest model);
    }
}
