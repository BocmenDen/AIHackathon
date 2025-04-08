using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class RatingInfo<TSubject> where TSubject: class, new()
    {
        [ForeignKey(nameof(SubjectId))]
        public TSubject Subject { get; set; } = null!;
        public int SubjectId { get; set; }
        public int Rating { get; set; }
        public int Position { get; set; }
        public double Metric { get; set; }
        public int CountMetric { get; set; }
    }
}
