using AIHackathon.Base;
using AIHackathon.DB.Models;
using AIHackathon.Extensions;
using AIHackathon.Models;
using BotCore.PageRouter.Interfaces;
using BotCore.Tg;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace AIHackathon.Pages
{
    [PageCacheable(Key)]
    public partial class PageInfo(IOptions<Settings> options) : PageBase, IPageLoading<User>
    {
        public const string Key = "PageInfo";

        private static Dictionary<string, (string, MediaSource[]?)> _infos = null!;

        protected string _chapter = options.Value.PageInfo.FirstOrDefault()?.Name!;
        private ButtonsSend? buttonsChapter;

        public override async Task HandleNewUpdateContext(UpdateContext context)
        {
            var searchBtn = buttonsChapter is null ? null : context.BotFunctions.GetIndexButton(context.Update, buttonsChapter);
            if (searchBtn is ButtonSearch resSearch)
            {
                var newChapter = resSearch.Button[Key].ToString()!;
                if (newChapter == _chapter)
                    return;
                _chapter = newChapter;
                GenerateButtons();
            }
            if (string.IsNullOrWhiteSpace(_chapter) || !_infos.TryGetValue(_chapter, out var sendData))
            {
                await context.ReplyBug("Не найден раздел для справки");
                return;
            }
            await context.Reply(new SendModel()
            {
                Message = sendData.Item1,
                Inline = buttonsChapter,
                Medias = sendData.Item2
            }.TgSetParseMode(Telegram.Bot.Types.Enums.ParseMode.MarkdownV2));
        }

        private void GenerateButtons()
        {
            buttonsChapter = new ButtonsSend(options.Value.PageInfo.Select<Chapter, IEnumerable<ButtonSend>>((x, i) => [new ButtonSend($"{(x.Name == _chapter ? "📌 " : "")}{i + 1} {x.Name}", [new KeyValuePair<string, object>(Key, x.Name)])]));
        }

        public static string ToMarkdownV2Escaped(string input) => RegexEscape().Replace(input, @"\$1");
        [GeneratedRegex(@"([\\.\-()#=!\[\]{}])")]
        private static partial Regex RegexEscape();

        public void PageLoading(User user)
        {
            GenerateButtons();
            if (_infos is not null) return;
            _infos = [];
            foreach (var chapter in options.Value.PageInfo)
            {
                var (mediasPath, text) = ExtractImagesAndMarkdown(File.ReadAllText(chapter.Path));
                string message = ToMarkdownV2Escaped(text);
                var pathToFile = Path.GetDirectoryName(Path.GetFullPath(chapter.Path))!;
                var medias = mediasPath.Count == 0 ? null : mediasPath.Select(x => MediaSource.FromFile(Path.GetFullPath(x, pathToFile))).ToArray();
                _infos.Add(chapter.Name, (message, medias));
            }
        }

        private static (List<string> imagePaths, string markdownText) ExtractImagesAndMarkdown(string input)
        {
            var imagePaths = new List<string>();

            // Извлекаем все пути из <img src="...">
            var imgTagRegex = ParseImg();
            var matches = imgTagRegex.Matches(input);

            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    imagePaths.Add(match.Groups[1].Value);
                }
            }

            // Удаляем все <img ...> теги
            string markdownText = imgTagRegex.Replace(input, "").Trim();

            return (imagePaths, markdownText);
        }

        [GeneratedRegex("<img[^>]*src=[\"']?([^\"'>]+)[\"']?[^>]*>", RegexOptions.IgnoreCase, "ru-RU")]
        private static partial Regex ParseImg();
    }
}
