using AIHackathon.Base;
using AIHackathon.DB;
using AIHackathon.Extensions;
using BotCoreGenerator.PageRouter.Mirror;

namespace AIHackathon.Pages.Register
{
    [GenerateModelMirror]
    public abstract partial class SetValuePageBase(HandlePageRouter pageRouter) : PageBase
    {
        private static readonly ButtonsSend Buttons = new([[ConstsShared.ButtonYes], [ConstsShared.ButtonNo], [RegisterStartPage.ButtonBackRegisterMain]]);
        private readonly static ButtonsSend ButtonsBack = new([[RegisterStartPage.ButtonBackRegisterMain]]);
        private readonly static MediaSource MediaInput = MediaSource.FromUri("https://media1.tenor.com/m/5O48nhgNvjIAAAAC/typing-cat.gif");
        private readonly static MediaSource MediaIsOk = MediaSource.FromUri("https://media1.tenor.com/m/NpxX43CMKcsAAAAC/omni-man-omni-man-are-you-sure.gif");
        private readonly static MediaSource MediaError = MediaSource.FromUri("https://media.tenor.com/8ND8TbjZqh0AAAAi/error.gif");

        protected abstract string MessageStart { get; }
        protected abstract string MessageNotCorrect { get; }

        protected partial string Value { get; set; }

        public override Task HandleNewUpdateContext(UpdateContext context)
        {
            if (context.Update.UpdateType.HasFlag(UpdateType.Message))
                return HandleMessage(context);
            if (context.BotFunctions.GetIndexButton(context.Update, Buttons) is ButtonSearch buttonSearch)
                return HandleButtons(context, buttonSearch);
            return Task.CompletedTask;
        }

        private async Task HandleMessage(BotCore.Interfaces.IUpdateContext<User> context)
        {
            if (await IsNotCorrectValue(context, context.Update.Message!)) return;
            Value = context.Update.Message!;
            await context.Reply(new()
            {
                Message = $"Вы уверены что ввели [{Value}] верно?",
                Inline = Buttons,
                Medias = [MediaIsOk]
            });
        }

        private async Task HandleButtons(UpdateContext context, ButtonSearch buttonSearch)
        {
            if (buttonSearch.Button == ConstsShared.ButtonYes)
            {
                if (await IsNotCorrectValue(context, Value)) return;
                await SaveValue(context.User, Value);
                await pageRouter.Navigate(context, RegisterStartPage.Key);
                return;
            }
            if (buttonSearch.Button == ConstsShared.ButtonNo)
            {
                await OnNavigate(context);
                return;
            }
            if (buttonSearch.Button == RegisterStartPage.ButtonBackRegisterMain)
            {
                await pageRouter.Navigate(context, RegisterStartPage.Key);
                return;
            }
            await context.ReplyBug("Сработал метод нажатия на кнопку, но не был найден ни один из обработчиков");
        }

        private async Task<bool> IsNotCorrectValue(UpdateContext context, string value)
        {
            if (IsCorrectValue(value)) return false;
            await context.Reply(new SendModel()
            {
                Message = $"{MessageNotCorrect}\n\n{MessageStart}",
                Inline = ButtonsBack,
                Medias = [MediaError]
            });
            return true;
        }

        protected abstract bool IsCorrectValue(string value);
        protected abstract Task SaveValue(User user, string value);

        public override Task OnNavigate(UpdateContext context) => context.Reply(new SendModel()
        {
            Message = MessageStart,
            Inline = ButtonsBack,
            Medias = [MediaInput]
        });
    }
}
