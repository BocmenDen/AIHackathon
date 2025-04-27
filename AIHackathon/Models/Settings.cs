namespace AIHackathon.Models
{

    public class Settings
    {
        public int MaxCountMetricsCommand { get; set; } = 100;
        public required string LinkNewsGroup { get; set; }
        public int CountCommandsInPage { get; set; } = 10;
        public required string[] ValidTypesHandleMediaFile { get; set; }
        public string PathRoot { get; set; } = "./";
        public int WaitUpdateMessageTestingModel { get; set; } = 1000;
        public required string DockerName { get; set; }
        public required string DockerUri { get; set; }
        public TimeSpan DockerStopTimeout { get; set; } = TimeSpan.FromMinutes(3);
        public required string[] PathDockerInputFiles { get; set; }
        public List<Chapter> PageInfo { get; set; } = [];
        public required string ScriptMetricGenerator { get; set; }
        public required string ScriptMetricGeneratorArgLine { get; set; }
    }
}