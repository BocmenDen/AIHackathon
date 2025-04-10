using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using BotCore.PageRouter;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class PageRouterHelper(HandlePageRouter<User, UpdateContext, string> router)
    {
        public async Task Navigate(UpdateContext context, string keyPage)
        {
            try
            {
                await router.Navigate(context, keyPage);
            }
            catch (Exception e)
            {
                await context.ReplyBug(e);
            }
        }
    }
}
