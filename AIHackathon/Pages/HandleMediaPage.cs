using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using AIHackathon.Models;
using AIHackathon.Services;
using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using BotCore.Services;
using BotCoreGenerator.PageRouter.Mirror;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    [GenerateModelMirror]
    public partial class HandleMediaPage(FilesStorage storage, FileCheckPlagiat fileCheckPlagiarist, TestingModel testModel, IOptions<Settings> options, ConditionalPooledObjectProvider<DataBase> db, IMemoryCache memoryCache, PageRouterHelper pageRouter) : PageBase, IGetCacheOptions
    {
        public const string Key = "HandleMediaPage";

        private const string TmpStorage = nameof(HandleMediaPage);

        private readonly static MediaSource MediaPlagiat = MediaSource.FromUri("https://media1.tenor.com/m/oCJsYj0GcEkAAAAC/copy-paste.gif");
        private readonly static MediaSource MediaMessageState = MediaSource.FromUri("https://media1.tenor.com/m/_28Wpe-HrfIAAAAC/nervous-spongebob.gif");
        private readonly static MediaSource MediaEnd = MediaSource.FromUri("https://media.tenor.com/flGNpobJuuoAAAAi/happy-clap.gif");
        private readonly static MediaSource MediaLoadingFiles = MediaSource.FromUri("https://media1.tenor.com/m/JG9-sqzIMgQAAAAd/work-files-filing-cabinet.gif");

        private readonly static ButtonsSend ButtonsReset = new([["Начать отправку новой модели"]]);

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        protected partial string FilePath { get; set; }
        protected partial string? FileType { get; set; }
        protected partial int? PlagiatId { get; set; }
        protected partial string? PlagiatCommand { get; set; }
        protected partial string? FileHash { get; set; }
        protected partial bool IsPlagiatMyCommand { get; set; }
        protected partial TestModelResult? TestModel { get; set; }
        protected partial int IdMetric { get; set; }

        protected partial int CountParts { get; set; }
        protected partial byte[] InputFileHash { get; set; }
        protected partial ConcurrentDictionary<int, PartInfo> PartFiles { get; set; }

        public override Task HandleNewUpdateContext(UpdateContext context)
            => HandleNewUpdateContext(context, false);
        public override Task OnNavigate(IUpdateContext<User> context)
            => HandleNewUpdateContext(context, true);

        private async Task HandleNewUpdateContext(UpdateContext context, bool isNavigate)
        {
            var serachButtons = context.BotFunctions.GetIndexButton(context.Update, ButtonsReset);
            if((!isNavigate && serachButtons != null) || ((TestModel != null || IsPlagiatMyCommand) && context.Update.UpdateType.HasFlag(UpdateType.Media)))
            {
                await pageRouter.Navigate(context, Key);
                return;
            }
            if (IsPlagiatMyCommand)
            {
                await SendPlagiatInfo(context);
                return;
            }
            if (TestModel != null)
            {
                await SendTestModelInfo(context);
                return;
            }
            if (FilePath is null)
            {
                if (await LoadMedias(context)) return;
                if (await CombineFiles()) return;
            }
            if (await WaitStep(IsPlagiatStep(context), context, "проверка на плагиат"))
            {
                await SendPlagiatInfo(context);
                return;
            }
            _ = await WaitStep(TestModelStep(context), context, "тестирование модели");
            await SendTestModelInfo(context);
        }

        private async Task<bool> LoadMedias(UpdateContext context)
        {
            StringBuilder stringBuilder = new();
            if (context.Update.UpdateType.HasFlag(UpdateType.Media))
            {
                FileType ??= context.Update.Medias![0].Type;
                _ = await WaitStep(LoadMedias(context, stringBuilder), context, $"Выполняю скачивание {context.Update.Medias!.Count} файлов");
                await Model.Save();
            }
            if (CountParts == 0)
            {
                await context.Reply(new()
                {
                    Message = "📦 Загрузите все части обученной модели по отдельности или группой медиафайлов.",
                    Medias = [MediaLoadingFiles]
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
                    Inline = ButtonsReset,
                    Medias = [MediaLoadingFiles]
                });
                return true;
            }
            return false;

            async Task<bool> LoadMedias(IUpdateContext<User> context, StringBuilder stringBuilder)
            {
                object lockObj = new();
                await Task.WhenAll(context.Update.Medias!.Select(LoadMedia(stringBuilder, lockObj)));
                await Model.Save();
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
                        var fileName = $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}_{pos}_{mediaSource.Name!}.{mediaSource.Type}";
                        var filePath = Path.Combine(TmpStorage, fileName);
                        using var fileStream = await storage.CreateFile(filePath);

                        void AddLog(string message)
                        {
                            lock (lockObj)
                            {
                                stringBuilder.AppendLine($"Файл {mediaSource.Name}.{mediaSource.Type}: {message}");
                                stringBuilder.AppendLine($"└>Позиция: {pos}");

                                stringBuilder.AppendLine();
                            }
                        }

                        if (PartFiles == null)
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
                        await fileMedia.CopyToAsync(fileStream);
                        PartFiles[pos] = new PartInfo()
                        {
                            Path = filePath,
                            Name = mediaSource.Name!
                        };
                    };
                }
            }
        }
        private async Task<bool> CombineFiles()
        {
            if (FilePath is null && PartFiles.Count == CountParts && CountParts > 0)
            {
                var fileName = $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}.{FileType}";
                var filePath = Path.Combine(options.Value.PathUserFiles, fileName);
                using var fileStream = await storage.CreateFile(filePath);

                foreach (var (_, fileInfo) in PartFiles.OrderBy(x => x.Key))
                {
                    var filePart = await storage.OpenReadFile(fileInfo.Path);
                    await filePart.CopyToAsync(fileStream);
                    await filePart.DisposeAsync();
                    await storage.DeleteFile(fileInfo.Path);
                }
                FilePath = filePath;
                await Model.Save();
                return false;
            }
            return true;
        }
        private async Task<bool> IsPlagiatStep(UpdateContext context)
        {
            var resultPlagiat = await fileCheckPlagiarist.CheckPlagiat(FilePath!);
            FileHash = resultPlagiat.Hash;
            if (resultPlagiat.IsPlagiat)
            {
                if (resultPlagiat.PlagiatMetricParticipant!.Participant!.Command.Id == context.User.Participant!.Command.Id)
                {
                    IsPlagiatMyCommand = true;
                }
                else
                {
                    async Task<int> fSavePlagiatReport(DataBase x)
                    {
                        var plagiat = new Plagiat()
                        {
                            MetricPlagiatId = resultPlagiat.PlagiatMetricParticipant!.Id,
                            ParticipantId = context.User.Participant!.Id,
                        };
                        x.Plagiats.Add(plagiat);
                        await x.SaveChangesAsync();
                        return plagiat.Id;
                    }
                    PlagiatCommand = resultPlagiat.PlagiatMetricParticipant!.Participant!.Command.Name;
                    PlagiatId = await db.TakeObjectAsync(fSavePlagiatReport);
                }
                await storage.DeleteFile(FilePath!);
                return true;
            }
            return false;
        }
        private async Task<bool> TestModelStep(UpdateContext context)
        {
            var resultTesting = await testModel.Testing(FilePath!, FileType!);
            MetricParticipant metricResult = new()
            {
                PathFile = FilePath!,
                FileType = FileType,
                FileHash = FileHash!,
                Library = resultTesting.Library,
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
            IdMetric = metricResult.Id;
            TestModel = resultTesting;
            await Model.Save();
            return true;
        }
        private Task SendPlagiatInfo(UpdateContext context)
        {
            if (IsPlagiatMyCommand)
            {
                return context.Reply(new()
                {
                    Message = "Ваша команда уже отправляла данное решение",
                    Medias = [MediaPlagiat]
                });
            }
            return context.Reply(new SendModel()
            {
                Message = @$"Обнаружен плагиат! Отправляемое решение совпадает с ""{PlagiatCommand}""

Идентификатор записи о плагиате: {PlagiatId}

Если не согласны отправте данное сообщение: TG/VK @BocmenDen
Участник: {context.User.GetInfoUser()}
",
                Medias = [MediaPlagiat]
            });
        }
        private async Task SendTestModelInfo(UpdateContext context)
        {
            if (TestModel is not TestModelResult resultTesting) return;
            if (resultTesting.IsError)
            {
                await context.Reply(new SendModel()
                {
                    Message = @$"Произошла ошибка при оценивании вашей модели: {resultTesting.Error}

Id записи: {IdMetric}
",
                    Medias = [ConstsShared.MediaError]
                });
                return;
            }
            await context.Reply(new SendModel()
            {
                Message = @$"
Модель успешно оценена!
Результаты:
├> Библиотека: {resultTesting.Library}
└> Метрика: {resultTesting.Accuracy}",
                Medias = [MediaEnd]
            });
        }
        private async Task<T> WaitStep<T>(Task<T> task, UpdateContext context, string nameStep)
        {
            string waitLine = "";
            while (!task.IsCompleted)
            {
                await context.Reply(new SendModel()
                {
                    Message = $@"
Процесс оценки вашей модели запущен. Пожалуйста, не выполняйте никаких действий, пока он не завершится.
├> обновилось в: {DateTime.Now}
└> {nameStep} {waitLine}

Если сообщение перестанет обновляться, возможно, произошла перезагрузка бота. В таком случае попробуйте повторно отправить файлы или сообщите об этом разработчику: @BocmenDen.",
                    Medias = [MediaMessageState]
                });
                waitLine += "🐢";
                await Task.WhenAny(Task.Delay(options.Value.WaitUpdateMessageTestingModel), task);
            }
            return await task;
        }

        protected override async Task OnExit(IUpdateContext<User> context)
        {
            await db.TakeObjectAsync(x =>
            {
                context.User.ModelPage = null;
                x.Users.Update(context.User);
                return x.SaveChangesAsync();
            });
            await _cancellationTokenSource.CancelAsync();
        }

        public MemoryCacheEntryOptions GetCacheOptions()
        {
            var options = new MemoryCacheEntryOptions();
            PageCacheableAttribute pageCacheableAttribute = GetType().GetCustomAttribute<PageCacheableAttribute>()!;
            options.SlidingExpiration = pageCacheableAttribute.SlidingExpiration;
            options.AddExpirationToken(new CancellationChangeToken(_cancellationTokenSource.Token));
            options.RegisterPostEvictionCallback((object key, object? value, EvictionReason reason, object? state) =>
            {
                memoryCache.Remove(key);
                _cancellationTokenSource.Dispose();
            });
            return options;
        }

        public struct PartInfo
        {
            public string Path;
            public string Name;
        }
    }
}
