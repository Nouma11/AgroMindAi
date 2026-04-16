using AgricultureAPI.Application.DTOs.Common;
using AgricultureAPI.Application.DTOs.History;
using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Domain.Entities;

namespace AgricultureAPI.Application.Services;

public class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _historyRepo;

    public HistoryService(IHistoryRepository historyRepo)
    {
        _historyRepo = historyRepo;
    }

    public async Task<PagedResult<HistoryDto>> GetUserHistoryAsync(string userId, int page, int limit)
    {
        var items = await _historyRepo.GetByUserIdAsync(userId, page, limit);
        var total = await _historyRepo.CountByUserIdAsync(userId);

        return new PagedResult<HistoryDto>
        {
            Data = items.Select(h => ToDto(h)).ToList(),
            Page = page,
            Limit = limit,
            Total = total
        };
    }

    public async Task<HistoryDto?> GetByIdAsync(string id)
    {
        var item = await _historyRepo.GetByIdAsync(id);
        return item == null ? null : ToDto(item);
    }

    public async Task<HistoryDto> CreateAsync(string userId, CreateHistoryRequest request)
    {
        var history = new AnalysisHistory
        {
            UserId = userId,
            Prompt = request.Prompt,
            AIResponse = request.AIResponse,
            ZoneGeoJson = request.ZoneGeoJson,
            ModelUsed = request.ModelUsed,
            CreatedAt = DateTime.UtcNow
        };

        await _historyRepo.InsertAsync(history);
        return ToDto(history);
    }

    public async Task DeleteAsync(string id, string userId)
    {
        var item = await _historyRepo.GetByIdAsync(id);
        if (item == null || item.UserId != userId)
            throw new Exception("History item not found.");

        await _historyRepo.DeleteAsync(id);
    }

    private static HistoryDto ToDto(AnalysisHistory h) => new()
    {
        Id = h.Id,
        Prompt = h.Prompt,
        AIResponse = h.AIResponse,
        ZoneGeoJson = h.ZoneGeoJson,
        ModelUsed = h.ModelUsed,
        CreatedAt = h.CreatedAt
    };
}