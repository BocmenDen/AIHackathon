using AIHackathon.Model;
using AIHackathon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneBot;
using OneBot.Interfaces;
using OneBot.Tg;
using System.Reflection;

namespace AIHackathon
{
    internal class Program
    {
        private const string KeyPathDB = "DBPath";
        private readonly static ContextBot<User, DataBase> _bot = new();
        private static string _connectText = null!;

        private static void Main()
        {
            _bot.Init(
                dbBuild => dbBuild.UseSqlite(_connectText),
                configuration => configuration.AddUserSecrets(Assembly.GetExecutingAssembly()),
                servicesDetect: [Assembly.GetAssembly(typeof(Program))!, Assembly.GetAssembly(typeof(TgClient))!]
            );

            var config = _bot.GetService<IConfiguration>();

            _connectText = $"Data Source={config[KeyPathDB]}";

            TgClient<User, DataBase> clientBot = _bot.GetService<TgClient<User, DataBase>>();
            BotHandle botHandle = _bot.GetService<BotHandle>();

            clientBot.RegisterUpdateHadler(botHandle.HandleCommand);

            if (_bot.GetService<ILogger>() is Logger logger)
            {
                logger.RegisterName(clientBot.Id, "TG");
                logger.RegisterName(BotHandle.Id, "BotHandle");
            }

            clientBot.Run().Wait();
        }
    }
}
