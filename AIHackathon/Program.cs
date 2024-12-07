using AIHackathon.Model;
using AIHackathon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OneBot;
using OneBot.Tg;
using OneBot.Utils;
using System.Reflection;

namespace AIHackathon
{
    internal class Program
    {
        private const string KeyConnectionDB = "connectionDB";

        private static void Main(string[] args)
        {
            string configPath = args!= null && args.Length == 1 ? args[0] : "./config.json";
            IHost host = BotBuilder.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c => c.AddJsonFile(configPath))
                .RegisterDBContextOptions((c, b) => b.UseNpgsql(c[KeyConnectionDB]??throw new Exception("Отсутствуют данные подключения к БД")))
                .RegisterServices(
                    Assembly.GetAssembly(typeof(Program)),
                    Assembly.GetAssembly(typeof(TgClient))
                )
                .Build();
            IServiceProvider serviceProvider = host.Services;

            TgClient<User, DataBase> clientBot = serviceProvider.GetRequiredService<TgClient<User, DataBase>>();
            MessageSpam spamFilter = serviceProvider.GetRequiredService<MessageSpam>();
            BotHandle botHandle = serviceProvider.GetRequiredService<BotHandle>();
            spamFilter.Init(botHandle.HandleCommand);
            clientBot.RegisterUpdateHadler(spamFilter.HandleCommand);

            var idThisSender = SharedUtils.CalculeteID<Program>();

            clientBot.Run().Wait();
        }
    }
}
