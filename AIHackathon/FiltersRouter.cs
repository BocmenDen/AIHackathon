#pragma warning disable IDE0079 // Удалить ненужное подавление
#pragma warning disable CS8321  // Локальная функция объявлена, но не используется
#pragma warning disable IDE0051 // Удалите неиспользуемые закрытые члены

using AIHackathon.Extensions;

namespace AIHackathon
{
    public static class FiltersRouter
    {
        [CommandFilter("SendMeInfo")]
        private static Task SendMeInfo(UpdateContext context) => context.Reply(context.User.GetInfoUser());
    }
}
