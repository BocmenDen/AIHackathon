#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CA1822 // Пометьте члены как статические

using AIHackathon.Models;
using Microsoft.Extensions.Options;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class FilesStorage(IOptions<Settings> options)
    {
        public ValueTask<Stream> OpenReadFile(string path) => new(File.OpenRead(Path.Combine(options.Value.PathRoot, path)));

        public ValueTask<Stream> CreateFile(string path)
        {
            path = Path.Combine(options.Value.PathRoot, path);
            var dir = Path.GetDirectoryName(path);
            if (dir != null && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return new(File.Create(path));
        }

        public ValueTask DeleteFile(string path)
        {
            path = Path.Combine(options.Value.PathRoot, path);
            if (!File.Exists(path)) return ValueTask.CompletedTask;
            File.Delete(Path.Combine(options.Value.PathRoot, path));
            return ValueTask.CompletedTask;
        }

        public async ValueTask ClearFolder(string path)
        {
            path = Path.Combine(options.Value.PathRoot, path);
            if (!Directory.Exists(path)) return;
            var files = Directory.GetFiles(path);
            var directories = Directory.GetDirectories(path);

            foreach (var file in files)
                File.Delete(file);
            foreach (var directory in directories)
                await ClearFolder(directory);
        }
    }
}
