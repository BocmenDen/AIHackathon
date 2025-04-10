using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.Models;
using BotCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    public class MainPage(ConditionalPooledObjectProvider<DataBase> dbObj, IOptions<Settings> settings) : PageBase
    {
        public const string Key = "MainPage";

        private readonly static MediaSource Media = MediaSource.FromUri("https://media1.tenor.com/m/izqQHTtWeEgAAAAC/apes-bored.gif");

        public override async Task HandleNewUpdateContext(UpdateContext context)
        {
            var db = dbObj.Get();
            var infoCommand = await db.GetCommandsRating().FirstOrDefaultAsync(x => x.SubjectId == context.User.Participant!.CommandId);
            var infoParticants = await db.GetParticipantsRating().Where(x => x.Subject.CommandId == context.User.Participant!.CommandId).ToListAsync();
            dbObj.Return(db);
            if (infoCommand == null) return;
            SendModel sendModel = new()
            {
                Inline = ConstsShared.ButtonsUpdate,
                Medias = [Media]
            };
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"Актуально на: {DateTime.Now}");
            stringBuilder.AppendLine(context.User.Participant!.Command.Name);
            stringBuilder.AppendLine($"├> рейтинг команды {infoCommand.Rating}");
            stringBuilder.AppendLine($"├> лучший результат {infoCommand.Metric}");
            stringBuilder.AppendLine($"└> использовано попыток {infoCommand.CountMetric} из {settings.Value.MaxCountMetricsCommand}");
            stringBuilder.AppendLine();

            foreach (var participantCommand in infoParticants)
            {
                stringBuilder.AppendLine($"{participantCommand.Subject.Surname} {participantCommand.Subject.Name} {participantCommand.Subject.MiddleName}");
                stringBuilder.AppendLine($"├> рейтинг участника {participantCommand.Rating}");
                stringBuilder.AppendLine($"├> лучший результат {participantCommand.Metric}");
                stringBuilder.AppendLine($"└> использовал {participantCommand.CountMetric} попыток");
                stringBuilder.AppendLine();
            }
            sendModel.Message = stringBuilder.ToString();
            await context.Reply(sendModel);
        }
    }
}
