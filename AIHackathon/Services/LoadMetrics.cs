using AIHackathon.Model;
using Microsoft.Extensions.Configuration;
using OneBot;
using OneBot.Attributes;
using OneBot.Models;
using System.Diagnostics;

namespace AIHackathon.Services
{
    [Service]
    public class LoadMetrics(IConfiguration configuration, ContextBot<User, DataBase> contextBot)
    {
        public const string KeyDirectory = "modelsPathStorage";
        public const string KeyPathPython = "pythonPathExe";
        public const string KeyPathScript = "pythonPathScript";
        public const string KeyPathDBModel = "pythonPathDBModel";
        public const string KeyFunctionName = "pythonNameFunction";
        public const string KeySplitArgument = "pythonSplitArgument";

        private readonly string _pathPython = configuration[KeyPathPython] ?? throw new Exception("Отсутствуют данные о расположении Python");
        private readonly string _pathScript = configuration[KeyPathScript] ?? throw new Exception("Отсутствуют данные о расположении скрипта");
        private readonly string _pathDBModel = configuration[KeyPathDBModel] ?? throw new Exception("Отсутствуют данные о расположении модели");
        private readonly string _splitArgs = configuration[KeySplitArgument] ?? throw new Exception("Отсутствуют данные о разбиении полученных данных на аргументы");

        private readonly string _directoryFiles = configuration[KeyDirectory] ?? throw new Exception("Нед данных о расположении хранилища моделей");
        private readonly ContextBot<User, DataBase> _contextBot = contextBot ?? throw new ArgumentNullException(nameof(contextBot));

        public async Task Run(ReceptionClient<User> updateData)
        {
            SendingClient sendingClient = new();
            MediaSource mediaSource = updateData.Medias![0];
            string subPath = Path.Combine(updateData.User.Id.ToString().Replace('-', '_'), $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}_{mediaSource.Name!}");
            string pathFile = Path.Combine(_directoryFiles, subPath);

            sendingClient.Message = "Загрузка началась! ⬇️ Подождите немного... ⏳";
            await updateData.Send(sendingClient);

            using (var stream = await mediaSource.GetStream())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pathFile)!);
                var writer = File.OpenWrite(pathFile);
                await stream.CopyToAsync(writer);
                writer.Close();
                writer.Dispose();
            }

            sendingClient.Message = "Погоди немного! ⏳ Сейчас я оцениваю модель... 🤔 Результаты будут скоро! \n✨";
            await updateData.Send(sendingClient);
            var metricTask = Load(pathFile);
            while (!metricTask.IsCompleted)
            {
                sendingClient.Message += "✨";
                await updateData.Send(sendingClient);
                Thread.Sleep(500);
            }
            var metric = await metricTask;
            if (metric.IsSuccess)
            {
                sendingClient.Message = "Сохраняю результаты... 💾 Почти готово! ⏳";
                await updateData.Send(sendingClient);
                var db = _contextBot.GetService<DataBase>();
                db.Metrics.Add(new MetricsUser(updateData.User.Id, metric.Accuracy, metric.Library!, subPath));
                await db.SaveChangesAsync();
            }
            else
            {
                File.Delete(pathFile);
            }

            sendingClient.Message = metric.ToString();
            await updateData.Send(sendingClient);
        }

        private async Task<MetricsModel> Load(string pathModel)
        {
            using Process process = new();
            process.StartInfo.FileName = _pathPython;
            process.StartInfo.Environment["TF_ENABLE_ONEDNN_OPTS"] = "0";
            process.StartInfo.Arguments = $"\"{_pathScript}\" {pathModel} {_pathDBModel}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrEmpty(error))
                    throw new Exception(error);
                string[] args = output.Replace("\n", "").Replace("\r", "").Replace('.', ',').Split(_splitArgs);
                if (args.Length != 2 || !double.TryParse(args[1], out double accuracy))
                    throw new Exception("Не удалось обработать полученные данные оценки модели, возможно не удалось определить тип модели");
                return new MetricsModel(args[0].Trim(), accuracy);
            }
            catch (Exception ex)
            {
                return new(ex);
            }
        }

        private struct MetricsModel
        {
            public double Accuracy;
            public string? Library;
            public Exception? Exception;
            public readonly bool IsSuccess => Exception == null && Library != null;

            public MetricsModel(Exception ex) => Exception = ex;
            public MetricsModel(string nameLibrary, double accuracy)
            {
                Accuracy=accuracy;
                Library=nameLibrary;
            }

            public override readonly string ToString() => IsSuccess ? $"Результат оценки модели из {Library}: 🎯 Точность: {Accuracy}! " : $"Ой! 💥 Произошла ошибка: {Exception!.Message} ";
        }
    }
}
