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
        public List<Player> Players { get; set; } = new();

        public List<Dice> Dices { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
