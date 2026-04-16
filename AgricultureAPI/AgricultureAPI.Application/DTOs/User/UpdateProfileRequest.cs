namespace AgricultureAPI.Application.DTOs.User;

public class UpdateProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? FarmSize { get; set; }
    public string? Language { get; set; }
    public string? Bio { get; set; }
}