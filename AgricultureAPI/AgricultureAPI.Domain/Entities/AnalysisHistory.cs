using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AgricultureAPI.Domain.Entities;

public class AnalysisHistory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = string.Empty;

    [BsonElement("userId")]
    [BsonRepresentation(BsonType.ObjectId)]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("prompt")]
    public string Prompt { get; set; } = string.Empty;

    [BsonElement("aiResponse")]
    public string AIResponse { get; set; } = string.Empty;

    [BsonElement("zoneGeoJson")]
    public string? ZoneGeoJson { get; set; }

    [BsonElement("modelUsed")]
    public string ModelUsed { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}