using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class MetricParticipant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        public int ParticipantId { get; set; }
        [ForeignKey(nameof(ParticipantId))]
        public Participant? Participant { get; set; }
        public double Accuracy { get; set; }
        public string? Library { get; set; }
        public string? Error { get; set; }
        public DateTime DateTime { get; set; }
        public required string PathFile { get; set; }
        public required string FileHash { get; set; }
        public string? FileType { get; set; }

        public bool IsSuccess => string.IsNullOrWhiteSpace(Error);
    }
}
