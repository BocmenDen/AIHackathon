using AIHackathon.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public partial class TestingModel(FilesStorage filesStorage, IOptions<Settings> options) : IDisposable
    {
        private readonly DockerClient client = new DockerClientConfiguration(new Uri(options.Value.DockerUri)).CreateClient();

        public async Task<TestModelResult> Testing(string pathFile, string fileType)
        {
            //return new TestModelResult()
            //{
            //    Accuracy = Random.Shared.NextDouble(),
            //    Library = "Test"
            //};
            var fullPath = await filesStorage.GetCurrentComputerPath(pathFile);

            var parameters = new CreateContainerParameters()
            {
                Image = options.Value.DockerName,
                HostConfig = new HostConfig()
                {
                    AutoRemove = false,
                    Binds = [.. options.Value.PathDockerInputFiles.Select(x => $"{Path.GetFullPath(x)}:/app/{Path.GetFileName(x)}:ro")],
                    NetworkMode = "none"
                }
            };
            parameters.HostConfig.Binds.Add($"{fullPath}:/app/model.{fileType}:ro");

            var container = await client.Containers.CreateContainerAsync(parameters);

            var parametersLog = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Follow = false,
                Timestamps = false
            };
            await client.Containers.StartContainerAsync(container.ID, new());
            await client.Containers.WaitContainerAsync(container.ID);
            using MultiplexedStream stream = await client.Containers.GetContainerLogsAsync(container.ID, true, parametersLog);
            var (output, error) = await stream.ReadOutputToEndAsync(default);

            await filesStorage.ReturnCurrentComputerPath(fullPath);
            await client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters
            {
                Force = true
            });

            if (!string.IsNullOrWhiteSpace(error))
            {
                return new TestModelResult()
                {
                    Accuracy = 0,
                    Library = null,
                    Error = error
                };
            }
            Regex regex = RegexGetJson();
            var match = regex.Match(output);
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<TestModelResult?>(match.Value);
                return result!.Value;
            }
            catch
            {
            }

            return new TestModelResult()
            {
                Accuracy = 0,
                Library = null,
                Error = "Не удалось получить ответ от модуля тестирования"
            };
        }
        public void Dispose()
        {
            client.Dispose();
            GC.SuppressFinalize(this);
        }

        [GeneratedRegex(@"\{(?:[^{}]|(?<Open>\{)|(?<-Open>\}))*\}")]
        private static partial Regex RegexGetJson();
    }
    public struct TestModelResult
    {
        public double Accuracy { get; set; }
        public string? Library { get; set; }
        public string? Error { get; set; }

        public readonly bool IsError => !string.IsNullOrWhiteSpace(Error);
    }
}
