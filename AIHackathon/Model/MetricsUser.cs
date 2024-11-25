using System.ComponentModel.DataAnnotations;

namespace AIHackathon.Model
{
    public class MetricsUser
    {
        [Key]
        public int MetricId { get; set; }
        public int UserId { get; set; }
        public double Accuracy { get; set; }
        public string Library { get; set; }
        public string PathFile { get; set; }
        public DateTime DateTime { get; set; }

        public MetricsUser(int metricId, int userId, double accuracy, string library, string pathFile, DateTime dateTime)
        {
            MetricId=metricId;
            UserId =userId;
            Accuracy=accuracy;
            Library=library??throw new ArgumentNullException(nameof(library));
            PathFile=pathFile??throw new ArgumentNullException(nameof(pathFile));
            DateTime=dateTime;
        }

        public MetricsUser(int userId, double accuracy, string library, string pathFile)
        {
            UserId=userId;
            Accuracy=accuracy;
            Library=library;
            PathFile=pathFile;
            DateTime = DateTime.Now;
        }
    }
}
