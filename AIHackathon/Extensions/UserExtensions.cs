using AIHackathon.DB.Models;

namespace AIHackathon.Extensions
{
    public static class UserExtensions
    {
        private const string NotSetDataMessage = "Нет данных";

        public static string GetInfoUser(this User user) => string.Format
(
"""
📌 Участник #{0}
👤 Telegram: @{1}
📝 Фамилия: {2}
📝 Имя: {3}
📝 Отчество: {4}
✉️ Email: {5}
📞 Телефон: {6}
👥 Команда: {7}
""", user.Id, user.TgChat.Username, user.Participant?.Surname ?? NotSetDataMessage, user.Participant?.Name ?? NotSetDataMessage, user.Participant?.MiddleName ?? NotSetDataMessage, user.Participant?.Email ?? NotSetDataMessage, user.Participant?.Phone ?? NotSetDataMessage, user.Participant?.Command?.Name ?? NotSetDataMessage
);
    }
}
