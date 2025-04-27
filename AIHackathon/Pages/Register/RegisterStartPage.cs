using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.DB.Models;
using AIHackathon.Services;
using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;
using BotCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public class RegisterStartPage(ConditionalPooledObjectProvider<DataBase> db, PageRouterHelper pageRouter) : PageBaseClearCache, IBindStorageModel<SharedRegisterModel>
    {
        public const string Key = "RegisterStartPage";
        public readonly static ButtonSend ButtonBackRegisterMain = "⬅️ Открыть регистрацию";
        public readonly static ButtonsSend ButtonsBackRegisterMain = new([[ButtonBackRegisterMain]]);

        private readonly static ButtonSend ButtonAutorization = "🔓 Авторизоваться";
        private readonly static MediaSource Media = MediaSource.FromUri("https://i.pinimg.com/originals/64/a3/1f/64a31fd8fadce2b2b74ba517bbc24485.gif");
        private const string SetValueEmodji = "🎯 Set: ";
        private const string EditValueEmodji = "✏️ Edit: ";
        private readonly static KeyValuePair<string, (string text, string keyPage)>[] _buttons =
            [
                new(nameof(SharedRegisterModel.Surname), ("Фамилия", SetSurnamePage.Key)),
                new(nameof(SharedRegisterModel.Email), ("Email", SetEmailPage.Key)),
                new(nameof(SharedRegisterModel.Phone), ("Номер телефона", SetPhonePage.Key))
            ];
        private const string Message =
"""
Перед тем как приступить к работе, вам нужно пройти авторизацию. Это необходимо чтобы найти вас на платформе Braim.

📌 Какие данные нужны для авторизации:
1️⃣ Фамилия
2️⃣ Email
3️⃣ Номер телефона

⚠️ Указывайте данные в точности как на платформе Braim, иначе вы не сможете пройти авторизацию.
""";
        private ButtonsSend _lastSendButtons = null!;
        private Participant? _participant = null;

        private SharedRegisterModel _model = null!;

        public void BindStorageModel(StorageModel<SharedRegisterModel> model) => _model = model.Value;

        public override async Task HandleNewUpdateContext(UpdateContext context)
        {
            if (_lastSendButtons == null)
                await LoadState();
            var buttonsSearch = context.BotFunctions.GetIndexButton(context.Update, _lastSendButtons!);
            if (buttonsSearch != null)
            {
                await HandleButtons(context, buttonsSearch.Value);
                return;
            }
            await context.Reply(new SendModel()
            {
                Message = Message,
                Inline = _lastSendButtons,
                Medias = [Media]
            });
        }

        public override async Task OnNavigate(IUpdateContext<User> context)
        {
            await LoadState();
            await base.OnNavigate(context);
        }

        private async Task HandleButtons(UpdateContext context, ButtonSearch buttonSearch)
        {
            if (buttonSearch.Row < _buttons.Length)
            {
                await pageRouter.Navigate(context, _buttons[buttonSearch.Row].Value.keyPage);
                return;
            }
            context.User.ParticipantId = _participant!.Id;
            context.User.Participant = _participant;
#if DEBUGTEST
            await db.TakeObject(db =>
            {
                db.Metrics.Add(new MetricParticipant()
                {
                    ParticipantId = context.User.ParticipantId!.Value,
                    Accuracy = 0.1,
                    DateTime = DateTime.Now.AddMinutes(-1),
                    PathFile = "Test.txt"
                });
                return db.SaveChangesAsync();
            });
#endif
            await db.TakeObjectAsync(x => { x.Users.Update(context.User); return x.SaveChangesAsync(); });
            await FiltersRouter.SendMainKeyboard(context);
            await pageRouter.Navigate(context, MainPage.Key);
        }

        private async Task LoadState()
        {
            List<ButtonSend> buttons = [];
            bool isFull = true;
            foreach (var nodeButton in _buttons)
            {
                var property = typeof(SharedRegisterModel).GetProperty(nodeButton.Key);
                var value = property?.GetValue(_model);
                if (value == null)
                {
                    isFull = false;
                    buttons.Add(SetValueEmodji + nodeButton.Value.text);
                    continue;
                }
                buttons.Add(EditValueEmodji + value.ToString());
            }
            if (isFull)
            {
                _participant = await db.TakeObjectAsync(x =>
                {
                    return x.Participants.Include(x => x.Command).FirstOrDefaultAsync(m => m.Surname == _model.Surname && m.Email == _model.Email && m.Phone == _model.Phone);
                });
                if (_participant != null)
                    buttons.Add(ButtonAutorization);
            }
            _lastSendButtons = new(buttons.Select(x => Enumerable.Empty<ButtonSend>().Append(x)));
        }
    }
}
