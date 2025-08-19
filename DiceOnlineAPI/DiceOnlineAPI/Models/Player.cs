namespace DiceOnlineAPI.Models
{
    public class Player
    {
        public string Name { get; set; } = string.Empty;
        public string ConnectionId { get; set; } = string.Empty;
        public Player() { }
        public Player(string name, string connectionId)
        {
            Name = name;
            ConnectionId = connectionId;
        }
    }
}
