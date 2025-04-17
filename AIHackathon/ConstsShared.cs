using BotCore.FilterRouter.Attributes;

namespace AIHackathon
{
    public static class ConstsShared
    {
        public const string ResourceButtonsMain = nameof(ResourceButtonsMain);

        public const string ScriptCommandKeras = "getKerasSaveModelScript";
        public const string ScriptCommandSklearn = "getSklearnSaveModelScript";
        public const string ScriptCommandXGBoost = "getXGBoostSaveModelScript";
        public const string ScriptCommandAuto = "getAutoSaveModelScript";
        public const string ScriptCommandDefault = "getDefaultSaveModelScript";

        public readonly static ButtonSend ButtonYes = "Да";
        public readonly static ButtonSend ButtonNo = "Нет";

        public readonly static string DefaultRegion = "RU";

        public readonly static ButtonSend ButtonUpdate = "🔄";
        public readonly static ButtonsSend ButtonsUpdate = new([[ButtonUpdate]]);

        public readonly static ButtonSend ButtonOpenMainPage = "🏠 Главная";
        public readonly static ButtonSend ButtonOpenRatingPage = "🌟 Рейтинг";
        public readonly static ButtonSend ButtonOpenInfo = "ℹ️ Справка";
        public readonly static ButtonSend ButtonOpenNews = "📰 Новости";

        [ResourceKey(ResourceButtonsMain)]
        public readonly static ButtonsSend ButtonsMain = new([[ButtonOpenRatingPage, ButtonOpenMainPage], [ButtonOpenInfo, ButtonOpenNews]]);

        public readonly static MediaSource MediaError = MediaSource.FromUri("https://media.tenor.com/8ND8TbjZqh0AAAAi/error.gif");

        public static string GetPathResource(string subPath) => Path.GetFullPath(Path.Combine("Resources", subPath));
    }
}