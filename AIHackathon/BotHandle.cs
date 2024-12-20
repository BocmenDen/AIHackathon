﻿using AIHackathon.Model;
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
            "4️⃣ Следи за результатами своей команды! 🏆\n",
            "Если вас пропустили и вы отправили анкету регистрации, напишите об этой проблеме @bocmenden"
        ];
        private readonly static ButtonsSend Commands = new([["Рейтинг", "Export to CSV"], ["Информация", "Код оценивания"], ["Статистика бота"]]);

        private readonly bool _isFilterUsers = configuration.GetValue<bool?>(KeyIsFilterUsers) ?? false;
        private readonly string _linkSurvey = configuration[KeyLinkSurvey]??throw new Exception("Нет данных о ссылке на опрос");
        private readonly string _commandGetKeyboard = configuration[KeyCommandGetKeyboard]??throw new Exception("Нет данных об команде получения клавиатуры");
        private readonly string _helpInfoText = File.ReadAllText(configuration[KeyHelpMessage]??throw new Exception("Нет данных файле с справкой"));

        private bool _stateAcceptModels = false;

        public async Task HandleCommand(UpdateContext<User> context)
        {
            using var scope = serviceProvider.CreateScope();
            if (await CheckRegister(context, scope.ServiceProvider)) return;

            if (context.Update.UpdateType.HasFlag(UpdateType.Command) &&
                context.Update.Command == _commandGetKeyboard &&
                (context.User.IsAdmin || context.User.IsStarted)
                )
            {
                await SendKeyboard(context);
            }
            else if (context.User.IsAdmin &&
                context.Update.UpdateType.HasFlag(UpdateType.Command) &&
                (context.Update.Command == SendOnAcceptModels || context.Update.Command == SendOffAcceptModels))
            {
                switch (context.Update.Command!)
                {
                    case SendOnAcceptModels:
                        _stateAcceptModels = true;
                        break;
                    case SendOffAcceptModels:
                        _stateAcceptModels = false;
                        break;
                }
                await context.Send($"Изменения применены, текущий статус: {_stateAcceptModels}");
            }
            else if (context.User.IsAdmin &&
                    context.Update.UpdateType.HasFlag(UpdateType.Message) &&
                    context.Update.Message?.IndexOf(SendAllCommand, StringComparison.OrdinalIgnoreCase) == 0)
            {
                string message = ((Telegram.Bot.Types.Update)context.Update.OriginalMessage!)!.Message!.ToMarkdown()!.Replace(SendAllCommand, "", StringComparison.OrdinalIgnoreCase).Trim();
                if (string.IsNullOrWhiteSpace(message) && !context.Update.UpdateType.HasFlag(UpdateType.Media))
                {
                    await context.Send("Вы отправили пустую строку такое сообщениее не будет разосланно");
                    return;
                }
                await SendAllUsers(context, message, scope.ServiceProvider);
            }
            else if (context.User.IsAdmin &&
            context.Update.UpdateType.HasFlag(UpdateType.Media) &&
            context.Update.Medias![0].Type == ".csv")
            {
                await ApplayCommands(context, scope.ServiceProvider);
            }
            else if (context.Update.UpdateType.HasFlag(UpdateType.Media) && !context.User.IsAdmin)
            {
                if (_stateAcceptModels)
                    await scope.ServiceProvider.GetRequiredService<LoadMetrics>().Run(context);
                else
                    await context.Send("В данный момент приём работ для оценивания отключен. Подождите немного возможно ведуться технические работы.");
            }
            else
            {
                var btnNull = context.Client.GetIndexButton(context, Commands);
                if (btnNull == null)
                {
                    await context.Send("Извини, 😞 я тебя не понял! 🤔");
                    return;
                }
                var btn = (ButtonSearch)btnNull;
                if (btn.Row == 0)
                {
                    if (btn.Column == 0)
                    {
                        await GetRatingUser(context, scope.ServiceProvider);
                    }
                    else
                    {
                        Func<int, bool> predict = _ => true;
                        if (!context.User.IsAdmin) predict = (command) => command == context.User.CommandId;
                        MemoryStream stream = new();
                        StreamWriter writer = new(stream, Encoding.UTF8);
                        GetCSVTable(predict, writer.WriteLine, context.User.IsAdmin, scope.ServiceProvider);
                        writer.Flush();
                        MediaSource mediaSource = new(() => Task.FromResult<Stream>(stream))
                        {
                            Name = "fullRating.csv",
                            Type = ".csv",
                            MimeType = "text/csv",
                        };
                        stream.Position = 0;
                        await context.Send(new()
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
                        await context.Send(new SendModel() { Message = _helpInfoText.Replace(KeyInsertId, context.User.Id.ToString()) }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
                    }
                    else // Code
                    {
                        await context.Send(new SendModel()
                        {
                            Message = $"```python\n{File.ReadAllText(configuration[LoadMetrics.KeyPathScript]!)}\n```"
                        }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
                    }
                }
                else
                {
                    await SendMetrics(context, scope.ServiceProvider);
                }
            }
        }

        private async Task<bool> CheckRegister(UpdateContext<User> context, IServiceProvider serviceProvider)
        {
            if (!(context.User.IsStarted || context.User.IsAdmin))
            {
                if (_isFilterUsers) return true;
                SendModel sendingClient = string.Empty;
                sendingClient.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown);
                foreach (var message in HelloMessage)
                {
                    sendingClient.Message += message.Replace(KeyLinkSurvey, _linkSurvey).Replace(KeyInsertId, context.User.Id.ToString()) + "\n";
                    await context.Send(sendingClient);
                    Thread.Sleep(WaitStartMessage);
                }
                if (context.Update.OriginalMessage is Telegram.Bot.Types.Update up && !string.IsNullOrWhiteSpace(up.Message?.From?.Username))
                {
                    context.User.Nickname = up.Message.From.Username;
                    using var db = serviceProvider.GetRequiredService<DataBase>();
                    db.Users.Update(context.User);
                    await db.SaveChangesAsync();
                }
                return true;
            }
            return false;
        }

        private async Task ApplayCommands(UpdateContext<User> context, IServiceProvider serviceProvider)
        {
            SendModel send = string.Empty;
            using var streamFile = new StreamReader(await context.Update.Medias![0].GetStream());
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
                    await context.Send(send);
                    continue;
                }
                else
                {
                    send.Message += $"🎉 Ура! 🎉 К команде {commandName} присоединился новый участник — пользователь [{id}]!\n";
                    await context.Send(send);
                }
                if (user.IsAdmin)
                {
                    send.Message += $"⚠️ Администратор не может принять роль участника [{id}]. Пожалуйста, проверьте настройки. 🤔\n";
                    await context.Send(send);
                    continue;
                }
                var oldIdComman = user.CommandId;
                user.CommandId = command.Id;
                user.Name = userName;
                user.IsStarted = true;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                TgUser<User> tgUser = db.TgUsers.AsNoTracking().FirstOrDefault(x => x.User.Id == id)!;
                if (oldIdComman == null)
                {
                    await tgClient.Send(new SendModel()
                    {
                        Message = $"Ого! 🤩 Меня добавили в базу данных бота! 🥳 Теперь я официально часть системы! 🤖\n\n{_helpInfoText.Replace(KeyInsertId, context.User.Id.ToString())}",
                        Keyboard = Commands
                    }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown), tgUser);
                }
            }
            await db.SaveChangesAsync();
        }

        private static Task SendKeyboard(UpdateContext<User> context)
        {
            return context.Send(new SendModel()
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

        private static async Task GetRatingUser(UpdateContext<User> context, IServiceProvider serviceProvider)
        {
            using var db = serviceProvider.GetRequiredService<DataBase>();
            SendModel sending = "";
            int rating = 0;

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
            sending += "Рейтинг построен на метрике ROC AUC:\n\n";
            foreach (var command in commandInfo)
            {
                sending += $"{NumberToEmodji(rating++)} {command.commandName}";
                if (command.commandId == context.User.CommandId)
                    sending += "📍";
                sending += $"\n└> {command.metric}";
                sending+="\n";
            }
            if (string.IsNullOrEmpty(sending.Message)) sending.Message = "Таблица рейтинга пока пуста. 😕 Ждем первых участников! ⏳ Скоро здесь появятся лучшие результаты! ✨";
            await context.Send(sending);
        }

        private static Dictionary<char, string> _emodji = new()
        {
            { '0', "0️⃣" },
            { '1', "1️⃣" },
            { '2', "2️⃣" },
            { '3', "3️⃣" },
            { '4', "4️⃣" },
            { '5', "5️⃣" },
            { '6', "6️⃣" },
            { '7', "7️⃣" },
            { '8', "8️⃣" },
            { '9', "9️⃣" },
        };

        private static string NumberToEmodji(int value)
           => string.Join(string.Empty, value.ToString().Select(x => _emodji[x]));

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

        private async Task SendAllUsers(UpdateContext<User> context, string message, IServiceProvider serviceProvider)
        {
            using var db = serviceProvider.GetRequiredService<DataBase>();
            var tgClient = serviceProvider.GetRequiredService<TgClient<User, DataBase>>();
            int counUsers = 0;
            foreach (var tgUser in db.TgUsers.Include(x => x.User).AsNoTracking().Where(x => !x.User.IsAdmin))
            {
                if (tgUser.User.IsAdmin || !tgUser.User.IsStarted) continue;
                await tgClient.Send(new SendModel()
                {
                    Message = $"✉️ Сообщение от организаторов хакатона\n\n{message}",
                    Keyboard = Commands,
                    Medias = context.Update.Medias
                }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.MarkdownV2), tgUser);
                counUsers++;
            }
            await context.Send($"Готово отправлено {counUsers} сообщений");
        }

        private Task SendMetrics(UpdateContext<User> context, IServiceProvider serviceProvider)
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
            return context.Send(stringBuilder);
        }
    }
}
