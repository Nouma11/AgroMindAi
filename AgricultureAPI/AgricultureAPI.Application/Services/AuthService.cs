using AgricultureAPI.Application.DTOs.Auth;
using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;


namespace AgricultureAPI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IRefreshTokenRepository _refreshTokenRepo;
    private readonly IConfiguration _config;

    public AuthService(
        IUserRepository userRepo,
        IRefreshTokenRepository refreshTokenRepo,
        IConfiguration config)
    {
        _userRepo = userRepo;
        _refreshTokenRepo = refreshTokenRepo;
        _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await _userRepo.GetByEmailAsync(request.Email);
        if (existing != null)
            throw new Exception("Email already in use.");

        var user = new User
        {
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            Role = "free",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.InsertAsync(user);
        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> SignInAsync(SignInRequest request)
    {
        var user = await _userRepo.GetByEmailAsync(request.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new Exception("Invalid email or password.");

        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);

        return await GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var token = await _refreshTokenRepo.GetByTokenAsync(refreshToken);
        if (token == null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            throw new Exception("Invalid or expired refresh token.");

        var user = await _userRepo.GetByIdAsync(token.UserId);
        if (user == null)
            throw new Exception("User not found.");

        await _refreshTokenRepo.RevokeAsync(refreshToken);
        return await GenerateAuthResponse(user);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        await _refreshTokenRepo.RevokeAsync(refreshToken);
    }

    private async Task<AuthResponse> GenerateAuthResponse(User user)
    {
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        var expiryDays = int.Parse(_config["JwtSettings:RefreshTokenExpiryDays"]!);

        await _refreshTokenRepo.InsertAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(expiryDays)
        });

        var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"]!);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expiryMinutes),
            User = new UserInfo
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.Role,
                AvatarUrl = user.AvatarUrl
            }
        };
    }

    private string GenerateJwtToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["JwtSettings:SecretKey"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiryMinutes = int.Parse(_config["JwtSettings:ExpiryMinutes"]!);

        var claims = new[]
        {
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName),
            new Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"],
            audience: _config["JwtSettings:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}