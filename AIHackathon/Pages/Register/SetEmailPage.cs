using AIHackathon.DB;
using BotCore.Services;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetEmailPage(ConditionalPooledObjectProvider<DataBase> db, HandlePageRouter pageRouter) : SetValuePageBase(pageRouter)
    {
        public const string Key = "SetEmailPage";

        protected override string MessageStart => "Пожалуйста, введите вашу почту";
        protected override string MessageNotCorrect => "Введённые данные почты не являются корректными";

        protected override bool IsCorrectValue(string value) => RegexEmail().IsMatch(value);

        protected override Task SaveValue(User user, string value)
        {
            return db.TakeObjectAsync(x =>
            {
                user.Email = value;
                x.Users.Update(user);
                return x.SaveChangesAsync();
            });
        }

        [GeneratedRegex("^\\S+@\\S+\\.\\S+$")]
        private static partial Regex RegexEmail();
    }
}
