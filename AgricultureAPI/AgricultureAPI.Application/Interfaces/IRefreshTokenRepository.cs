using AgricultureAPI.Domain.Entities;

namespace AgricultureAPI.Application.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task InsertAsync(RefreshToken token);
    Task RevokeAsync(string token);
    Task RevokeAllByUserIdAsync(string userId);
}