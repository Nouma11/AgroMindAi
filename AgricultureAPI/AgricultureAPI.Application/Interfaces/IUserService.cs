using AgricultureAPI.Application.DTOs.User;

namespace AgricultureAPI.Application.Interfaces;

public interface IUserService
{
    Task<UserDto?> GetByIdAsync(string id);
    Task<UserDto> UpdateProfileAsync(string id, UpdateProfileRequest request);
}