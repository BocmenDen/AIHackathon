using System.Runtime.CompilerServices;

namespace AIHackathon.Extensions
{
    public static class BugReport
    {
        public static Task ReplyBug(this UpdateContext context,
            string? message = null,
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            string relativePath = filePath[filePath.LastIndexOf(nameof(AIHackathon))..].Replace("\\", "/");

            context.Reply($"[{DateTime.UtcNow}] Извините произошла неизвестная ошибка: {message}\n{context.User.GetInfoUser()}\n\nМесто возникновения ошибки: https://github.com/BocmenDen/AIHackathon/blob/main/{relativePath}#L{lineNumber}\n\nПожалуйста, напишите в TG/VK: @bocmenden и опишите действия, которые привели, а также пришлите данное сообщение для решения проблем");

            return Task.CompletedTask;
        }
    }
}