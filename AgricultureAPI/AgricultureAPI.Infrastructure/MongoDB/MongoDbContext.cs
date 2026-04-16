using AgricultureAPI.Domain.Entities;
using AgricultureAPI.Infrastructure.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace AgricultureAPI.Infrastructure.MongoDB
{
    public class MongoDbContext
    {
        public IMongoCollection<User> Users { get; }
        public IMongoCollection<AnalysisHistory> History { get; }
        public IMongoCollection<RefreshToken> RefreshTokens { get; }

        public MongoDbContext(IOptions<MongoDbSettings> options)
        {
            var settings = options.Value;
            var client = new MongoClient(settings.ConnectionString);
            var db = client.GetDatabase(settings.DatabaseName);

            Users = db.GetCollection<User>("users");
            History = db.GetCollection<AnalysisHistory>("history");
            RefreshTokens = db.GetCollection<RefreshToken>("refreshTokens");
        }
    }
}
