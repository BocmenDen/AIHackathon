#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CA1822 // Пометьте члены как статические

using AIHackathon.Models;
using Microsoft.Extensions.Options;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class FilesStorage(IOptions<Settings> options)
    {
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

        public ValueTask<string> GetCurrentComputerPath(string subPath) => new(Path.GetFullPath(Path.Combine(options.Value.PathRoot, subPath)));
        public ValueTask ReturnCurrentComputerPath(string _) => ValueTask.CompletedTask;
    }
}
