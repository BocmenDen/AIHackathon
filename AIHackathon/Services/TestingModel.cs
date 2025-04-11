namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class TestingModel(FilesStorage filesStorage)
    {
        public Task<TestModelResult> Testing(string pathFile, string fileType)
        {
            return Task.FromResult(new TestModelResult()
            {
                Accuracy = Random.Shared.NextDouble(),
                Library = "TestLibrary",
                Error = null
            });
        }
    }
    public struct TestModelResult
    {
        public double Accuracy { get; set; }
        public required string Library { get; set; }
        public string? Error { get; set; }

        public readonly bool IsError => !string.IsNullOrWhiteSpace(Error);
    }
}
