using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class Plagiat
    {
        [Key]
        public int Id { get; set; }
        public DateTime DateTime { get; set; } = DateTime.UtcNow;
        public int ParticipantId { get; set; }
        [ForeignKey(nameof(ParticipantId))]
        public Participant Participant { get; set; } = null!;

        public int MetricPlagiatId { get; set; }
        [ForeignKey(nameof(MetricPlagiatId))]
        public MetricParticipant MetricPlagiat { get; set; } = null!;
    }
}
