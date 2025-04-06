using AIHackathon.DB;

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
✉️ Email: {3}
📞 Телефон: {4}
""", user.Id, user.TgChat.Username, user.Surname ?? NotSetDataMessage, user.Email ?? NotSetDataMessage, user.PhoneNumber ?? NotSetDataMessage
);

        public static bool IsRegister(User user) => !string.IsNullOrEmpty(user.Surname) &&
                                                    !string.IsNullOrEmpty(user.Email) &&
                                                    !string.IsNullOrEmpty(user.PhoneNumber);
    }
}
