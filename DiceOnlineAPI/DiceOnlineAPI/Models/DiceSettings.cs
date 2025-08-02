namespace DiceOnlineAPI.Models
{
    public class DiceSettings
    {
        public int Count { get; set; }
        public int MinValue { get; set; } = 1; // Default minimum value
        public int MaxValue { get; set; } = 6; // Default maximum value
    }
}
