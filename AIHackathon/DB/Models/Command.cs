using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class Command
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        /// <summary>
        /// Название команды
        /// </summary>
        public string Name { get; set; } = null!;
        /// <summary>
        /// Статус команды
        /// </summary>
        public StatusCommand StatusCommand { get; set; }
        /// <summary>
        /// Описание команды
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Должности (объявления)
        /// </summary>
        public string? Positions { get; set; }
        /// <summary>
        /// Навыки в объявлениях
        /// </summary>
        public string? AnnouncementSkills { get; set; }
        /// <summary>
        /// Время регистрации
        /// </summary>
        public DateTime RegistrationTime { get; set; }
        /// <summary>
        /// E-mail наставника
        /// </summary>
        public string? MentorEmail { get; set; }
    }

    public enum StatusCommand
    {
        /// <summary>
        /// Без статуса
        /// </summary>
        None,
        /// <summary>
        /// Формируется
        /// </summary>
        Forming,
        /// <summary>
        /// Сформировано
        /// </summary>
        Formed
    }
}