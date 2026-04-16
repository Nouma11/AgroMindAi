namespace AgricultureAPI.Application.DTOs.History;

public class CreateHistoryRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string AIResponse { get; set; } = string.Empty;
    public string? ZoneGeoJson { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
}