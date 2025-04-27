using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.DB.Models;
using AIHackathon.Models;
using AIHackathon.Services;
using AIHackathon.Utils;
using BotCore.Interfaces;
using BotCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    [GenerateModelMirror]
    public partial class HandleMediaPage(
        FilesStorage storage,
        IOptions<Settings> options,
        ConditionalPooledObjectProvider<DataBase> db,
        PageRouterHelper pageRouter,
        FilesArchive filesArchive,
        DockerGenerateOutput dockerGenerateOutput,
        LayerOldEditMessage<User, UpdateContext> oldEdit,
        LoadMetrics loadMetrics) : PageBaseClearCache
    {
        public const string Key = "HandleMediaPage";

        private const string PathUserFiles = "UserFiles";

        private readonly static MediaSource MediaEnd = MediaSource.FromUri("https://media.tenor.com/flGNpobJuuoAAAAi/happy-clap.gif");
        private readonly static MediaSource MediaLimitError = MediaSource.FromUri("https://media.tenor.com/yn7b5sHWez0AAAAi/dislike-no.gif");

        private readonly static ButtonsSend ButtonsReset = new([["Начать отправку новой модели"]]);

        protected partial string? FileType { get; set; }
        protected partial int CountParts { get; set; }
        protected partial byte[] InputFileHash { get; set; }
        protected partial ConcurrentDictionary<int, PartInfo> PartFiles { get; set; }

        protected partial string EndStateMessage { get; set; }
        protected partial EndStatesTypes EndStates { get; set; }

        public override Task HandleNewUpdateContext(UpdateContext context)
            => HandleNewUpdateContext(context, false);
        public override Task OnNavigate(UpdateContext context) => HandleNewUpdateContext(context, true);

        private async Task HandleNewUpdateContext(UpdateContext context, bool isNavigate)
        {
            var serachButtons = context.BotFunctions.GetIndexButton(context.Update, ButtonsReset);
            if ((!isNavigate && serachButtons != null) ||
                (!isNavigate && EndStates != EndStatesTypes.None && context.Update.UpdateType.HasFlag(UpdateType.Media)))
            {
                await pageRouter.Navigate(context, Key);
                return;
            }
            if (await IsSendEndStateMessage(context)) return;
            if (await CheckLimit(context)) return;
            if (await LoadMedias(context)) return;
            string pathFile = await CombineFiles(context);
            await using var archive = await filesArchive.Open(pathFile);
            string? outputPath = await GenerateOutput(context, pathFile, archive);
            if (outputPath == null) return;
            await TestModel(context, pathFile, outputPath);
        }

        private async Task<bool> IsSendEndStateMessage(UpdateContext context)
        {
            switch (EndStates)
            {
                case EndStatesTypes.Limit:
                    await context.Reply(new SendModel()
                    {
                        Message = "🚫 Ваша команда больше не может отправлять модели на оценивание — достигнут лимит попыток",
                        Medias = [MediaLimitError]
                    });
                    return true;
                case EndStatesTypes.Result:
                    await context.Reply(new SendModel()
                    {
                        Message = EndStateMessage,
                        Medias = [MediaEnd]
                    });
                    return true;
                case EndStatesTypes.ResultError:
                    await context.Reply(new SendModel()
                    {
                        Message = EndStateMessage,
                        Medias = [ConstsShared.MediaError]
                    });
                    return true;
                default:
                    return false;
            }
        }
        private async Task<bool> CheckLimit(UpdateContext context)
        {
            var isLimit = await db.TakeObjectAsync(x => x.Metrics.Include(x => x.Participant).Where(x => x.Participant!.CommandId == context.User.Participant!.CommandId && x.Error != null && x.Error != "").CountAsync()) > options.Value.MaxCountMetricsCommand;
            if (!isLimit) return false;
            EndStates = EndStatesTypes.Limit;
            await Model.Save();
            _ = await IsSendEndStateMessage(context);
            return true;
        }
        private async Task<bool> LoadMedias(UpdateContext context)
        {
            StringBuilder stringBuilder = new();
            if (context.Update.UpdateType.HasFlag(UpdateType.Media))
            {
                FileType ??= context.Update.Medias![0].Type;
                PartFiles ??= new();
                int oldCountFiles = PartFiles.Count;
                _ = await context.WaitStep(LoadMedias(context, stringBuilder), () => $"Скачано {PartFiles.Count - oldCountFiles} из {context.Update.Medias!.Count} файлов");
                await Model.Save();
            }
            if (CountParts == 0)
            {
                await context.Reply(new()
                {
                    Message = "📦 Загрузите все части обученной модели по отдельности или группой медиафайлов.",
                });
                return true;
            }
            if (CountParts != PartFiles.Count)
            {
                stringBuilder.Insert(0, @$"Ожидается загрузка всех файлов.
├> загружено {PartFiles.Count} из {CountParts}
└> расширение: {FileType}

");
                foreach (var file in PartFiles)
                {
                    stringBuilder.AppendLine($"Загружен: {file.Value.Name}.{FileType}");
                    stringBuilder.AppendLine($"└> часть {file.Key}");
                    stringBuilder.AppendLine();
                }

                await context.Reply(new()
                {
                    Message = stringBuilder.ToString(),
                    Inline = ButtonsReset
                });
                return true;
            }
            return false;

            async Task<bool> LoadMedias(IUpdateContext<User> context, StringBuilder stringBuilder)
            {
                object lockObj = new();
                await Task.WhenAll(context.Update.Medias!.Select(LoadMedia(stringBuilder, lockObj)));
                await SaveStorage();
                return true;

                Func<MediaSource, Task> LoadMedia(StringBuilder stringBuilder, object lockObj)
                {
                    return async mediaSource =>
                    {
                        using var fileMedia = await mediaSource.GetStream();
                        byte[] bufferHash = new byte[32];
                        await fileMedia.ReadExactlyAsync(bufferHash);
                        int countParts = fileMedia.ReadByte();
                        int pos = fileMedia.ReadByte();

                        using var fileTemp = await storage.CreateTempFile(mediaSource.Type!);

                        void AddLog(string message)
                        {
                            lock (lockObj)
                            {
                                stringBuilder.AppendLine($"Файл {mediaSource.Name}.{mediaSource.Type}: {message}");
                                stringBuilder.AppendLine($"└>Позиция: {pos}");

                                stringBuilder.AppendLine();
                            }
                        }

                        if (PartFiles == null || PartFiles.IsEmpty)
                        {
                            PartFiles = [];
                            InputFileHash = bufferHash;
                            CountParts = countParts;
                        }
                        if (FileType != mediaSource.Type)
                        {
                            AddLog($"Не загружен  т.к имеет расширение отличное от {FileType}");
                            return;
                        }
                        if (!bufferHash.SequenceEqual(InputFileHash))
                        {
                            AddLog($"Не загружен т.к данный фрагмент принадлежит другому файлу");
                            return;
                        }
                        if (pos >= CountParts)
                        {
                            AddLog($"Не загружен т.к данный фрагмент имеет неверный номер");
                            return;
                        }
                        await fileMedia.CopyToAsync(fileTemp.Stream);
                        PartFiles[pos] = new PartInfo()
                        {
                            Path = fileTemp.Path,
                            Name = mediaSource.Name!
                        };
                    };
                }
            }
        }
        private async Task<string> CombineFiles(UpdateContext context)
        {
            var fileName = $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}.{FileType}";
            var filePath = Path.Combine(PathUserFiles, Path.Combine(context.User.ParticipantId.ToString()!, fileName));
            using var fileStream = await storage.CreateFile(filePath);

            foreach (var (_, fileInfo) in PartFiles.OrderBy(x => x.Key))
            {
                var filePart = await storage.OpenReadFile(fileInfo.Path);
                await filePart.CopyToAsync(fileStream);
                await filePart.DisposeAsync();
                await storage.DeleteFile(fileInfo.Path);
            }
            return filePath;
        }
        private async Task<string?> GenerateOutput(UpdateContext context, string filePath, Archive archive)
        {
            var resultTesting = await context.WaitStep(dockerGenerateOutput.Testing(archive), () => "генерация выходных данных");
            if (resultTesting.IsError)
            {
                MetricParticipant metricResult = new()
                {
                    PathFile = filePath,
                    Error = resultTesting.Error,
                    Accuracy = 0,
                    DateTime = DateTime.Now,
                    ParticipantId = context.User.Participant!.Id
                };
                await db.TakeObjectAsync(x =>
                {
                    x.Metrics.Add(metricResult);
                    return x.SaveChangesAsync();
                });
                EndStateMessage = @$"Произошла ошибка при оценивание вашей модели: {resultTesting.Error}

Id записи: {metricResult.Id}";
                EndStates = EndStatesTypes.ResultError;
                await Model.Save();
                _ = await IsSendEndStateMessage(context);
                return null;
            }
            return resultTesting.PathOutput!;
        }
        private async Task TestModel(UpdateContext context, string filePath, string pathOutput)
        {
            var resultTesting = await context.WaitStep(loadMetrics.Load(pathOutput), () => "тестирование модели");
            MetricParticipant metricResult = new()
            {
                PathFile = filePath,
                Error = resultTesting.Error,
                Accuracy = resultTesting.Accuracy,
                DateTime = DateTime.Now,
                ParticipantId = context.User.Participant!.Id
            };
            await db.TakeObjectAsync(x =>
            {
                x.Metrics.Add(metricResult);
                return x.SaveChangesAsync();
            });
            EndStates = resultTesting.IsError ? EndStatesTypes.ResultError : EndStatesTypes.Result;
            if (resultTesting.IsError)
            {
                EndStateMessage = @$"Произошла ошибка при оценивании вашей модели: {resultTesting.Error}

Id записи: {metricResult.Id}";
            }
            else
            {
                EndStateMessage = @$"
Модель успешно оценена!
Результаты:
└> Метрика: {resultTesting.Accuracy}";
            }
            await Model.Save();
            _ = await IsSendEndStateMessage(context);
            oldEdit.StopEditLastMessage(context.User.Id);
        }

        public struct PartInfo
        {
            public string Path;
            public string Name;
        }
        public enum EndStatesTypes
        {
            None,
            Limit,
            Result,
            ResultError
        }
    }
}