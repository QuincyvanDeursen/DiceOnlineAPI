using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DiceOnlineAPI.Models
{
    public class Lobby
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)] 
        public Guid Id { get; set; }
        public string LobbyCode { get; set; } = string.Empty;
        public List<string> Players { get; set; } = new();

        public DiceSettings DiceSettings { get; set; } = new DiceSettings();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
