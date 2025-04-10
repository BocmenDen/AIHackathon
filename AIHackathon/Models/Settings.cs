namespace AIHackathon.Models
{
    public class Settings
    {
        public int MaxCountMetricsCommand { get; set; }
        public required string LinkNewsGroup { get; set; }
        public int CountCommandsInPage { get; set; }
        public required string[] ValidTypesHandleMediaFile { get; set; }
    }
}
