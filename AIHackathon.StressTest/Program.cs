using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using AIHackathon.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneBot;
using System.Reflection;
using OneBot.Models;
using OneBot.Interfaces;
using System.Diagnostics;

namespace AIHackathon.StressTest
{
    internal class Program
    {
        private const string KeyConnectionDB = "connectionDB";

        static void Main(string[] args)
        {
            string configPath = args!= null && args.Length == 1 ? args[0] : "./config.json";
            IHost host = BotBuilder.CreateDefaultBuilder()
                .ConfigureAppConfiguration(c => c.AddJsonFile(configPath))
                .RegisterDBContextOptions((c, b) => b.UseNpgsql(c[KeyConnectionDB]??throw new Exception("Отсутствуют данные подключения к БД")))
                .RegisterServices(
                    Assembly.GetAssembly(typeof(DataBase))
                )
                .Build();
            IServiceProvider serviceProvider = host.Services;

            BotHandle botHandle = serviceProvider.GetRequiredService<BotHandle>();
            string pathModel = serviceProvider.GetRequiredService<IConfiguration>()["test_pathModel"]!;
            TestClient testClient = new();
            botHandle.HandleCommand(new ReceptionClient<User>
                (
                testClient,
                new User(1, true, 1, true, "", ""),
                (_, _) => Task.CompletedTask,
                ReceptionType.Message | ReceptionType.Command
                )
            {
                Command = "on"
            }).Wait();
            async Task<long> MeasureSendMessageTimeAsync(int index)
            {
                Console.WriteLine($"Запущен: {index}");
                Stream stream = File.OpenRead(pathModel);
                TaskCompletionSource completionSource = new();
                var stopwatch = Stopwatch.StartNew();
                await botHandle.HandleCommand(new ReceptionClient<User>
                    (
                        testClient,
                        new User(1, true, 1, false, "", ""),
                        (s, _) =>
                        {
                            if (s.Message?.Contains("ROC AUC") ?? false)
                            {
                                completionSource.SetResult();
                            }
                            return Task.CompletedTask;
                        },
                        ReceptionType.Media
                    )
                {
                    Medias = 
                    [
                        new MediaSource(async () => stream){
                            Name = $"{Path.GetRandomFileName()}.json"
                        }
                    ]
                });
                await completionSource.Task;
                stopwatch.Stop();
                stream.Dispose();
                Console.WriteLine($"Завершён: {index}");
                return stopwatch.ElapsedMilliseconds;
            }
            var tasks = new List<Task<long>>();
            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
            {
                int c = i;
                tasks.Add(Task.Factory.StartNew<long>(() => MeasureSendMessageTimeAsync(c).Result));
            }
            Task.WhenAll(tasks).Wait();
            stopwatch.Stop();
            Console.WriteLine($"Полное время: {stopwatch.ElapsedMilliseconds}");
            foreach (var task in tasks)
                Console.WriteLine(task.Result);
        }
    }

    internal class TestClient : IClientBot<User>
    {
        int IClientBot<User>.Id => int.MinValue;

        ButtonSearch? IClientBot<User>.GetIndexButton(ReceptionClient<User> client, ButtonsSend buttonsSend)
        {
            return null;
        }

        void IClientBot<User>.RegisterUpdateHadler(Action<ReceptionClient<User>> action)
        {
        }

        Task IClientBot<User>.Run(CancellationToken token)
        {
            return Task.CompletedTask;
        }

        void IClientBot<User>.UnregisterUpdateHadler(Action<ReceptionClient<User>> action)
        {
        }
    }
}
