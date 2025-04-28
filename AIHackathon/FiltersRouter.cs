#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CS8321  // Локальная функция объявлена, но не используется
#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены

using AIHackathon.Attributes;
using AIHackathon.DB;
using AIHackathon.Extensions;
using AIHackathon.Models;
using AIHackathon.Pages;
using AIHackathon.Services;
using BotCore.Services;
using BotCore.Tg;
using Microsoft.Extensions.Options;

namespace AIHackathon
{
    public static class FiltersRouter
    {
        [CommandFilter(true, "sendMeInfo")]
        [IsRegisterFilter]
        private static Task SendMeInfo(UpdateContext context) => context.Reply(context.User.GetInfoUser());

        [CommandFilter(true, "keyboard")]
        [IsRegisterFilter]
        public static Task SendMainKeyboard(UpdateContext context)
        {
            return context.Reply(new SendModel()
            {
                Message = "Выдаю вам клавиатуру",
                Keyboard = ConstsShared.ButtonsMain
            });
        }

        [ButtonsFilter(ConstsShared.ResourceButtonsMain)]
        [IsRegisterFilter]
        private static Task HandleMainButtons(UpdateContext context, ButtonSearch? buttonSearch, PageRouterHelper pageRouter, IOptions<Settings> options)
        {
            var button = buttonSearch!.Value.Button;
            if (button == ConstsShared.ButtonOpenMainPage)
                return pageRouter.Navigate(context, MainPage.Key);
            if (button == ConstsShared.ButtonOpenRatingPage)
                return pageRouter.Navigate(context, RatingPage.Key);
            if (button == ConstsShared.ButtonOpenNews)
                return GetNewsInfo(context, options.Value);
            return pageRouter.Navigate(context, PageInfo.Key);
        }

        private readonly static ButtonsSend ButtonsHandleMediaFile = new([[ConstsShared.ButtonOpenInfo]]);
        [IsRegisterFilter]
        [MessageTypeFilter(UpdateType.Media)]
        private static async Task<bool> HandleMediaFile(UpdateContext context, PageRouterHelper pageRouter, IOptions<Settings> options)
        {
            if (context.User.KeyPage == HandleMediaPage.Key) return false;
            if (!options.Value.ValidTypesHandleMediaFile.Contains(context.Update.Medias![0].Type))
                await context.Reply(new SendModel()
                {
                    Message =$"Тип отправленного файла на оценивавшие модели не поддерживается, узнать об поддерживаемых библиотеках и форматах моделей можно в разделе \"{ConstsShared.ButtonOpenInfo}\"",
                    Medias = [ConstsShared.MediaError],
                    Inline = ButtonsHandleMediaFile
                });
            else await pageRouter.Navigate(context, HandleMediaPage.Key);
            return true;
        }

        private readonly static MediaSource MediaGetNewsInfo = MediaSource.FromUri("https://challenge.braim.org/storage/341/challenge/558/avatar/61b1e7c4-1081-11f0-99e2-8d5603df9e3c.jpg");
        private static Task GetNewsInfo(UpdateContext context, Settings settings)
        {
            return context.Reply(new SendModel()
            {
                Message = $"Хотите быть в курсе всех самых актуальных новостей? Подписывайтесь на наш канал, чтобы не пропустить важную информацию!\r\n\r\n📲 [Перейти на канал]({settings.LinkNewsGroup})\r\n\r\nБудьте всегда на шаг впереди! 📰",
                Medias = [MediaGetNewsInfo]
            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
        }

#if DEBUGTEST
        private readonly static MediaSource MediaExitHandle = MediaSource.FromUri("https://media1.tenor.com/m/wLCaGpXM7VgAAAAC/exit-exit-pepe.gif");
        [CommandFilter(true, "exit")]
        [IsRegisterFilter]
        private static async Task ExitHandle(UpdateContext context, ConditionalPooledObjectProvider<DataBase> dbP)
        {
            await context.Reply(new()
            {
                Message = $"Выполнен выход из аккаунта:\n{context.User.GetInfoUser()}",
                Medias = [MediaExitHandle]
            });
            await dbP.TakeObjectAsync(x =>
            {
                x.RemoveUser(context.User);
                return x.SaveChangesAsync();
            });
        }
#endif
    }
}
