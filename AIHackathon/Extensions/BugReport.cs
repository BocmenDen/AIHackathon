using BotCore.Tg;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

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

            return context.Reply(new SendModel()
            {
                Message = GetMessageBug(context, message, $"Место возникновения ошибки: https://github.com/BocmenDen/AIHackathon/blob/main/{relativePath}#L{lineNumber - 1}"),
                Medias = [ConstsShared.MediaError]
            });
        }

        public static Task ReplyBug(this UpdateContext context, Exception exception)
        {
            var info = FormatExceptionWithGitHubLink(exception);
            return context.Reply(new SendModel()
            {
                Message = GetMessageBug(context, exception.Message, info),
                Medias = [ConstsShared.MediaError]
            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.Markdown));
        }

        private static string GetMessageBug(BotCore.Interfaces.IUpdateContext<DB.Models.User> context, string? message, string info)
            => $"[{DateTime.UtcNow}] Извините произошла ошибка: {message}\n\nДанные пользователя:\n{context.User.GetInfoUser()}\n\n{info}\n\nПожалуйста, напишите в TG/VK: @bocmenden и опишите действия, которые привели к этому, а также пришлите данное сообщение для решения проблем";

        private static string FormatExceptionWithGitHubLink(Exception ex)
        {
            var st = new StackTrace(ex, true);
            StringBuilder sb = new();
            sb.AppendLine("Стек вызовов:");
            foreach (var frame in st.GetFrames())
            {
                var method = frame.GetMethod();
                if (method is null) continue;
                string? @namespace = method.DeclaringType?.Namespace;
                if (@namespace is null) continue;
                var repo = GetRepo(@namespace, out var correctPath);
                if (repo is null) continue;
                sb.Append('[');
                sb.Append("в ");
                sb.Append(GetFileNameForType(method.DeclaringType!.FullName));
                string? fileName = frame.GetFileName();
                if (fileName is not null)
                {
                    sb.Append(": строка ");
                    sb.Append(frame.GetFileLineNumber());
                }
                sb.Append("](");
                var subPath = correctPath(@namespace);
                if (fileName is not null)
                    sb.Append($"https://github.com/{repo}/blob/main/{subPath}/{Path.GetFileName(fileName)}#L{frame.GetFileLineNumber()}");
                else
                {
                    var className = GetFileNameForType(method.DeclaringType.FullName).Split('.').Last();
                    bool isCompilerGenerate = method.GetCustomAttribute<CompilerGeneratedAttribute>() != null || method.DeclaringType.GetCustomAttribute<CompilerGeneratedAttribute>() != null;
                    if (isCompilerGenerate)
                    {
                        sb.Append($"https://github.com/{repo}/blob/main/{subPath}/{className}.cs");
                    }
                    else
                    {
                        var methodName = GetFileNameForType(method.Name);
                        var methodVisibility = GetMethodVisibility(method);
                        sb.Append($"https://github.com/search?q=repo%3A{repo.Replace("/", "%2F")}+class+{className}+{methodVisibility}+{methodName}&type=code");
                        sb.Append($"{repo}{subPath}/{GetFileNameForType(method.DeclaringType.FullName).Split('.').Last()}.cs");
                    }
                }
                sb.Append("");
                sb.Append(")\n");
            }
            return sb.ToString();
        }

        private static string GetMethodVisibility(MethodBase methodInfo)
        {
            if (methodInfo.IsPublic)
                return "public";
            else if (methodInfo.IsPrivate)
                return "private";
            else if (methodInfo.IsFamily)
                return "protected";
            else if (methodInfo.IsAssembly)
                return "internal";
            else if (methodInfo.IsFamilyOrAssembly)
                return "private protected";
            else if (methodInfo.IsFamilyAndAssembly)
                return "protected internal";
            else
                return "unknown";
        }

        private static string GetFileNameForType(string? typeName)
        {
            if (typeName == null) return string.Empty;
            var index = typeName.IndexOfAny(['`', '+', '<']);
            if (index > 0)
                typeName = typeName[..index];
            return typeName;
        }

        private static string? GetRepo(string @namespace, out Func<string, string> getPath)
        {
            getPath = (s) => s.Replace('.', '/');
            string[] namespacesBotCore = [
                "BotCore.EfUserDb",
                "BotCore.FilterRouter",
                "BotCore.OneBot",
                "BotCore.PageRouter",
                "BotCore.SpamBroker",
                "BotCore.Tg",
                "BotCore",
                "BotCoreGenerator.PageRouter.Mirror"
                ];
            if (namespacesBotCore.Any(@namespace.StartsWith))
            {
                getPath = (path) =>
                {
                    var findNamespace = namespacesBotCore.First(@namespace.StartsWith);
                    var result = findNamespace + path.Replace(findNamespace, string.Empty).Replace('.', '/');
                    return result;
                };
                return "BocmenDen/BotCore";
            }
            if (@namespace.StartsWith("AIHackathon"))
                return "BocmenDen/AIHackathon";
            return null;
        }
    }
}