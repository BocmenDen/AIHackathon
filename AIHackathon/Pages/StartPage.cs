using AIHackathon.Base;
using AIHackathon.Pages.Register;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    public class StartPage(HandlePageRouter pageRouter) : PageBase
    {
        public const string Key = "StartPage";

        private const string StartMessage = "Я — ваш оценочный бот, и моя задача заключается в том, чтобы оценить, как хорошо обучены ваши модели и насколько точно они предсказывают значения.";
        private readonly static ButtonsSend NextButtons = new([["Далее"]]);

        public override Task HandleNewUpdateContext(UpdateContext context)
        {
            if (context.BotFunctions.GetIndexButton(context.Update, NextButtons) != null)
                return pageRouter.Navigate(context, RegisterStartPage.Key);
            return context.Reply(new SendModel()
            {
                Message = StartMessage,
                Inline = NextButtons
            });
        }
    }
}
