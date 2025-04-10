using BotCore.Interfaces;
using BotCore.Tg;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Telegram.Bot.Types;

namespace AIHackathon.DB.Models
{
    public class User : IUser, IUserTgExtension
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public int? ParticipantId { get; set; }
        [ForeignKey(nameof(ParticipantId))]
        public Participant? Participant { get; set; }

        public Chat TgChat { get; set; } = null!;
        public string? KeyPage { get; set; }
        public string? ParameterPage { get; set; }
        public string? ModelPage { get; set; }

        public bool IsRegister => ParticipantId is not null;

        public Chat GetTgChat() => TgChat;
    }
}
