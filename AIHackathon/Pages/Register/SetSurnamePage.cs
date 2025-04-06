using AIHackathon.DB;
using BotCore.Services;

namespace AIHackathon.Pages.Register
{
    [PageCacheable(Key)]
    public partial class SetSurnamePage(ConditionalPooledObjectProvider<DataBase> db, HandlePageRouter pageRouter) : SetValuePageBase(pageRouter)
    {
        public const string Key = "SetSurnamePage";

        protected override string MessageStart => "Пожалуйста, введите вашу фамилию";
        protected override string MessageNotCorrect => "Введённые данные фамилии не являются корректными";

        protected override bool IsCorrectValue(string value) => !string.IsNullOrWhiteSpace(value) && value.All(char.IsLetter);

        protected override Task SaveValue(User user, string value) => db.TakeObjectAsync(x =>
        {
            user.Surname = value;
            x.Users.Update(user);
            return x.SaveChangesAsync();
        });
    }
}
