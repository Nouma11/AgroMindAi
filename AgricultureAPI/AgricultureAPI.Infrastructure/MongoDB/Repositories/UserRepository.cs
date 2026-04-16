using AgricultureAPI.Application.Interfaces;
using AgricultureAPI.Domain.Entities;
using AgricultureAPI.Infrastructure.MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AgricultureAPI.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IMongoCollection<User> _col;

    public UserRepository(MongoDbContext context)
    {
        _col = context.Users;
    }

    public async Task<User?> GetByIdAsync(string id) =>
        await _col.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<User?> GetByEmailAsync(string email) =>
        await _col.Find(u => u.Email == email).FirstOrDefaultAsync();

    public async Task InsertAsync(User user) =>
        await _col.InsertOneAsync(user);

    public async Task UpdateAsync(User user) =>
        await _col.ReplaceOneAsync(u => u.Id == user.Id, user);

    public async Task DeleteAsync(string id) =>
        await _col.DeleteOneAsync(u => u.Id == id);
}