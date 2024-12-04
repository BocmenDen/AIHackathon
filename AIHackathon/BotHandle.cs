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
using System.Collections.Concurrent;
using System.Text;
using Telegram.Bot.Extensions;

namespace AIHackathon
{
    [Service]
    public class BotHandle(ILogger logger, ContextBot<User, DataBase> bot, IConfiguration configuration)
    {
        public static int Id => SharedUtils.CalculeteID<BotHandle>();
        private const string KeyInsertId = "{InsertIdUser}";
        private const string KeyLinkSurvey = "bot_linkSurvey";
        private const string KeyCommandGetKeyboard = "bot_commandKeyboard";
        private const string KeyHelpMessage = "bot_helpInfoPath";
        private const string KeyIsFilterUsers = "bot_isFilterUsers";

        public const string SendAllCommand = "/sendAll";
        private const int WaitStartMessage = 1000;
        private readonly static string[] HelloMessage =
        [
            "Привет! 👋 Я помогу тебе проверить себя! 🚀",
            "\nВот что нужно сделать:",
            $"\n1️⃣Заполни анкету регистрации по этой [ссылке]({KeyLinkSurvey}) ➡️ и укажи свой ID: [{KeyInsertId}]",
            "2️⃣ Дождись сообщения от бота об открытии доступа. ✉️",
            "3️⃣ Отправь свою обученную модель для оценки. 🤖",
            "4️⃣ Следи за результатами своей команды!\n🏆"
        ];
        private readonly static ButtonsSend Commands = new([["Рейтинг", "Export to CSV"], ["Информация", "Код оценивания"]]);

        private readonly bool _isFilterUsers = configuration.GetValue<bool?>(KeyIsFilterUsers) ?? false;
        private readonly string _linkSurvey = configuration[KeyLinkSurvey]??throw new Exception("Нет данных о ссылке на опрос");
        private readonly string _commandGetKeyboard = configuration[KeyCommandGetKeyboard]??throw new Exception("Нет данных об команде получения клавиатуры");
        private readonly string _helpInfoText = File.ReadAllText(configuration[KeyHelpMessage]??throw new Exception("Нет данных файле с справкой"));

        private readonly ConcurrentDictionary<int, bool> _usersWait = [];
        private readonly ILogger _logger = logger.CacheSender(Id)??throw new ArgumentNullException(nameof(logger));
        private readonly ContextBot<User, DataBase> _bot = bot??throw new ArgumentNullException(nameof(bot));

        private async Task HandleCommandAbstractyon(ReceptionClient<User> updateData, Func<Task> handle)
        {
            if (!updateData.User.IsAdmin && _usersWait.TryGetValue(updateData.User.Id, out bool isSend))
            {
                if (!isSend) {
                    await updateData.Send("Ого! 😮 Кажется, я немного перегружен! 😅 Попробуйте отправить сообщение чуть позже. Сейчас я обрабатываю другое. 🙏 Только одно сообщение за раз! ☝️");
                    _usersWait.TryUpdate(updateData.User.Id, true, false);
                }
                return;
            }
            _usersWait.TryAdd(updateData.User.Id, false);
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
                _usersWait.TryRemove(updateData.User.Id, out bool _);
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
                }else if (updateData.User.IsAdmin &&
                        updateData.ReceptionType == ReceptionType.Message &&
                        updateData.Message?.IndexOf(SendAllCommand, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    string message = ((Telegram.Bot.Types.Update)updateData.OriginalMessage!)!.Message!.ToMarkdown()!.Replace(SendAllCommand, "", StringComparison.OrdinalIgnoreCase);
                    await SendAllUsers(message);
                }
                else if (updateData.ReceptionType.HasFlag(ReceptionType.Media) && !updateData.User.IsAdmin)
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
                            MemoryStream stream = new();
                            StreamWriter writer = new(stream, Encoding.UTF8);
                            GetCSVTable(predict, writer.WriteLine);
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
                            await updateData.Send(new SendingClient() { Message = _helpInfoText.Replace(KeyInsertId, updateData.User.Id.ToString()) }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
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
                if (_isFilterUsers) return true;
                SendingClient sendingClient = string.Empty;
                sendingClient.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown);
                foreach (var message in HelloMessage)
                {
                    sendingClient.Message += message.Replace(KeyLinkSurvey, _linkSurvey).Replace(KeyInsertId, updateData.User.Id.ToString()) + "\n";
                    await updateData.Send(sendingClient);
                    Thread.Sleep(WaitStartMessage);
                }
                if (updateData.OriginalMessage is Telegram.Bot.Types.Update up && !string.IsNullOrWhiteSpace(up.Message?.From?.Username))
                {
                    updateData.User.Nickname = up.Message.From.Username;
                    var db = _bot.GetService<DataBase>();
                    db.Users.Update(updateData.User);
                    db.SaveChanges();
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
            var matrix = file.Split("\n").Select(x => x.Replace("\r", "").Replace(',', ';').Split(';').Select(d => d.Trim()).ToArray());
            var db = _bot.GetService<DataBase>();
            var tgClient = _bot.GetService<TgClient<User, DataBase>>();
            foreach (var line in matrix)
            {
                if (line.Length < 3 || !int.TryParse(line[0], out int id)) continue;
                string commandName = line[2];
                string userName = line[1];
                Command? command = db.Commands.AsNoTracking().FirstOrDefault(x => x.Name == commandName);
                if (command == null)
                {
                    command = new Command(commandName);
                    db.Commands.Add(command);
                    db.SaveChanges();
                }
                User? user = db.Users.Find(id);
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
                user.CommandId = command.Id;
                user.Name = userName;
                db.Users.Update(user);
                db.SaveChanges();
                user.IsStarted = true;
                TgUser<User> tgUser = db.TgUsers.AsNoTracking().FirstOrDefault(x => x.User.Id == id)!;
                if (oldIdComman != user.CommandId)
                {
                    await tgClient.Send(tgUser, new SendingClient()
                    {
                        Message = $"Ого! 🤩 Меня добавили в базу данных бота! 🥳 Теперь я официально часть системы! 🤖\n\n{_helpInfoText.Replace(KeyInsertId, updateData.User.Id.ToString())}",
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
                               join c in db.Commands on u.CommandId equals c.Id
                               join m in db.Metrics on u.Id equals m.UserId
                               group new { c, m } by new { c.Name, c.Id } into g
                               select new
                               {
                                   commandName = g.Key.Name,
                                   commandId = g.Key.Id,
                                   metric = g.Max(x => x.m.ROC_AUC),
                               }
                       ).OrderBy(x => x.metric);

            foreach (var command in commandInfo)
            {
                sending += $"[{rating++}] {command.commandName} -> ROC AUC {command.metric}";
                if (command.commandId == updateData.User.CommandId)
                    sending += "📍";
                sending+="\n";
            }
            if (string.IsNullOrEmpty(sending.Message)) sending.Message = "Таблица рейтинга пока пуста. 😕 Ждем первых участников! ⏳ Скоро здесь появятся лучшие результаты! ✨";
            await updateData.Send(sending);
        }

        private void GetCSVTable(Func<int, bool> predict, Action<string> writeLine)
        {
            var db = _bot.GetService<DataBase>();
            var lines = (from u in db.Users
                         join c in db.Commands on u.CommandId equals c.Id
                         join m in db.Metrics on u.Id equals m.UserId
                         where m.Error == null || m.Error == string.Empty
                         select new
                         {
                             commandId = c.Id,
                             line = $"{u.Id};{u.Name};{u.Nickname};{u.CommandId};{c.Name};{m.MetricId};{m.DateTime};{m.Library};{m.Accuracy};{m.PathFile};"
                         }
            ).AsNoTracking().AsEnumerable().Where(x => predict(x.commandId)).Select(x => x.line);
            writeLine("UserId;UserName;UserNickname;CommandId;CommandName;MetricId;DateTime;Library;MetricValue;PathModel;");
            foreach (var line in lines)
                writeLine(line);
        }

        private async Task SendAllUsers(string message)
        {
            var db = _bot.GetService<DataBase>();
            var tgClient = _bot.GetService<TgClient<User, DataBase>>();
            foreach (var tgUser in db.TgUsers.Include(x => x.User).AsNoTracking())
            {
                if (tgUser.User.IsAdmin) continue;
                await tgClient.Send(tgUser, new SendingClient()
                {
                    Message = $"✉️ Сообщение от организаторов хакатона:\n\n{message}",
                    Keyboard = Commands
                }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
            }
        }
    }
}
