using BotCore.Interfaces;
using AIHackathon.Extensions;
using AIHackathon.DB.Models;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class LayerViewError: IInputLayer<User, UpdateContext>, INextLayer<User, UpdateContext>
    {
        public event Func<UpdateContext, Task>? Update;

        public async Task HandleNewUpdateContext(UpdateContext context)
        {
            if (Update == null) return;
            try
            {
                await Update(context);
            }catch(Exception e)
            {
                await context.ReplyBug(e);
            }
        }
    }
}