using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class Participant: SharedRegisterModelBase
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CommandId { get; set; }
        [ForeignKey(nameof(CommandId))]
        public Command Command { get; set; } = null!;

        public string? Name { get; set; }
        public string? MiddleName { get; set; }
    }
}
