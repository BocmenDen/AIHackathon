using AIHackathon.DB;
using AIHackathon.Services;
using BotCore.Interfaces;
using BotCore.PageRouter.Interfaces;

namespace AIHackathon.Base
{
    public class PageBase : IPage, IPageOnExit, IBindService<LayerOldEditMessage<User, UpdateContext>>, IBindUser<User>
    {
        private LayerOldEditMessage<User, UpdateContext> _layerEditMessage = null!;
        protected long UserId { get; private set; }

        public virtual Task HandleNewUpdateContext(UpdateContext context) => Task.CompletedTask;
        public virtual Task OnNavigate(UpdateContext context) => HandleNewUpdateContext(context);
        protected virtual Task OnExit(UpdateContext context) => context.Reply([]);

        async Task IPageOnExit<User, IUpdateContext<User>>.OnExit(IUpdateContext<User> context)
        {
            await OnExit(context);
            _layerEditMessage.StopEditLastMessage(context.User.Id);
        }
        void IBindUser<User>.BindUser(User user) => UserId = user.Id;
        void IBindService<LayerOldEditMessage<User, UpdateContext>>.BindService(LayerOldEditMessage<User, IUpdateContext<User>> service)
            => _layerEditMessage = service;

        protected void StopEditLastMessage() => _layerEditMessage.StopEditLastMessage(UserId);
    }
}
