using AIHackathon.DB;
using AIHackathon.Extensions;
using AIHackathon.Services;
using BotCore;
using BotCore.EfDb;
using BotCore.FilterRouter.Extensions;
using BotCore.Interfaces;
using BotCore.PageRouter;
using BotCore.Tg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace AIHackathon
{
    internal class Program
    {
        /// <summary>
        /// Создание и конфигурация хоста
        /// </summary>
        static IHostBuilder ConfigureServices() => BotBuilder.CreateDefaultBuilder()
                .ConfigureAppConfiguration(app => app.AddUserSecrets(Assembly.GetExecutingAssembly()))
                .RegisterServices(
                    Assembly.GetAssembly(typeof(Program)),
                    Assembly.GetAssembly(typeof(TgClient)),
                    Assembly.GetAssembly(typeof(HandleFilterRouter)),
                    Assembly.GetAssembly(typeof(HandlePageRouter))

                )
                .ConfigureServices((b, s) =>
                {
                    s.AddMemoryCache();

                    // Говорим откуда читать параметры
                    s.Configure<TgClientOptions>(b.Configuration.GetSection(nameof(TgClientOptions)));
                    s.Configure<DataBaseOptions>(b.Configuration.GetSection(nameof(DataBaseOptions)));
                    s.Configure<PooledObjectProviderOptions<DataBase>>(b.Configuration.GetSection(nameof(DataBaseOptions)));
                })
                .RegisterFiltersRouterAuto<User, UpdateContext>() // Регистрация фильтров
                .RegisterDBContextOptions((s, _, b) => b.UseSqlite($"Data Source={s.GetRequiredService<IOptions<DataBaseOptions>>().Value.GetPathOrDefault()}"));

        static void Main()
        {
            IHost host = ConfigureServices()
                        .RegisterClient<TgClient<User, DataBase>>()
                        .RegisterPagesInRouter<User, UpdateContext, string>(Assembly.GetAssembly(typeof(Program))!)
                        .Build();

            var spamFilter = host.Services.GetRequiredService<MessageSpam>();
            var editOldMessage = host.Services.GetRequiredService<LayerOldEditMessage<User, UpdateContext>>();
            var filterRouting = host.Services.GetRequiredService<HandleFilterRouter>();
            var pageRouting = host.Services.GetRequiredService<HandlePageRouter>();

            foreach (var client in host.Services.GetServices<IClientBot<IUser, IUpdateContext<IUser>>>())
                if (client is IClientBot<User, UpdateContext> castClient)
                    castClient.Update += spamFilter.HandleNewUpdateContext;

            spamFilter.Update += editOldMessage.HandleNewUpdateContext;
            editOldMessage.Update += filterRouting.HandleNewUpdateContext;
            filterRouting.Update += pageRouting.HandleNewUpdateContext;
            pageRouting.Update += (context) => context.ReplyBug("В данный момент у вас не открыта страница 📄.");

            host.Run();
        }
    }
}
