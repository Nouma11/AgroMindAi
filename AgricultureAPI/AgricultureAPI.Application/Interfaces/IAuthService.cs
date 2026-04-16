using AgricultureAPI.Application.DTOs.Auth;

namespace AgricultureAPI.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> SignInAsync(SignInRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}