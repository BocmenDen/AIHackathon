using BotCore.Interfaces;
using System.Collections.Concurrent;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class LayerOldEditMessage<TUser, TContext> : IInputLayer<TUser, TContext>, INextLayer<TUser, IUpdateContext<TUser>>
        where TUser : IUser
        where TContext : IUpdateContext<TUser>
    {
        private readonly ConcurrentDictionary<long, object> usersEditMessage = new();

        public event Func<IUpdateContext<TUser>, Task>? Update;

        public void StopEditLastMessage(long userId) => _ = usersEditMessage.Remove(userId, out var _);

        public async Task HandleNewUpdateContext(TContext context)
        {
            if (context.Update.UpdateType.HasFlag(UpdateType.Message) || context.Update.UpdateType.HasFlag(UpdateType.Media))
            {
                if (usersEditMessage.TryGetValue(context.User.Id, out object? lastMessage))
                    await context.Reply([new KeyValuePair<string, object>(BotCore.Tg.TgClient.KeyMessagesToEdit, lastMessage)]);
                StopEditLastMessage(context.User.Id);
            }
            UpdateContextEdit newContext = new(context.BotFunctions, context.User, context.Update, async (sendModel) =>
            {
                if (usersEditMessage.TryGetValue(context.User.Id, out object? lastMessage))
                {
                    sendModel[BotCore.Tg.TgClient.KeyMessagesToEdit] = lastMessage;
                    await context.Reply(sendModel);
                    if (sendModel.IsEmpty)
                    {
                        StopEditLastMessage(context.User.Id);
                    }
                }
                else
                {
                    await context.Reply(sendModel);
                    if (!sendModel.IsEmpty && sendModel.Keyboard is null && sendModel.ContainsKey(BotCore.Tg.TgClient.KeyMessagesToEdit))
                        usersEditMessage[context.User.Id] = sendModel[BotCore.Tg.TgClient.KeyMessagesToEdit];
                }
            });
            if (Update != null)
                await Update.Invoke(newContext);
        }

        private class UpdateContextEdit(IClientBotFunctions botFunctions, TUser user, UpdateModel model, Func<SendModel, Task> reply) : IUpdateContext<TUser>
        {
            public IClientBotFunctions BotFunctions => botFunctions;
            public TUser User => user;
            public UpdateModel Update => model;
            public Task Reply(SendModel send) => reply(send);
        }
    }
}
