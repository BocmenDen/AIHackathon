﻿using AIHackathon.Base;
using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using AIHackathon.Services;
using BotCore.PageRouter.Interfaces;
using BotCore.PageRouter.Models;

namespace AIHackathon.Pages.Register
{
    public abstract partial class SetValuePageBase : PageBaseClearCache, IBindStorageModel<SharedRegisterModel>, IBindService<PageRouterHelper>
    {
        private static readonly ButtonsSend Buttons = new([[ConstsShared.ButtonYes], [ConstsShared.ButtonNo], [RegisterStartPage.ButtonBackRegisterMain]]);
        private readonly static ButtonsSend ButtonsBack = new([[RegisterStartPage.ButtonBackRegisterMain]]);
        private readonly static MediaSource MediaInput = MediaSource.FromUri("https://media1.tenor.com/m/5O48nhgNvjIAAAAC/typing-cat.gif");
        private readonly static MediaSource MediaIsOk = MediaSource.FromUri("https://media1.tenor.com/m/NpxX43CMKcsAAAAC/omni-man-omni-man-are-you-sure.gif");

        private StorageModel<SharedRegisterModel> _storageModel = null!;
        private PageRouterHelper _pageRouter = null!;

        protected abstract string MessageStart { get; }
        protected abstract string MessageNotCorrect { get; }
        protected SharedRegisterModel RegisterModel => _storageModel.Value;

        public void BindStorageModel(StorageModel<SharedRegisterModel> model) => _storageModel = model;

        public override Task HandleNewUpdateContext(UpdateContext context)
        {
            if (context.Update.UpdateType.HasFlag(UpdateType.Message))
                return HandleMessage(context);
            if (context.BotFunctions.GetIndexButton(context.Update, Buttons) is ButtonSearch buttonSearch)
                return HandleButtons(context, buttonSearch);
            return context.ReplyBug("Не сработал ни один из обработчиков сообщения внутри страницы задания значения");
        }

        private async Task HandleMessage(BotCore.Interfaces.IUpdateContext<User> context)
        {
            if (await IsNotCorrectValue(context, context.Update.Message!)) return;
            RegisterModel.Value = CorrectValue(context.Update.Message!);
            await _storageModel.Save();
            await context.Reply(new()
            {
                Message = $"Вы уверены что ввели [{RegisterModel.Value}] верно?",
                Inline = Buttons,
                Medias = [MediaIsOk]
            });
        }

        private async Task HandleButtons(UpdateContext context, ButtonSearch buttonSearch)
        {
            if (buttonSearch.Button == ConstsShared.ButtonYes)
            {
                SaveValue(context.User, RegisterModel.Value);
                await _storageModel.Save();
                await _pageRouter.Navigate(context, RegisterStartPage.Key);
                return;
            }
            if (buttonSearch.Button == ConstsShared.ButtonNo)
            {
                await OnNavigate(context);
                return;
            }
            if (buttonSearch.Button == RegisterStartPage.ButtonBackRegisterMain)
            {
                await _pageRouter.Navigate(context, RegisterStartPage.Key);
                return;
            }
            await context.ReplyBug("Сработал метод нажатия на кнопку, но не был найден ни один из обработчиков");
        }

        private async Task<bool> IsNotCorrectValue(UpdateContext context, string? value)
        {
            if (IsCorrectValue(value)) return false;
            await context.Reply(new SendModel()
            {
                Message = $"{MessageNotCorrect}\n\n{MessageStart}",
                Inline = ButtonsBack,
                Medias = [ConstsShared.MediaError]
            });
            return true;
        }

        protected abstract bool IsCorrectValue(string? value);
        protected abstract void SaveValue(User user, string? value);
        protected virtual string? CorrectValue(string? value) => value?.Trim();

        public override Task OnNavigate(UpdateContext context) => context.Reply(new SendModel()
        {
            Message = MessageStart,
            Inline = ButtonsBack,
            Medias = [MediaInput]
        });

        public void BindService(PageRouterHelper service) => _pageRouter = service;
    }
}
