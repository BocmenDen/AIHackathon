﻿using AIHackathon.Model;
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
        private const string KeyConnectionDB = "connectionDB";
        private readonly static ContextBot<User, DataBase> _bot = new();
        private static string _connectText = null!;

        private static void Main(string[] args)
        {
            string configPath = args!= null && args.Length == 1 ? args[0] : "./config.json";
            _bot.Init(
                dbBuild => dbBuild.UseNpgsql(_connectText),
                configuration => configuration.AddJsonFile(configPath),
                servicesDetect: [Assembly.GetAssembly(typeof(Program))!, Assembly.GetAssembly(typeof(TgClient))!]
            );

            var config = _bot.GetService<IConfiguration>();

            _connectText = config[KeyConnectionDB]??throw new Exception("Отсутствуют данные подключения");

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
