using AgricultureAPI.Application.DTOs.Common;
using AgricultureAPI.Application.DTOs.History;

namespace AgricultureAPI.Application.Interfaces;

public interface IHistoryService
{
    Task<PagedResult<HistoryDto>> GetUserHistoryAsync(string userId, int page, int limit);
    Task<HistoryDto?> GetByIdAsync(string id);
    Task<HistoryDto> CreateAsync(string userId, CreateHistoryRequest request);
    Task DeleteAsync(string id, string userId);
}