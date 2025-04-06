using AIHackathon.Base;
using AIHackathon.DB;
using BotCore.Interfaces;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public class RegisterStartPage(HandlePageRouter pageRouter) : PageBase
    {
        public const string Key = "RegisterStartPage";
        public readonly static ButtonSend ButtonBackRegisterMain = "⬅️ Открыть регистрацию";
        public readonly static ButtonsSend ButtonsBackRegisterMain = new([[ButtonBackRegisterMain]]);

        private const string SetValueEmodji = "🎯 Set: ";
        private const string EditValueEmodji = "✏️ Edit: ";
        private readonly static KeyValuePair<string, (string text, string keyPage)>[] _buttons =
            [
                new(nameof(User.Surname), ("Фамилия", SetSurnamePage.Key)),
                new(nameof(User.Email), ("Email", SetEmailPage.Key)),
                new(nameof(User.PhoneNumber), ("Номер телефона", SetPhonePage.Key))
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

        private ButtonsSend lastSendButtons = null!;

        public override Task HandleNewUpdateContext(UpdateContext context)
        {
            var buttonsSearch = context.BotFunctions.GetIndexButton(context.Update, lastSendButtons);
            if (buttonsSearch != null)
                return HandleButtons(context, buttonsSearch.Value);
            return context.Reply(new SendModel()
            {
                Message = Message,
                Inline = lastSendButtons,
                Medias = [MediaSource.FromUri("https://i.pinimg.com/originals/64/a3/1f/64a31fd8fadce2b2b74ba517bbc24485.gif")]
            });
        }

        public override Task OnNavigate(IUpdateContext<User> context)
        {
            UpdateButtonsProperty(context.User);
            return base.OnNavigate(context);
        }

        private Task HandleButtons(UpdateContext context, ButtonSearch buttonSearch)
        {
            if (buttonSearch.Row < _buttons.Length)
                return pageRouter.Navigate(context, _buttons[buttonSearch.Row].Value.keyPage);
            return pageRouter.Navigate(context, MainPage.Key);
        }

        private void UpdateButtonsProperty(User user)
        {
            List<ButtonSend> buttons = [];
            foreach (var nodeButton in _buttons)
            {
                var property = typeof(User).GetProperty(nodeButton.Key);
                var value = property?.GetValue(user);
                if (value == null)
                {
                    buttons.Add(SetValueEmodji + nodeButton.Value.text);
                    continue;
                }
                buttons.Add(EditValueEmodji + value.ToString());
            }
            lastSendButtons = new(buttons.Select(x => Enumerable.Empty<ButtonSend>().Append(x)));
        }
    }
}
