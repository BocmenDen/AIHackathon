namespace AIHackathon.Models
{
    public class MessageSpamOptions
    {
        public int CountMessage { get; set; } = 5;
        public TimeSpan IntervalMessages { get; set; } = TimeSpan.FromSeconds(4);
        public TimeSpan IntervalBan { get; set; } = TimeSpan.FromMinutes(5);
    }
}
