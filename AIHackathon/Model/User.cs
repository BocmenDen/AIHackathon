using OneBot.Base;

namespace AIHackathon.Model
{
    public class User : BaseUser
    {
        public bool IsStarted { get; set; }
        public int? CommandId { get; set; } = null;
        public bool IsAdmin { get; set; } = false;
        public string Name { get; set; } = null!;

        public User(int id, bool isStarted, int commandId, bool isAdmin, string name) : base(id)
        {
            IsStarted=isStarted;
            CommandId=commandId;
            IsAdmin=isAdmin;
            Name=name;
        }

        public User() { }

        public override BaseUser CreateEmpty() => new User();
    }
}
