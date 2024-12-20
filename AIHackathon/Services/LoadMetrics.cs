﻿using AIHackathon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using OneBot.Attributes;
using OneBot.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace AIHackathon.Services
{
    [Service(Type = ServiceType.Scoped)]
    public class LoadMetrics(IConfiguration configuration, IServiceProvider serviceProvider, ILogger<LoadMetrics> logger)
    {
        private readonly static Regex _splitArgsRegex = new(@"[A-Za-z0-9_]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

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

        public async Task Run(UpdateContext<User> context)
        {
            Interlocked.Increment(ref currentProcessing);
            try
            {
                SendModel sendingClient = [];
                MediaSource mediaSource = context.Update.Medias![0];
                string subPath = Path.Combine(context.User.Id.ToString().Replace('-', '_'), $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}_{mediaSource.Name!}");
                string pathFile = Path.Combine(_directoryFiles, subPath);

                sendingClient.Message = "Загрузка началась! ⬇️ Подождите немного... ⏳";
                await context.Send(sendingClient);

                using (var stream = await mediaSource.GetStream())
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(pathFile)!);
                    var writer = File.OpenWrite(pathFile);
                    await stream.CopyToAsync(writer);
                    writer.Close();
                    writer.Dispose();
                }

                sendingClient.Message = "Погоди немного! ⏳ Сейчас я оцениваю модель... 🤔 Результаты будут скоро! \n✨";
                await context.Send(sendingClient);
                var metricTask = Load(pathFile, context.Update.Message);
                while (!metricTask.IsCompleted)
                {
                    sendingClient.Message += "✨";
                    await context.Send(sendingClient);
                    Thread.Sleep(500);
                }
                var metric = await metricTask;
                metric.UserId = context.User.Id;
                metric.PathFile = subPath;
                sendingClient.Message = "Сохраняю результаты... 💾 Почти готово! ⏳";
                await context.Send(sendingClient);
                using var db = _serviceProvider.GetRequiredService<DataBase>();
                db.Metrics.Add(metric);
                await db.SaveChangesAsync();
                sendingClient.Message = metric.ToString();
                await context.Send(sendingClient);
                logger.LogInformation("Результаты оценивания модели пользователя [{user}]: {result}", context.User, metric);
            }
            catch(Exception e)
            {
                await context.Send("Произошла неизвестная ошибка при обработке вашей модели");
                logger.LogError(e, "Произошла ошибка при обработке модели");
            }
            finally
            {
                Interlocked.Decrement(ref currentProcessing);
            }
        }

        private async Task<MetricsUser> Load(string pathModel, string? argsLine)
        {
            if (!string.IsNullOrWhiteSpace(argsLine))
            {
                var args = _splitArgsRegex.Matches(argsLine!).Where(x => x.Success).OrderBy(x => x.Index).Select(x => x.Value.Trim());
                argsLine = string.Join(' ', args);
            }
            argsLine ??= string.Empty;
            using Process process = new();
            process.StartInfo.FileName = _pathPython;
            process.StartInfo.Environment["TF_ENABLE_ONEDNN_OPTS"] = "0";
            process.StartInfo.Arguments = $"\"{_pathScript}\" {pathModel} {_pathDBModel} {_pathDBTarget} {argsLine}";
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
