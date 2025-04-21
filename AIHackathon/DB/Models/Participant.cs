using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AIHackathon.DB.Models
{
    public class Participant
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CommandId { get; set; }
        [ForeignKey(nameof(CommandId))]
        public Command Command { get; set; } = null!;

        /// <summary>Фамилия</summary>
        public string Surname { get; set; } = null!;

        /// <summary>Имя</summary>
        public string Name { get; set; } = null!;

        /// <summary>Отчество</summary>
        public string MiddleName { get; set; } = null!;

        /// <summary>Пол</summary>
        public Gender Gender { get; set; }

        /// <summary>Регистрационный email</summary>
        public string Email { get; set; } = null!;

        /// <summary>Логин Telegram</summary>
        public string? Telegram { get; set; }

        /// <summary>Возраст</summary>
        public int Age { get; set; }

        /// <summary>Дата рождения</summary>
        public DateTime BirthDate { get; set; }

        /// <summary>Страна</summary>
        public string? Country { get; set; }

        /// <summary>Регион</summary>
        public string? Region { get; set; }

        /// <summary>Город</summary>
        public string? City { get; set; }

        /// <summary>Учебное заведение</summary>
        public string? University { get; set; }

        /// <summary>УЗ зарегистрирован на платформе</summary>
        public bool UniversityRegistered { get; set; }

        /// <summary>Тип учебного заведения</summary>
        public string? UniversityType { get; set; }

        /// <summary>Ступень образования</summary>
        public string? EducationLevel { get; set; }

        /// <summary>Факультет</summary>
        public string? Faculty { get; set; }

        /// <summary>Курс</summary>
        public int Course { get; set; }

        /// <summary>Класс</summary>
        public string? Grade { get; set; }

        /// <summary>Навыки</summary>
        public string? Skills { get; set; }

        /// <summary>Телефон</summary>
        public string Phone { get; set; } = null!;

        /// <summary>Немного о себе</summary>
        public string? About { get; set; }

        /// <summary>Разрешить отображать логин</summary>
        public bool AllowTelegramDisplay { get; set; }

        /// <summary>Показывать мои данные УЗ</summary>
        public bool ShowToUniversityRepresentative { get; set; }

        /// <summary>Наличие инвалидности</summary>
        public bool Disability { get; set; }

        /// <summary>Дата регистрации</summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>Дата последнего входа</summary>
        public DateTime LastLoginDate { get; set; }

        /// <summary>Согласие на рассылку</summary>
        public bool MailingConsent { get; set; }

        /// <summary>Размер одежды</summary>
        public string? ClothingSize { get; set; }

        /// <summary>Капитан</summary>
        public bool IsCaptain { get; set; }
    }

    public enum Gender
    {
        Man,
        Women
    }
}
