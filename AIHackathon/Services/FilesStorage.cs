#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CA1822 // Пометьте члены как статические

using AIHackathon.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AIHackathon.Services
{
    [Service(ServiceType.Hosted)]
    public class FilesStorage(IOptions<Settings> options) : IHostedService
    {
        private const string PathTempFiles = "TmpFiles";

        public ValueTask<Stream> OpenReadFile(string subPath) => new(File.OpenRead(Path.Combine(options.Value.PathRoot, subPath)));

        public ValueTask<Stream> CreateFile(string subPath)
        {
            subPath = Path.Combine(options.Value.PathRoot, subPath);
            var dir = Path.GetDirectoryName(subPath);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return new(File.Create(subPath));
        }

        public ValueTask DeleteFile(string subPath)
        {
            subPath = Path.Combine(options.Value.PathRoot, subPath);
            if (!File.Exists(subPath)) return ValueTask.CompletedTask;
            File.Delete(Path.Combine(options.Value.PathRoot, subPath));
            return ValueTask.CompletedTask;
        }

        public async ValueTask ClearFolder(string subPath)
        {
            subPath = Path.Combine(options.Value.PathRoot, subPath);
            if (!Directory.Exists(subPath)) return;
            var files = Directory.GetFiles(subPath);
            var directories = Directory.GetDirectories(subPath);

            foreach (var file in files)
                File.Delete(file);
            foreach (var directory in directories)
                await ClearFolder(directory);
        }

        public async ValueTask<TempFileInfo> CreateTempFile(string ex = ".tmp")
        {
            var fileName = Path.GetRandomFileName() + ex;
            var filePath = Path.Combine(PathTempFiles, fileName);
            var file = await CreateFile(filePath);
            return new TempFileInfo()
            {
                Stream = file,
                Path = filePath,
            };
        }

        public ValueTask<string> GetCurrentComputerPath(string subPath) => new(Path.GetFullPath(Path.Combine(options.Value.PathRoot, subPath)));

        public Task StartAsync(CancellationToken cancellationToken) => ClearFolder(PathTempFiles).AsTask();
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public readonly struct TempFileInfo : IDisposable
    {
        public readonly string Path { get; init; }
        public readonly Stream Stream { get; init; }

        public void Dispose()
        {
            Stream.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
