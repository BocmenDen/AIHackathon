using AIHackathon.DB;
using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using AIHackathon.Models;
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
                .ConfigureServices((context, services) =>
                {
                    services.AddMemoryCache();

                    // Говорим откуда читать параметры
                    services.Configure<TgClientOptions>(context.Configuration.GetSection(nameof(TgClientOptions)));
                    services.Configure<DataBaseOptions>(context.Configuration.GetSection(nameof(DataBaseOptions)));
                    services.Configure<PooledObjectProviderOptions<DataBase>>(context.Configuration.GetSection(nameof(DataBaseOptions)));
                    services.Configure<Settings>(context.Configuration.GetSection(nameof(Settings)));
                    services.Configure<MessageSpamOptions>(context.Configuration.GetSection(nameof(MessageSpamOptions)));
                })
                .RegisterFiltersRouterAuto<User, UpdateContext>() // Регистрация фильтров
                .RegisterDBContextOptions((s, _, b) =>
                {
                    b.UseSqlite($"Data Source={s.GetRequiredService<IOptions<DataBaseOptions>>().Value.GetPathOrDefault()}");
                });

        static void Main()
        {
            IHost host = ConfigureServices()
                        .RegisterClient<TgClient<User, DataBase>>()
                        .RegisterPagesInRouter<User, UpdateContext, string>(Assembly.GetAssembly(typeof(Program))!)
                        .Build();

            var spamFilter = host.Services.GetRequiredService<MessageSpam>();
            var viewErrorsLayer = host.Services.GetRequiredService<LayerViewError>();
            var editOldMessage = host.Services.GetRequiredService<LayerOldEditMessage<User, UpdateContext>>();
            var filterRouting = host.Services.GetRequiredService<HandleFilterRouter>();
            var pageRouting = host.Services.GetRequiredService<HandlePageRouter>();

            foreach (var client in host.Services.GetServices<IClientBot<IUser, IUpdateContext<IUser>>>())
                if (client is IClientBot<User, UpdateContext> castClient)
                    castClient.Update += spamFilter.HandleNewUpdateContext;

            spamFilter.Update += viewErrorsLayer.HandleNewUpdateContext;
            viewErrorsLayer.Update += editOldMessage.HandleNewUpdateContext;
            editOldMessage.Update += filterRouting.HandleNewUpdateContext;
            filterRouting.Update += pageRouting.HandleNewUpdateContext;
            pageRouting.Update += (context) => context.ReplyBug("В данный момент у вас не открыта страница 📄.");
#if DEBUGTEST
            var db = host.Services.GetRequiredService<DataBase>();
            var command = new Command()
            {
                Name = "Привет, мир!"
            };
            db.Commands.Add(command);
            db.Participants.Add(new Participant()
            {
                Surname = "Иванов",
                Name = "Денис",
                MiddleName = "Дмитриевич",
                Email = "test@test.com",
                Phone = "+77777777777",
                Command = command
            });
            db.Participants.Add(new Participant()
            {
                Surname = "Иванов2",
                Name = "Денис",
                MiddleName = "Дмитриевич",
                Email = "test@test.com",
                Phone = "+77777777777",
                Command = command
            });
            for (int i = 0; i < 100; i++)
            {
                var tmpCommand = new Command()
                {
                    Name = Path.GetRandomFileName()
                };
                db.Participants.Add(new Participant()
                {
                    Surname = Path.GetRandomFileName(),
                    Name = "Денис",
                    MiddleName = "Дмитриевич",
                    Email = "test@test.com",
                    Phone = "+77777777777",
                    Command = tmpCommand
                });
                var particantTmp = new Participant()
                {
                    Surname = Path.GetRandomFileName(),
                    Name = "Денис",
                    MiddleName = "Дмитриевич",
                    Email = "test@test.com",
                    Phone = "+77777777777",
                    Command = tmpCommand
                };
                db.Participants.Add(particantTmp);
                db.SaveChanges();
                db.Metrics.Add(new MetricParticipant()
                {
                    ParticipantId = particantTmp.Id,
                    Library = "L1",
                    DateTime = DateTime.Now.AddMinutes(-i),
                    PathFile = "Test.txt",
                    Accuracy = 0.2
                });
            }
            db.SaveChanges();
            db.ChangeTracker.Clear();
#endif
            host.Run();
        }
    }
}
