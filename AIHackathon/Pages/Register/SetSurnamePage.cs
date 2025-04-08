using AIHackathon.DB.Models;
using System.Globalization;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetSurnamePage(HandlePageRouter pageRouter) : SetValuePageBase(pageRouter)
    {
        public const string Key = "SetSurnamePage";

        protected override string MessageStart => "Пожалуйста, введите вашу фамилию";
        protected override string MessageNotCorrect => "Введённые данные фамилии не являются корректными";

        protected override bool IsCorrectValue(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetter);

        protected override void SaveValue(User user, string? value) => RegisterModel.Surname = value;

        protected override string? CorrectValue(string? value) => value == null ? null : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.Trim().ToLower());
    }
}
