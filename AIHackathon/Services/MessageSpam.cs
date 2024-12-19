using AIHackathon.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneBot.Attributes;
using OneBot.Models;
using OneBot.SpamBroker;
using System.Text;

namespace AIHackathon.Services
{
    [Service]
    public class MessageSpam
    {
        private readonly SpamBroker<int, User> _spamFilter;
        private readonly SingleMessageQueue<int, User> _singleMessageFilter;
        private readonly BlackList<User> _blackList;
        private readonly ILogger _logger;
        private Func<UpdateContext<User>, Task>? _action;
        private readonly TimeSpan _banTime;

#pragma warning disable IDE0290 // Использовать основной конструктор
        public MessageSpam(
#pragma warning restore IDE0290 // Использовать основной конструктор
            IConfiguration configuration,
            ILogger<MessageSpam> logger,
            ILogger<SpamBroker<int, User>> loggerSpam,
            ILogger<SingleMessageQueue<int, User>> loggerSingleSpam,
            ILogger<BlackList<User>> blackListLogger
            )
        {
            _singleMessageFilter = new(
                (u) => u.User.Id,
                loggerSingleSpam
            );
            _spamFilter = new(
                (u) => u.User.Id,
                configuration.GetValue<int?>("spam_countMessage") ?? 5,
                configuration.GetValue<TimeSpan?>("spam_timeWindow") ?? TimeSpan.FromSeconds(3),
                loggerSpam
            );
            _blackList = new(blackListLogger);
            _logger=logger;
            _banTime=configuration.GetValue<TimeSpan?>("spam_timeBan") ?? TimeSpan.FromMinutes(5);
            // TODO автоочистка SpamBroker по таймеру
        }

        public void Init(Func<UpdateContext<User>, Task> action)
        {
            _action = action;
        }

        public async void HandleCommand(UpdateContext<User> context)
        {
            if (context.User.IsAdmin)
            {
                await HandleMessage(context);
                return;
            }
            if (_blackList.GetSpamState(context).IsSpam() ||
                (await _singleMessageFilter.CheckMessageSpamStatus(context, "Пожалуйста, подождите немного! ✨ Ваше сообщение обрабатывается… ⚙️")).IsSpam()
                ) return;
            var state = await _spamFilter.CheckMessageSpamStatus(context, $"Вы превысели {_spamFilter.MaxEvent} сообщений за {ConvertTimeSpan(_spamFilter.TimeWindow)}, выдан бан на {ConvertTimeSpan(_banTime)}");
            if (state == StateSpam.ForbiddenFirst)
                _blackList.AddBlock(context.User, _banTime);
            if (state.IsSpam()) return;
            _singleMessageFilter.RegisterEvent(context);
            await HandleMessage(context);
            _singleMessageFilter.UnregisterEvent(context);
        }

        private async Task HandleMessage(UpdateContext<User> context)
        {
            if (_action == null) return;
            try
            {
                await _action(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "При обработке сообщения [{message}] у пользователя [{user}] произошла ошибка", context, context.User);
                _ = context.Send($"Произошла ошибка при обработке сообщения {ex.Message}");
            }
        }

        private static string ConvertTimeSpan(TimeSpan timeSpan)
        {
            string readableTimeSpan = "";
            if (timeSpan.Days > 0)
                readableTimeSpan += timeSpan.Days + " дн. ";
            if (timeSpan.Hours > 0)
                readableTimeSpan += timeSpan.Hours + " ч. ";
            if (timeSpan.Minutes > 0)
                readableTimeSpan += timeSpan.Minutes + " мин. ";
            if (timeSpan.Seconds > 0)
                readableTimeSpan += timeSpan.Seconds + " сек.";
            return readableTimeSpan;
        }

        public StringBuilder GetMetrics()
        {
            StringBuilder stringBuilder = new();
            stringBuilder.AppendLine($"🚫 Черный список: {_blackList.Count} пользователей");
            _spamFilter.CleanupEmptyHistory(out int countElemInHistory);
            stringBuilder.AppendLine($"✉️ Активность: За последние {ConvertTimeSpan(_spamFilter.TimeWindow)} получено {countElemInHistory} сообщени(е/я/й)");
            var metric = _singleMessageFilter.GetMetric();
            stringBuilder.AppendLine($"⏱️ Очередь: Обрабатывается {metric.CountAll} сообщение(е/я/й) ({metric.CountForbiddenSpam} продолжают спамить)");
            return stringBuilder;
        }
    }
}
