using AIHackathon.DB.Models;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetEmailPage : SetValuePageBase
    {
        public const string Key = "SetEmailPage";

        protected override string MessageStart => "Пожалуйста, введите вашу почту";
        protected override string MessageNotCorrect => "Введённые данные почты не являются корректными";

        protected override bool IsCorrectValue(string? value) => value is not null && RegexEmail().IsMatch(value);
        protected override string? CorrectValue(string? value) => value?.ToLower();

        protected override void SaveValue(User user, string? value) => RegisterModel.Email = value;

        [GeneratedRegex("^\\S+@\\S+\\.\\S+$")]
        private static partial Regex RegexEmail();
    }
}
