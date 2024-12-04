using System.ComponentModel.DataAnnotations;

namespace AIHackathon.Model
{
    public class Command
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }

        public Command(string name, int id)
        {
            Id=id;
            Name=name??throw new ArgumentNullException(nameof(name));
        }
        public Command(string name)
        {
            Name=name??throw new ArgumentNullException(nameof(name));
        }
    }
}
