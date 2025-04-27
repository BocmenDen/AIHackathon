using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using BotCore.PageRouter;

namespace AIHackathon.Services
{
    [Service(ServiceType.Singleton)]
    public class PageRouterHelper(HandlePageRouter<User, UpdateContext, string> router, IDBUserPageParameter dBUserPageParameter)
    {
        public async Task Navigate(UpdateContext context, string keyPage, object? parameter = null)
        {
            try
            {
                if (parameter != null)
                    await dBUserPageParameter.SetParameter(context.User, parameter);
                await router.Navigate(context, keyPage);
            }
            catch (Exception e)
            {
                await context.ReplyBug(e);
            }
        }
    }
}
