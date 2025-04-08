using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.DB.Models;
using BotCore.Interfaces;
using BotCore.Services;
using BotCore.Tg;
using BotCoreGenerator.PageRouter.Mirror;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Text;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    public partial class RatingPage(ConditionalPooledObjectProvider<DataBase> db, IOptions<Settings> options) : PageBase
    {
        public const string Key = "RatingPage";
        private readonly static ButtonSend ButtonBack = "⬅️";
        private readonly static ButtonSend ButtonNext = "➡️";
        private readonly static ButtonsSend ButtonsCheck = new([[ButtonBack, ConstsShared.ButtonUpdate, ButtonNext]]);

        private readonly static Dictionary<char, string> _emodji = new()
        {
            { '0', "0️⃣" },
            { '1', "1️⃣" },
            { '2', "2️⃣" },
            { '3', "3️⃣" },
            { '4', "4️⃣" },
            { '5', "5️⃣" },
            { '6', "6️⃣" },
            { '7', "7️⃣" },
            { '8', "8️⃣" },
            { '9', "9️⃣" },
        };

        protected int _indexPage;

        public override async Task HandleNewUpdateContext(IUpdateContext<User> context)
        {
            var searchBtn = context.BotFunctions.GetIndexButton(context.Update, ButtonsCheck);
            if (searchBtn is ButtonSearch buttonSearch)
            {
                if (buttonSearch.Button == ButtonNext)
                    _indexPage++;
                else if (buttonSearch.Button == ButtonBack)
                    _indexPage--;
            }
            else if (int.TryParse(context.Update.Message, out int newIndexPage))
            {
                _indexPage = newIndexPage - 1;
            }
            var dbObj = db.Get();
            var countsCommand = await dbObj.Commands.CountAsync();
            var lastPage = countsCommand / options.Value.CountCommandsInPage;
            if (_indexPage > lastPage)
                _indexPage = lastPage;
            else if(_indexPage <  0)
                _indexPage = 0;
            var commandsRating = dbObj.GetCommandsRating().Skip(_indexPage * options.Value.CountCommandsInPage).Take(options.Value.CountCommandsInPage).AsAsyncEnumerable();
            db.Return(dbObj);
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"📅 Актуально на: {DateTime.Now}");
            stringBuilder.AppendLine($"🏁 Гонка за лидерством — {_indexPage + 1} страница");
            stringBuilder.AppendLine($"🔄 Для перехода введите номер страницы от 1 до {lastPage + 1}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"📋 Команды и их рейтинг:");
            stringBuilder.AppendLine($"────────────────────────");
            await foreach (var elem in commandsRating)
            {
                stringBuilder.AppendLine($"{NumberToEmodji(elem.Rating)} {(elem.SubjectId == context.User.Participant!.CommandId ? "🎯" : string.Empty)} {elem.Subject.Name}");
                stringBuilder.AppendLine($"└> {elem.Metric}");
            }
            bool isBack = _indexPage > 0;
            bool isNext = ((_indexPage + 1) * options.Value.CountCommandsInPage) < countsCommand;
            List<ButtonSend> sendButtons = [];
            if (isBack) sendButtons.Add(ButtonBack);
            sendButtons.Add(ConstsShared.ButtonUpdate);
            if (isNext) sendButtons.Add(ButtonNext);
            await context.Reply(new SendModel()
            {
                Message = stringBuilder.ToString(),
                Inline = new([sendButtons])
            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
        }

        public override async Task OnNavigate(IUpdateContext<User> context)
        {
            _indexPage = (await db.TakeObjectAsync(x => x.GetCommandsRating().FirstAsync(x => x.SubjectId == context.User.Participant!.CommandId))).Position - 1;
            _indexPage /= 10;
            await base.OnNavigate(context);
        }

        private static string NumberToEmodji(int value)
           => string.Join(string.Empty, value.ToString().Select(x => _emodji[x]));
    }
}
