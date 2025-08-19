namespace DiceOnlineAPI.Models
{
    public class Dice
    {
        public int Index { get; set; }
        public int MinValue { get; set; } = 1; // Default minimum value
        public int MaxValue { get; set; } = 6; // Default maximum value
    }
}
