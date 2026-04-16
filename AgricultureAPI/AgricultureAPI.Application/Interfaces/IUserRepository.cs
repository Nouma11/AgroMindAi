using AgricultureAPI.Domain.Entities;

namespace AgricultureAPI.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(string id);
    Task<User?> GetByEmailAsync(string email);
    Task InsertAsync(User user);
    Task UpdateAsync(User user);
    Task DeleteAsync(string id);
}