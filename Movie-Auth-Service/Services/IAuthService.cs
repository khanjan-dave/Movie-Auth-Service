using Movie_Auth_Service.DTOs;

namespace Movie_Auth_Service.Services
{
    public interface IAuthService
    {
        Task<RegisterResponseDto> Register(RegisterDto request);
        Task<AuthResponseDto> Login(LoginDto request);
    }
}