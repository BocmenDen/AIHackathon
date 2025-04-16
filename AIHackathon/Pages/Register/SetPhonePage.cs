using AIHackathon.DB.Models;
using PhoneNumbers;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetPhonePage : SetValuePageBase
    {
        public const string Key = "SetPhonePage";

        private readonly static PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        protected override string MessageStart => "Введите номер телефона";
        protected override string MessageNotCorrect => "Введённые данные номера телефона не являются корректными";

        protected override bool IsCorrectValue(string? value) => value != null && GetValidatorNumber().IsMatch(value) && PhoneUtil.IsValidNumber(PhoneUtil.Parse(value, "RU"));
        protected override string? CorrectValue(string? value) => PhoneUtil.Format(PhoneUtil.Parse(value, "RU"), PhoneNumberFormat.E164);
        protected override void SaveValue(User user, string? value) => RegisterModel.Phone = value;
        [GeneratedRegex("^\\+?[1-9][0-9]{7,14}$")]
        private static partial Regex GetValidatorNumber();
    }
}
