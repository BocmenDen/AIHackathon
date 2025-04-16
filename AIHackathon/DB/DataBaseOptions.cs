namespace AIHackathon.DB
{
    public class DataBaseOptions
    {
        public string? Connection { get; set; }
        public TimeSpan? TimeoutCacheUser { get; set; }

        public string GetPathOrDefault()
            => string.IsNullOrWhiteSpace(Connection) ? $"{System.IO.Path.GetRandomFileName()}.db" : Connection;

        public TimeSpan GetTimeoutCacheUserOrDefault() => TimeoutCacheUser ?? TimeSpan.FromMinutes(5);
    }
}
