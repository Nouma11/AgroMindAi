using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Domain.Entities;
using AgricultureAPI.Infrastructure.MongoDB;
using MongoDB.Driver;

namespace AgricultureAPI.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly IMongoCollection<RefreshToken> _col;

    public RefreshTokenRepository(MongoDbContext context)
    {
        _col = context.RefreshTokens;
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token) =>
        await _col.Find(t => t.Token == token && !t.IsRevoked).FirstOrDefaultAsync();

    public async Task InsertAsync(RefreshToken token) =>
        await _col.InsertOneAsync(token);

    public async Task RevokeAsync(string token)
    {
        var update = Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true);
        await _col.UpdateOneAsync(t => t.Token == token, update);
    }

    public async Task RevokeAllByUserIdAsync(string userId)
    {
        var update = Builders<RefreshToken>.Update.Set(t => t.IsRevoked, true);
        await _col.UpdateManyAsync(t => t.UserId == userId, update);
    }
}