using AgricultureAPI.Domain.Entities;

namespace AgricultureAPI.Application.Interfaces;

public interface IHistoryRepository
{
    Task<List<AnalysisHistory>> GetByUserIdAsync(string userId, int page, int limit);
    Task<long> CountByUserIdAsync(string userId);
    Task<AnalysisHistory?> GetByIdAsync(string id);
    Task InsertAsync(AnalysisHistory history);
    Task DeleteAsync(string id);
}