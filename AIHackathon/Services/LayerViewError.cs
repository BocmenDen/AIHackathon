using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using BotCore.Interfaces;
using Microsoft.Extensions.Logging;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class LayerViewError(ILogger<LayerViewError> logger) : IInputLayer<User, UpdateContext>, INextLayer<User, UpdateContext>
    {
        public event Func<UpdateContext, Task>? Update;

        public async Task HandleNewUpdateContext(UpdateContext context)
        {
            if (Update == null) return;
            try
            {
                await Update(context);
            }
            catch (Exception e)
            {
                await context.ReplyBug(e);
                logger.LogError(e, "У пользователя {userName}[{userId}] произошла ошибка", context.User.TgChat.Username, context.User.Id);
            }
        }
    }
}