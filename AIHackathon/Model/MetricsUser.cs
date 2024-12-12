using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AIHackathon.Model
{
    public class MetricsUser
    {
        [Key]
        public int MetricId { get; set; }
        public int UserId { get; set; }
        [JsonProperty("accuracy")]
        public double Accuracy { get; set; }
        [JsonProperty("roc_auc")]
        public double ROC_AUC { get; set; }
        [JsonProperty("library")]
        public string? Library { get; set; }
        public string PathFile { get; set; }
        public DateTime DateTime { get; set; }
        [JsonProperty("error")]
        public string? Error { get; set; }
        [JsonProperty("columns")]
        public string? Columns { get; set; }

        public bool IsSuccess => string.IsNullOrWhiteSpace(Error);

        public MetricsUser(int metricId, int userId, double accuracy, string library, string pathFile, DateTime dateTime)
        {
            MetricId=metricId;
            UserId =userId;
            Accuracy=accuracy;
            Library=library??throw new ArgumentNullException(nameof(library));
            PathFile=pathFile??throw new ArgumentNullException(nameof(pathFile));
            DateTime=dateTime;
        }

        public MetricsUser()
        {
            DateTime = DateTime.UtcNow;
            PathFile = null!;
        }

        public override string ToString() => IsSuccess ? ROC_AUC switch
        {
            double v when v == 1 => $"🚀 ROC AUC: {ROC_AUC}   Космические результаты!",
            double v when v >= 0.9 => $"🚀 ROC AUC: {ROC_AUC}   Попробуй ещё улучшить!",
            double v when v >= 0.8 => $"👍 ROC AUC: {ROC_AUC}.  Достаточно хороший результат, есть куда расти! 📈",
            double v when v >= 0.7 => $"🤔 ROC AUC: {ROC_AUC}.  Неплохо, но можно улучшить!  Попробуем другие параметры. ⚙️",
            _ => $"😢 ROC AUC: {ROC_AUC}.  Результат не очень...  Нужно серьезно пересмотреть модель. ⚠️",
        } : $"Ой! 💥 Произошла ошибка: {Error}";
    }
}
