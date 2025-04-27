using AIHackathon.Models;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AIHackathon.Services
{
    [Service(ServiceType.Hosted)]
    public partial class DockerGenerateOutput(IOptions<Settings> options, FilesStorage filesStorage) : IDisposable, IHostedService
    {
        private readonly DockerClient client = new DockerClientConfiguration(new Uri(options.Value.DockerUri)).CreateClient();
        private const string SubPath = "DockerOutput";

        [GeneratedRegex(@"\{(?:[^{}]|(?<Open>\{)|(?<-Open>\}))*\}")]
        private static partial Regex RegexGetJson();

        public async Task<DockerOutput> Testing(Archive archive)
        {
            var pathFile = Path.Combine(SubPath, Path.GetRandomFileName());
            await (await filesStorage.CreateFile(pathFile)).DisposeAsync();
            var fullPath = await filesStorage.GetCurrentComputerPath(pathFile);
            var parameters = new CreateContainerParameters()
            {
                Image = options.Value.DockerName,
                NetworkDisabled = true,
                StopTimeout = options.Value.DockerStopTimeout,
                HostConfig = new HostConfig()
                {
                    AutoRemove = false,
                    Binds = [.. options.Value.PathDockerInputFiles.Select(x => $"{Path.GetFullPath(x)}:/app/{Path.GetFileName(x)}:ro")],
                    NetworkMode = "none"
                }
            };
            foreach (var file in archive.Files)
            {
                var pathFileArchive = await filesStorage.GetCurrentComputerPath(file.Value);
                parameters.HostConfig.Binds.Add($"{pathFileArchive}:/app/{file.Key}:ro");
            }
            parameters.HostConfig.Binds.Add($"{fullPath}:/app/output.txt");

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

            await client.Containers.RemoveContainerAsync(container.ID, new ContainerRemoveParameters
            {
                Force = true
            });

            if (!string.IsNullOrWhiteSpace(error))
            {
                return new DockerOutput()
                {
                    Error = error
                };
            }
            Regex regex = RegexGetJson();
            var match = regex.Match(output);
            try
            {
                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<DockerOutput?>(match.Value);
                return result!.Value;
            }
            catch
            {
                return new DockerOutput()
                {
                    PathOutput = fullPath
                };
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => filesStorage.ClearFolder(SubPath).AsTask();
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public void Dispose()
        {
            client.Dispose();
            GC.SuppressFinalize(this);
        }
    }
    public struct DockerOutput
    {
        public string? Error { get; set; }
        public string? PathOutput { get; set; }
        public readonly bool IsError => !string.IsNullOrWhiteSpace(Error);
    }
}
