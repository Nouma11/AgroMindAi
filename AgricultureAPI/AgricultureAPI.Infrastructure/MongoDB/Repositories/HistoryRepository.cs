using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Domain.Entities;
using AgricultureAPI.Infrastructure.MongoDB;
using MongoDB.Driver;

namespace AgricultureAPI.Infrastructure.Repositories;

public class HistoryRepository : IHistoryRepository
{
    private readonly IMongoCollection<AnalysisHistory> _col;

    public HistoryRepository(MongoDbContext context)
    {
        _col = context.History;
    }

    public async Task<List<AnalysisHistory>> GetByUserIdAsync(string userId, int page, int limit) =>
        await _col.Find(h => h.UserId == userId)
                  .SortByDescending(h => h.CreatedAt)
                  .Skip((page - 1) * limit)
                  .Limit(limit)
                  .ToListAsync();

    public async Task<long> CountByUserIdAsync(string userId) =>
        await _col.CountDocumentsAsync(h => h.UserId == userId);

    public async Task<AnalysisHistory?> GetByIdAsync(string id) =>
        await _col.Find(h => h.Id == id).FirstOrDefaultAsync();

    public async Task InsertAsync(AnalysisHistory history) =>
        await _col.InsertOneAsync(history);

    public async Task DeleteAsync(string id) =>
        await _col.DeleteOneAsync(h => h.Id == id);
}