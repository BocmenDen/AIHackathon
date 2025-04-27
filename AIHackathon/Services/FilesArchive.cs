using Microsoft.Extensions.Hosting;
using System.Text;

namespace AIHackathon.Services
{
    [Service(ServiceType.Hosted)]
    public class FilesArchive(FilesStorage storage) : IHostedService
    {
        private const string TmpPath = "FilesArchive";

        public async Task<Archive> Open(string path)
        {
            Dictionary<string, string> files = [];
            using var fs = await storage.OpenReadFile(path);
            int offset = 0;
            while (true)
            {
                // Чтение имени ресурса (до первого символа новой строки)
                StringBuilder resourceNameBuilder = new();
                int currentByte;
                while ((currentByte = fs.ReadByte()) != '\n' && currentByte != -1)
                    resourceNameBuilder.Append((char)currentByte);

                if (currentByte == -1) break;

                string resourceName = resourceNameBuilder.ToString();

                // Чтение диапазона (startPos-endPos)
                StringBuilder rangeBuilder = new();
                while ((currentByte = (byte)fs.ReadByte()) != '\n' && currentByte != -1)
                    rangeBuilder.Append((char)currentByte);

                string rangeStr = rangeBuilder.ToString();
                string[] rangeParts = rangeStr.Split('-');
                int startPos = int.Parse(rangeParts[0]);
                int endPos = int.Parse(rangeParts[1]);

                string outputPath = Path.Combine(TmpPath, Path.GetTempFileName() + resourceName);
                files[resourceName] = outputPath;

                using (var fileSave = await storage.CreateFile(outputPath))
                {
                    // Читаем только нужную часть
                    int lengthToRead = endPos - startPos;
                    await CopyStream(fs, fileSave, lengthToRead);
                }

                // Переходим к следующему ресурсу
                offset = (int)fs.Position;
            }
            return new Archive(storage, files);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1835:Старайтесь использовать перегрузки на основе памяти для \"ReadAsync\" и \"WriteAsync\"", Justification = "<Ожидание>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Удалить ненужное подавление", Justification = "<Ожидание>")]
        private static async Task CopyStream(Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 &&
                   (read = await input.ReadAsync(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                await output.WriteAsync(buffer, 0, read);
                bytes -= read;
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => storage.ClearFolder(TmpPath).AsTask();
        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public class Archive(FilesStorage storage, IReadOnlyDictionary<string, string> files) : IAsyncDisposable
    {
        private readonly FilesStorage _storage = storage??throw new ArgumentNullException(nameof(storage));
        public IReadOnlyDictionary<string, string> Files { get; init; } = files??throw new ArgumentNullException(nameof(files));

        public async ValueTask DisposeAsync()
        {
            foreach (var file in Files)
                await _storage.DeleteFile(file.Value);
            GC.SuppressFinalize(this);
        }
    }
}
