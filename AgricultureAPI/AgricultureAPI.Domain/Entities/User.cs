using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgricultureAPI.Domain.Entities;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("passwordHash")]
    public string PasswordHash { get; set; } = string.Empty;

    [BsonElement("fullName")]
    public string FullName { get; set; } = string.Empty;

    [BsonElement("avatarUrl")]
    public string? AvatarUrl { get; set; }

    [BsonElement("phone")]
    public string? Phone { get; set; }

    [BsonElement("location")]
    public string? Location { get; set; }

    [BsonElement("farmSize")]
    public string? FarmSize { get; set; }

    [BsonElement("language")]
    public string Language { get; set; } = "English";

    [BsonElement("bio")]
    public string? Bio { get; set; }

    [BsonElement("role")]
    public string Role { get; set; } = "free";

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("lastLoginAt")]
    public DateTime? LastLoginAt { get; set; }
}