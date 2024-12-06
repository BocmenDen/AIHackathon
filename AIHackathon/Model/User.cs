using OneBot.Base;

namespace AIHackathon.Model
{
    public class User : BaseUser
    {
        public bool IsStarted { get; set; }
        public int? CommandId { get; set; } = null;
        public bool IsAdmin { get; set; } = false;
        public string Name { get; set; } = null!;
        public string? Nickname { get; set; }

        public User(int id, bool isStarted, int commandId, bool isAdmin, string name, string nickname) : base(id)
        {
            IsStarted=isStarted;
            CommandId=commandId;
            IsAdmin=isAdmin;
            Name=name;
            Nickname=nickname;
        }

        public User() { }

        public override BaseUser CreateEmpty() => new User();

        public override string ToString() => $"Id: {Id} Nickname: {Nickname} Name: {Name}";
    }
}
