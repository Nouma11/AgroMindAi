namespace AgricultureAPI.Application.DTOs.History;

public class HistoryDto
{
    public string Id { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public string? ZoneGeoJson { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}