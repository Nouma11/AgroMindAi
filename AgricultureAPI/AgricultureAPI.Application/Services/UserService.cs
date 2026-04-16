using AgricultureAPI.Application.DTOs.User;
using AgricultureAPI.Application.Interfaces;

namespace AgricultureAPI.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepo;

    public UserService(IUserRepository userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<UserDto?> GetByIdAsync(string id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) return null;

        return MapToDto(user);
    }

    public async Task<UserDto> UpdateProfileAsync(string id, UpdateProfileRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null) throw new Exception("User not found.");

        user.FullName = request.FullName;
        user.AvatarUrl = request.AvatarUrl;
        user.Phone = request.Phone;
        user.Location = request.Location;
        user.FarmSize = request.FarmSize;
        if (request.Language != null) user.Language = request.Language;
        user.Bio = request.Bio;

        await _userRepo.UpdateAsync(user);

        return MapToDto(user);
    }

    private static UserDto MapToDto(Domain.Entities.User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        Location = user.Location,
        FarmSize = user.FarmSize,
        Language = user.Language,
        Bio = user.Bio,
        Role = user.Role,
        AvatarUrl = user.AvatarUrl,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt
    };
}