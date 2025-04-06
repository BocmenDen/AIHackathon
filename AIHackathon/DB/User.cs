using BotCore.Interfaces;
using BotCore.Tg;
using Telegram.Bot.Types;

namespace AIHackathon.DB
{
    public class User : IUser, IUserTgExtension
    {
        public long Id { get; set; }
        public Chat TgChat { get; set; } = null!;
        public string? KeyPage { get; set; }
        public string? ParameterPage { get; set; }
        public string? ModelPage { get; set; }

        public string? Surname { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public string? PhoneNumber { get; set; } = null!;

        public Chat GetTgChat() => TgChat;
    }
}
