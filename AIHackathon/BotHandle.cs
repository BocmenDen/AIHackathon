using AIHackathon.Model;
using AIHackathon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OneBot;
using OneBot.Attributes;
using OneBot.Extensions;
using OneBot.Interfaces;
using OneBot.Models;
using OneBot.Tg;
using OneBot.Utils;
using System.Text;

namespace AIHackathon
{
    [Service]
    public class BotHandle(ILogger logger, ContextBot<User, DataBase> bot, IConfiguration configuration)
    {
        private const string KeyInsertId = "Id";
        private const string KeyCommandGetKeyboard = "bot_commandKeyboard";
        private const string KeyHelpMessage = "bot_helpInfoPath";

        private readonly int WaitStartMessage = 1000;
        private readonly static string[] HelloMessage =
        [
            "Привет! 👋 Я помогу тебе проверить себя! 🚀",
            "\nВот что нужно сделать:",
            $"\n1️⃣Заполни анкету регистрации по этой ссылке: https://github.com/BocmenDen ➡️ и укажи свой ID: [{KeyInsertId}]",
            "2️⃣ Дождись сообщения от бота об открытии доступа. ✉️",
            "3️⃣ Отправь свою обученную модель для оценки. 🤖",
            "4️⃣ Следи за результатами своей команды!\n🏆"
        ];
        private readonly static ButtonsSend Commands = new([["Рейтинг", "To CSV"], ["Информация", "Код оценивания"]]);
        private readonly string _commandGetKeyboard = configuration[KeyCommandGetKeyboard]??throw new Exception("Нет данных об команде получения клавиатуры");
        private readonly string _helpInfoText = File.ReadAllText(configuration[KeyHelpMessage]??throw new Exception("Нет данных файле с справкой"));
        public static int Id => SharedUtils.CalculeteID<BotHandle>();

        private readonly ConcurrentList<int> _usersWait = [];
        private readonly ILogger _logger = logger.CacheSender(Id)??throw new ArgumentNullException(nameof(logger));
        private readonly ContextBot<User, DataBase> _bot = bot??throw new ArgumentNullException(nameof(bot));

        private async Task HandleCommandAbstractyon(ReceptionClient<User> updateData, Func<Task> handle)
        {
            if (!updateData.User.IsAdmin && _usersWait.Contains(updateData.User.Id))
            {
                await updateData.Send("Ого! 😮 Кажется, я немного перегружен! 😅 Попробуйте отправить сообщение чуть позже. Сейчас я обрабатываю другое. 🙏 Только одно сообщение за раз! ☝️");
                return;
            }
            _usersWait.Add(updateData.User.Id);
            try
            {
                await handle();
            }
            catch (Exception ex)
            {
                _logger.Error($"Произошла ошибка при обработке команды[{updateData.ReceptionType}] пользователя[{updateData.User.Id}]: {ex}");
                SendingClient sendingClient = ex.Message;
                await updateData.Send(sendingClient);
            }
            finally
            {
                _usersWait.Remove(updateData.User.Id);
            }
        }

        public async void HandleCommand(ReceptionClient<User> updateData)
        {
            await HandleCommandAbstractyon(updateData, async () =>
            {
                if (await CheckRegister(updateData)) return;

                if (updateData.ReceptionType.HasFlag(ReceptionType.Command) &&
                    updateData.Command == _commandGetKeyboard &&
                    (updateData.User.IsAdmin || updateData.User.IsStarted)
                    )
                {
                    await SendKeyboard(updateData);
                }
                else if (updateData.User.IsAdmin &&
                updateData.ReceptionType.HasFlag(ReceptionType.Media) &&
                updateData.Medias![0].Type == ".csv")
                {
                    await ApplayCommands(updateData);
                }
                else if (updateData.ReceptionType.HasFlag(ReceptionType.Media))
                {
                    await _bot.GetService<LoadMetrics>().Run(updateData);
                }
                else
                {
                    var btnNull = updateData.Client.GetIndexButton(updateData, Commands);
                    if (btnNull == null)
                    {
                        await updateData.Send("Извини, 😞 я тебя не понял! 🤔");
                        return;
                    }
                    var btn = (ButtonSearch)btnNull;
                    if (btn.Row == 0)
                    {
                        if (btn.Column == 0)
                        {
                            await GetRatingUser(updateData);
                        }
                        else
                        {
                            Func<int, bool> predict = _ => true;
                            if (!updateData.User.IsAdmin) predict = (command) => command == updateData.User.CommandId;
                            var text = RatingCSVTable(predict);
                            MemoryStream stream = new();
                            StreamWriter writer = new(stream, Encoding.UTF8);
                            writer.WriteLine("UserId;CommandId;CommandName;MetricId;DateTime;Library;Accuracy;PathModel;"); // Лень оптимизировать
                            writer.Write(text); // Лень оптимизировать
                            writer.Flush();
                            MediaSource mediaSource = new(() => Task.FromResult<Stream>(stream))
                            {
                                Name = "fullRating.csv",
                                Type = ".csv",
                                MimeType = "text/csv",
                            };
                            stream.Position = 0;
                            await updateData.Send(new()
                            {
                                Message = "Готово! 🎉 Информация о попытках сохранена в .csv файле! ✅ Можно приступать к анализу! 🤓 📊",
                                Medias = [mediaSource]
                            });
                            stream.Close();
                            stream.Dispose();
                            writer.Dispose();
                        }
                    }
                    else
                    {
                        if (btn.Column == 0) // Info
                        {
                            await updateData.Send(_helpInfoText);
                        }
                        else // Code
                        {
                            await updateData.Send(new SendingClient()
                            {
                                Message = $"```python\n{File.ReadAllText(configuration[LoadMetrics.KeyPathScript]!)}\n```"
                            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
                        }
                    }
                }
            });
        }

        private async Task<bool> CheckRegister(ReceptionClient<User> updateData)
        {
            if (!(updateData.User.IsStarted || updateData.User.IsAdmin))
            {
                SendingClient sendingClient = string.Empty;
                foreach (var message in HelloMessage)
                {
                    sendingClient.Message += message.Replace(KeyInsertId, updateData.User.Id.ToString()) + "\n";
                    await updateData.Send(sendingClient);
                    Thread.Sleep(WaitStartMessage);
                }
                return true;
            }
            return false;
        }

        private async Task ApplayCommands(ReceptionClient<User> updateData)
        {
            SendingClient send = string.Empty;
            using var streamFile = new StreamReader(await updateData.Medias![0].GetStream());
            string file = streamFile.ReadToEnd();
            var matrix = file.Split("\n").Select(x => x.Replace("\r", "").Split(',').Select(d => d.Trim()).ToArray());
            var db = _bot.GetService<DataBase>();
            var tgClient = _bot.GetService<TgClient<User, DataBase>>();
            foreach (var line in matrix)
            {
                if (line.Length != 2 || !int.TryParse(line[0], out int id)) continue;
                string commandName = line[1];
                Command? command = db.Commands.AsNoTracking().FirstOrDefault(x => x.Name == commandName);
                if (command == null)
                {
                    command = new Command(commandName);
                    command = db.CreateElementAndReload(command, db.Commands);
                }
                User? user = db.GetUser(id);
                if (user == null)
                {
                    send.Message += $"Упс! 😕 Похоже, пользователя с ID [{id}] нет в базе данных. 🤔 Не могу обновить данные, которых нет. 😞 Надо проверить, всё ли в порядке.\n";
                    await updateData.Send(send);
                    continue;
                }
                else
                {
                    send.Message += $"🎉 Ура! 🎉 К команде {commandName} присоединился новый участник — пользователь [{id}]!\n";
                    await updateData.Send(send);
                }
                var oldIdComman = user.CommandId;
                user.CommandId = command.CommandId;
                db.Users.Update(user);
                db.SaveChanges();
                user.IsStarted = true;
                TgUser<User> tgUser = db.TgUsers.FirstOrDefault(x => x.User.Id == id)!;
                if (oldIdComman != user.CommandId)
                {
                    await tgClient.Send(tgUser, new SendingClient()
                    {
                        Message = $"Ого! 🤩 Меня добавили в базу данных бота! 🥳 Теперь я официально часть системы! 🤖\n\n{_helpInfoText}",
                        Keyboard = Commands
                    });
                }
            }
            db.SaveChanges();
        }

        private static Task SendKeyboard(ReceptionClient<User> updateData)
        {
            return updateData.Send(new SendingClient()
            {
                Message = "ГОРЯЧАЯ НОВИНА! 🔥 Клавиатура в твоих руках! ⌨️ Получай и наслаждайся быстрой печатью! 🚀",
                Keyboard = Commands
            });
        }

        private struct CommandRating(double a, string n)
        {
            public double Accuracy = a;
            public string NameCommand = n;
        }

        private async Task GetRatingUser(ReceptionClient<User> updateData)
        {
            var db = _bot.GetService<DataBase>();
            SendingClient sending = "";
            int rating = 1;

            var commandInfo = (from u in db.Users
                               join c in db.Commands on u.CommandId equals c.CommandId
                               join m in db.Metrics on u.Id equals m.UserId
                               group new { c, m } by new { c.Name, c.CommandId } into g
                               select new
                               {
                                   commandName = g.Key.Name,
                                   commandId = g.Key.CommandId,
                                   accuracy = g.Max(x => x.m.Accuracy),
                               }
                       ).OrderBy(x => x.accuracy);

            foreach (var command in commandInfo)
            {
                sending += $"[{rating++}] {command.commandName} -> Accuracy {command.accuracy}";
                if (command.commandId == updateData.User.CommandId)
                    sending += "📍";
                sending+="\n";
            }
            if (string.IsNullOrEmpty(sending.Message)) sending.Message = "Таблица рейтинга пока пуста. 😕 Ждем первых участников! ⏳ Скоро здесь появятся лучшие результаты! ✨";
            await updateData.Send(sending);
        }

        private string RatingCSVTable(Func<int, bool> predict)
        {
            var db = _bot.GetService<DataBase>();
            var fullInfo = string.Join("\n", (from u in db.Users
                                              join c in db.Commands on u.CommandId equals c.CommandId
                                              join m in db.Metrics on u.Id equals m.UserId
                                              select new
                                              {
                                                  commandId = c.CommandId,
                                                  line = $"{u.Id};{u.CommandId};{c.Name};{m.MetricId};{m.DateTime};{m.Library};{m.Accuracy};{m.PathFile};"
                                              }
            ).AsNoTracking().AsEnumerable().Where(x => predict(x.commandId)).Select(x => x.line));
            return fullInfo;
        }
    }
}
