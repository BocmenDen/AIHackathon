namespace AIHackathon.DB
{
    public class DataBaseOptions
    {
        public string? Path { get; set; }
        public TimeSpan? TimeoutCacheUser { get; set; }

        public string GetPathOrDefault()
            => string.IsNullOrWhiteSpace(Path) ? $"{System.IO.Path.GetRandomFileName()}.db" : Path;

        public TimeSpan GetTimeoutCacheUserOrDefault() => TimeoutCacheUser ?? TimeSpan.FromMinutes(5);
    }
}
