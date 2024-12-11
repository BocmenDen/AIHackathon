using AIHackathon.Model;
using AIHackathon.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneBot.Attributes;
using OneBot.Models;
using OneBot.Tg;
using System.Text;
using Telegram.Bot.Extensions;

namespace AIHackathon
{
    [Service]
    public class BotHandle(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        private const string KeyInsertId = "{InsertIdUser}";
        private const string KeyLinkSurvey = "bot_linkSurvey";
        private const string KeyCommandGetKeyboard = "bot_commandKeyboard";
        private const string KeyHelpMessage = "bot_helpInfoPath";
        private const string KeyIsFilterUsers = "bot_isFilterUsers";

        public const string SendAllCommand = "/sendAll";
        public const string SendOnAcceptModels = "on";
        public const string SendOffAcceptModels = "off";

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
        private readonly static ButtonsSend Commands = new([["Рейтинг", "Export to CSV"], ["Информация", "Код оценивания"], ["Статистика бота"]]);

        private readonly bool _isFilterUsers = configuration.GetValue<bool?>(KeyIsFilterUsers) ?? false;
        private readonly string _linkSurvey = configuration[KeyLinkSurvey]??throw new Exception("Нет данных о ссылке на опрос");
        private readonly string _commandGetKeyboard = configuration[KeyCommandGetKeyboard]??throw new Exception("Нет данных об команде получения клавиатуры");
        private readonly string _helpInfoText = File.ReadAllText(configuration[KeyHelpMessage]??throw new Exception("Нет данных файле с справкой"));

        private bool _stateAcceptModels = false;

        public async Task HandleCommand(ReceptionClient<User> updateData)
        {
            using var scope = serviceProvider.CreateScope();
            if (await CheckRegister(updateData, scope.ServiceProvider)) return;

            if (updateData.ReceptionType.HasFlag(ReceptionType.Command) &&
                updateData.Command == _commandGetKeyboard &&
                (updateData.User.IsAdmin || updateData.User.IsStarted)
                )
            {
                await SendKeyboard(updateData);
            }
            else if (updateData.User.IsAdmin &&
                updateData.ReceptionType.HasFlag(ReceptionType.Command) &&
                (updateData.Command == SendOnAcceptModels || updateData.Command == SendOffAcceptModels))
            {
                switch (updateData.Command!)
                {
                    case SendOnAcceptModels:
                        _stateAcceptModels = true;
                        break;
                    case SendOffAcceptModels:
                        _stateAcceptModels = false;
                        break;
                }
                await updateData.Send($"Изменения применены, текущий статус: {_stateAcceptModels}");
            }
            else if (updateData.User.IsAdmin &&
                    updateData.ReceptionType.HasFlag(ReceptionType.Message) &&
                    updateData.Message?.IndexOf(SendAllCommand, StringComparison.OrdinalIgnoreCase) == 0)
            {
                string message = ((Telegram.Bot.Types.Update)updateData.OriginalMessage!)!.Message!.ToMarkdown()!.Replace(SendAllCommand, "", StringComparison.OrdinalIgnoreCase).Trim();
                if (string.IsNullOrWhiteSpace(message))
                {
                    await updateData.Send("Вы отправили пустую строку такое сообщениее не будед разосланно");
                    return;
                }
                await SendAllUsers(updateData, message, scope.ServiceProvider);
            }
            else if (updateData.User.IsAdmin &&
            updateData.ReceptionType.HasFlag(ReceptionType.Media) &&
            updateData.Medias![0].Type == ".csv")
            {
                await ApplayCommands(updateData, scope.ServiceProvider);
            }
            else if (updateData.ReceptionType.HasFlag(ReceptionType.Media) && !updateData.User.IsAdmin)
            {
                if (_stateAcceptModels)
                    await scope.ServiceProvider.GetRequiredService<LoadMetrics>().Run(updateData);
                else
                    await updateData.Send("В данный момент приём работ для оценивания отключен. Подождите немного возможно ведуться технические работы.");
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
                        await GetRatingUser(updateData, scope.ServiceProvider);
                    }
                    else
                    {
                        Func<int, bool> predict = _ => true;
                        if (!updateData.User.IsAdmin) predict = (command) => command == updateData.User.CommandId;
                        MemoryStream stream = new();
                        StreamWriter writer = new(stream, Encoding.UTF8);
                        GetCSVTable(predict, writer.WriteLine, updateData.User.IsAdmin, scope.ServiceProvider);
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
                else if (btn.Row == 1)
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
                else
                {
                    await SendMetrics(updateData, scope.ServiceProvider);
                }
            }
        }

        private async Task<bool> CheckRegister(ReceptionClient<User> updateData, IServiceProvider serviceProvider)
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
                    using var db = serviceProvider.GetRequiredService<DataBase>();
                    db.Users.Update(updateData.User);
                    await db.SaveChangesAsync();
                }
                return true;
            }
            return false;
        }

        private async Task ApplayCommands(ReceptionClient<User> updateData, IServiceProvider serviceProvider)
        {
            SendingClient send = string.Empty;
            using var streamFile = new StreamReader(await updateData.Medias![0].GetStream());
            string file = streamFile.ReadToEnd();
            var matrix = file.Split("\n").Select(x => x.Replace("\r", "").Replace(',', ';').Split(';').Select(d => d.Trim()).ToArray());
            using var db = serviceProvider.GetRequiredService<DataBase>();
            var tgClient = serviceProvider.GetRequiredService<TgClient<User, DataBase>>();
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
                    await db.SaveChangesAsync();
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
                if (user.IsAdmin)
                {
                    send.Message += $"⚠️ Администратор не может принять роль участника [{id}]. Пожалуйста, проверьте настройки. 🤔\n";
                    await updateData.Send(send);
                    continue;
                }
                var oldIdComman = user.CommandId;
                user.CommandId = command.Id;
                user.Name = userName;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                user.IsStarted = true;
                TgUser<User> tgUser = db.TgUsers.AsNoTracking().FirstOrDefault(x => x.User.Id == id)!;
                if (oldIdComman == null)
                {
                    await tgClient.Send(tgUser, new SendingClient()
                    {
                        Message = $"Ого! 🤩 Меня добавили в базу данных бота! 🥳 Теперь я официально часть системы! 🤖\n\n{_helpInfoText.Replace(KeyInsertId, updateData.User.Id.ToString())}",
                        Keyboard = Commands
                    }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
                }
            }
            await db.SaveChangesAsync();
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

        private async Task GetRatingUser(ReceptionClient<User> updateData, IServiceProvider serviceProvider)
        {
            using var db = serviceProvider.GetRequiredService<DataBase>();
            SendingClient sending = "";
            int rating = 1;

            var commandInfo = (from u in db.Users
                               join c in db.Commands on u.CommandId equals c.Id
                               join m in db.Metrics on u.Id equals m.UserId
                               where !EF.Functions.Like(u.Name, "%~%") && m.Error == null
                               group new { c, m } by new { c.Name, c.Id } into g
                               select new
                               {
                                   commandName = g.Key.Name,
                                   commandId = g.Key.Id,
                                   metric = g.Max(x => x.m.ROC_AUC),
                               }
                       ).OrderByDescending(x => x.metric);

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

        private void GetCSVTable(Func<int, bool> predict, Action<string> writeLine, bool isIgnoreTestUser, IServiceProvider serviceProvider)
        {
            using var db = serviceProvider.GetRequiredService<DataBase>();
            var lines = (from u in db.Users
                         join c in db.Commands on u.CommandId equals c.Id
                         join m in db.Metrics on u.Id equals m.UserId
                         where (m.Error == null || m.Error == string.Empty)
                         && (!isIgnoreTestUser || !EF.Functions.Like(u.Name, "%~%"))
                         select new
                         {
                             commandId = c.Id,
                             line = $"{u.Id};{u.Name};{u.Nickname};{u.CommandId};{c.Name};{m.MetricId};{m.DateTime.AddHours(3)};{m.Library};{m.Accuracy};{m.PathFile};"
                         }
            ).AsNoTracking().AsEnumerable().Where(x => predict(x.commandId)).Select(x => x.line);
            writeLine("UserId;UserName;UserNickname;CommandId;CommandName;MetricId;DateTime;Library;MetricValue;PathModel;");
            foreach (var line in lines)
                writeLine(line);
        }

        private async Task SendAllUsers(ReceptionClient<User> updateData, string message, IServiceProvider serviceProvider)
        {
            using var db = serviceProvider.GetRequiredService<DataBase>();
            var tgClient = serviceProvider.GetRequiredService<TgClient<User, DataBase>>();
            int counUsers = 0;
            foreach (var tgUser in db.TgUsers.Include(x => x.User).AsNoTracking().Where(x => !x.User.IsAdmin))
            {
                if (tgUser.User.IsAdmin || !tgUser.User.IsStarted) continue;
                await tgClient.Send(tgUser, new SendingClient()
                {
                    Message = $"✉️ Сообщение от организаторов хакатона:\n\n{message}",
                    Keyboard = Commands,
                    Medias = updateData.Medias
                }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
                counUsers++;
            }
            await updateData.Send($"Готово отправлено {counUsers} сообщений");
        }

        private Task SendMetrics(ReceptionClient<User> updateData, IServiceProvider serviceProvider)
        {
            StringBuilder stringBuilder = new();
            if (_stateAcceptModels)
                stringBuilder.AppendLine("Бот сейчас принимает модели для оценивания! 🎉 Вот текущая статистика:");
            else
                stringBuilder.AppendLine("Сейчас бот не принимает новые задачи 🚫. Вот краткий обзор текущей ситуации:");
            stringBuilder.AppendLine();
            stringBuilder.Append(serviceProvider.GetRequiredService<MessageSpam>().GetMetrics());
            stringBuilder.AppendLine($"📊 Оцениваемые работы: {LoadMetrics.CurrentProcessing}");
            stringBuilder.AppendLine();
            if (_stateAcceptModels)
                stringBuilder.AppendLine("Прием моделей включен ✅. Можете отправлять ваши модели! 🚀");
            else
                stringBuilder.AppendLine("Всё спокойно! 😌 Бот готов к работе и ожидает включение приёма работ ✅. В это время Вы можете подготовить модели к отправки или заготовить несколько вариаций обученной модели.");
            return updateData.Send(stringBuilder);
        }
    }
}
