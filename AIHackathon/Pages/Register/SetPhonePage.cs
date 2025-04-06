using AIHackathon.DB;
using BotCore.Services;
using PhoneNumbers;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetPhonePage(ConditionalPooledObjectProvider<DataBase> db, HandlePageRouter pageRouter) : SetValuePageBase(pageRouter)
    {
        public const string Key = "SetPhonePage";

        private readonly static PhoneNumberUtil PhoneUtil = PhoneNumberUtil.GetInstance();

        protected override string MessageStart => "Введите номер телефона";
        protected override string MessageNotCorrect => "Введённые данные номера телефона не являются корректными";

        protected override bool IsCorrectValue(string value) => GetValidatorNumber().IsMatch(value) && PhoneUtil.IsValidNumber(PhoneUtil.Parse(value, ConstsShared.DefaultRegion));

        protected override Task SaveValue(User user, string value) => db.TakeObjectAsync(x =>
        {
            user.PhoneNumber = PhoneUtil.Format(PhoneUtil.Parse(value, ConstsShared.DefaultRegion), PhoneNumberFormat.E164);
            x.Users.Update(user);
            return x.SaveChangesAsync();
        });
        [GeneratedRegex("^\\+?[1-9][0-9]{7,14}$")]
        private static partial Regex GetValidatorNumber();
    }
}
