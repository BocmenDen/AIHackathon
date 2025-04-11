using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using AIHackathon.Models;
using AIHackathon.Services;
using BotCore.Interfaces;
using BotCore.Services;
using BotCoreGenerator.PageRouter.Mirror;
using Microsoft.Extensions.Options;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    [GenerateModelMirror]
    public partial class HandleMediaPage(FilesStorage storage, FileCheckPlagiat fileCheckPlagiarist, TestingModel testModel, IOptions<Settings> options, ConditionalPooledObjectProvider<DataBase> db) : PageBase
    {
        public const string Key = "HandleMediaPage";

        private readonly static MediaSource MediaPlagiat = MediaSource.FromUri("https://media1.tenor.com/m/oCJsYj0GcEkAAAAC/copy-paste.gif");
        private readonly static MediaSource MediaMessageState = MediaSource.FromUri("https://media1.tenor.com/m/_28Wpe-HrfIAAAAC/nervous-spongebob.gif");
        private readonly static MediaSource MediaEnd = MediaSource.FromUri("https://media.tenor.com/flGNpobJuuoAAAAi/happy-clap.gif");

        protected partial string? PathFile { get; set; }
        protected partial string? TypeFile { get; set; }
        protected partial int? PlagiatId { get; set; }
        protected partial string? PlagiatCommand { get; set; }
        protected partial string? FileHash { get; set; }
        protected partial bool IsPlagiatMyCommand { get; set; }

        public override async Task HandleNewUpdateContext(UpdateContext context)
        {
            if (PlagiatId != null || IsPlagiatMyCommand)
            {
                await SendPlagiatInfo(context);
                return;
            }
            if (!await WaitStep(IsLoadMediaStep(context), context, "скачивание файла"))
            {
                await context.ReplyBug(
@"Не удалось обнаружить отправляемый файл, возможные причины:
├> файл слишком большой
├> файл не был отправлен
└> во время скачивания вашей модели из Tg чат-бот был перезагружен

Попробуйте ещё раз отправить файл, если ошибка повториться сообщите об этом.");
                return;
            }
            if (await WaitStep(IsPlagiatStep(context), context, "проверка на плагиат"))
            {
                await SendPlagiatInfo(context);
                return;
            }
            var resultTesting = await WaitStep(testModel.Testing(PathFile!, TypeFile!), context, "оценивание модели");
            MetricParticipant metricResult = new()
            {
                PathFile = PathFile!,
                FileType = TypeFile,
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
            if (resultTesting.IsError)
            {
                await context.Reply(new SendModel()
                {
                    Message = @$"Произошла ошибка при оценивании вашей модели: {resultTesting.Error}",
                    Medias = [ConstsShared.MediaError]
                });
                return;
            }
            await context.Reply(new SendModel()
            {
                Message = @$"
Модель успешно оценена!
Результаты:
├> Id: {metricResult.Id}
├> Библиотека: {metricResult.Library}
└> Метрика: {metricResult.Accuracy}",
                Medias = [MediaEnd]
            });
        }

        private static async Task<T> WaitStep<T>(Task<T> task, UpdateContext context, string nameStep)
        {
            string waitLine = "";
            while (!task.IsCompleted)
            {
                await context.Reply(new SendModel()
                {
                    Message = $@"
Процесс оценки вашей модели запущен. Пожалуйста, не выполняйте никаких действий, пока он не завершится.
└> {nameStep} {waitLine}

Если сообщение перестанет обновляться, возможно, произошла перезагрузка бота. В таком случае попробуйте повторно отправить файл или сообщите об этом разработчику: @BocmenDen.",
                    Medias = [MediaMessageState]
                });
                waitLine += ".";
                await Task.Delay(1000);
            }
            return await task;
        }

        private async Task<bool> IsPlagiatStep(UpdateContext context)
        {
            var resultPlagiat = await fileCheckPlagiarist.CheckPlagiat(PathFile!, TypeFile);
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
                await storage.DeleteFile(PathFile!);
                PathFile = null;
                await Model.Save();
                return true;
            }
            return false;
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

        private async Task<bool> IsLoadMediaStep(UpdateContext context)
        {
            if (!string.IsNullOrWhiteSpace(PathFile)) return true;
            if (context.Update.Medias == null || context.Update.Medias.Count == 0) return false;
            var mediaSource = context.Update.Medias![0];
            var fileName = $"{DateTime.Now:dd.mm.yyyy_hh.mm.ss}_{mediaSource.Name!}";
            var filePath = Path.Combine(options.Value.PathUserFiles, fileName);
            using var fileStream = await storage.CreateFile(filePath);
            using var fileMedia = await mediaSource.GetStream();
            await fileMedia.CopyToAsync(fileStream);
            PathFile = filePath;
            TypeFile = mediaSource.Type!;
            await Model.Save();
            return true;
        }

        protected override Task OnExit(IUpdateContext<User> context)
        {
            PlagiatId = null;
            PlagiatCommand = null;
            FileHash = null;
            PathFile = null;
            TypeFile = null;
            IsPlagiatMyCommand = false;
            return Model.Save();
        }
    }
}
