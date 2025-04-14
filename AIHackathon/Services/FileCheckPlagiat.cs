using AIHackathon.DB;
using AIHackathon.DB.Models;
using BotCore.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class FileCheckPlagiat(FilesStorage filesStorage, ConditionalPooledObjectProvider<DataBase> db)
    {
        public async Task<FileCheckPlagiatResult> CheckPlagiat(string pathFile)
        {
            var file = await filesStorage.OpenReadFile(pathFile);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(file);
            var hashLine = Convert.ToHexStringLower(hash);
            var findResult = await db.TakeObjectAsync(x => x.Metrics.Include(x => x.Participant).ThenInclude(x => x!.Command).FirstOrDefaultAsync(x => x.FileHash == hashLine));
            if (findResult?.PathFile is not null)
            {
                if (!file.CanSeek)
                {
                    await file.DisposeAsync();
                    file = await filesStorage.OpenReadFile(pathFile);
                }
                else
                    file.Position = 0;
                using var filePlagiat = await filesStorage.OpenReadFile(findResult?.PathFile!);
                if (!AreStreamsEqual(file, filePlagiat))
                    findResult = null;
            }
            await file.DisposeAsync();
            return new FileCheckPlagiatResult()
            {
                Hash = hashLine,
                PlagiatMetricParticipant = findResult
            };
        }

        private static bool AreStreamsEqual(Stream stream, Stream other)
        {
            const int bufferSize = 2048;
            if (other.Length != stream.Length)
            {
                return false;
            }

            byte[] buffer = new byte[bufferSize];
            byte[] otherBuffer = new byte[bufferSize];
            while ((_ = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                var _ = other.Read(otherBuffer, 0, otherBuffer.Length);

                if (!otherBuffer.SequenceEqual(buffer))
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    other.Seek(0, SeekOrigin.Begin);
                    return false;
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            other.Seek(0, SeekOrigin.Begin);
            return true;
        }

    }
    public readonly struct FileCheckPlagiatResult
    {
        public string Hash { get; init; }
        public MetricParticipant? PlagiatMetricParticipant { get; init; }

        public readonly bool IsPlagiat => PlagiatMetricParticipant is not null;
    }
}
