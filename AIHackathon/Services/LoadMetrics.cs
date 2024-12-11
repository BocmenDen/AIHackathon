using AIHackathon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneBot.Attributes;
using OneBot.Models;
using System.Diagnostics;

namespace AIHackathon.Services
{
    [Service(Type = ServiceType.Scoped)]
    public class LoadMetrics(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<LoadMetrics> logger)
    {
        public const string KeyDirectory = "modelsPathStorage";
        public const string KeyPathPython = "pythonPathExe";
        public const string KeyPathScript = "pythonPathScript";
        public const string KeyPathDBModel = "pythonPathDBModel";
        public const string KeyDBTarget = "pythonDBTarget";

        private readonly string _pathPython = configuration[KeyPathPython] ?? throw new Exception("Отсутствуют данные о расположении Python");
        private readonly string _pathScript = configuration[KeyPathScript] ?? throw new Exception("Отсутствуют данные о расположении скрипта");
        private readonly string _pathDBModel = configuration[KeyPathDBModel] ?? throw new Exception("Отсутствуют данные о расположении модели");
        private readonly string _pathDBTarget = configuration[KeyDBTarget] ?? throw new Exception("Отсутствуют данные о целевом столбце");

        private readonly string _directoryFiles = configuration[KeyDirectory] ?? throw new Exception("Нед данных о расположении хранилища моделей");
        private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

        private static int currentProcessing = 0;
        public static int CurrentProcessing => currentProcessing;

        public async Task Run(ReceptionClient<User> updateData)
        {
            Interlocked.Increment(ref currentProcessing);
            try
            {
                SendingClient sendingClient = [];
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
                metric.UserId = updateData.User.Id;
                metric.PathFile = subPath;
                sendingClient.Message = "Сохраняю результаты... 💾 Почти готово! ⏳";
                await updateData.Send(sendingClient);
                using var db = _serviceProvider.GetRequiredService<DataBase>();
                db.Metrics.Add(metric);
                await db.SaveChangesAsync();
                sendingClient.Message = metric.ToString();
                await updateData.Send(sendingClient);
                logger.LogInformation("Результаты оценивания модели пользователя [{user}]: {result}", updateData.User, metric);
            }
            catch(Exception e)
            {
                await updateData.Send("Произошла неизвестная ошибка при обработке вашей модели");
                logger.LogError(e, "Произошла ошибка при обработке модели");
            }
            finally
            {
                Interlocked.Decrement(ref currentProcessing);
            }
        }

        private async Task<MetricsUser> Load(string pathModel)
        {
            using Process process = new();
            process.StartInfo.FileName = _pathPython;
            process.StartInfo.Environment["TF_ENABLE_ONEDNN_OPTS"] = "0";
            process.StartInfo.Arguments = $"\"{_pathScript}\" {pathModel} {_pathDBModel} {_pathDBTarget}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            static MetricsUser createError(string error)
            {
                return new MetricsUser()
                {
                    Error = error
                };
            }

            try
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                string error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                //if (string.IsNullOrWhiteSpace(output) && !string.IsNullOrEmpty(error))
                //    return createError(error);

                var metric = JsonConvert.DeserializeObject<MetricsUser>(output);
                if (metric == null)
                    return createError("Не удалось распарсить ответ от скрипта получения метрик");
                return metric;
            }
            catch (Exception ex)
            {
                return createError(ex.Message);
            }
        }
    }
}
