namespace AIHackathon.Utils
{
    public static class SharedUtil
    {
        private readonly static MediaSource MediaMessageState = MediaSource.FromUri("https://media1.tenor.com/m/_28Wpe-HrfIAAAAC/nervous-spongebob.gif");
        public static async Task<T> WaitStep<T>(this UpdateContext context, Task<T> waitTask, Func<string> getMessageState)
        {
            await WaitStep(context, (Task)waitTask, getMessageState);
            return waitTask.Result;
        }
        public static async Task WaitStep(this UpdateContext context, Task waitTask, Func<string> getMessageState)
        {
            string waitLine = "";
            while (!waitTask.IsCompleted)
            {
                await context.Reply(new SendModel()
                {
                    Message = $@"
Пожалуйста, не выполняйте никаких действий, пока он не завершится процесс.
├> обновилось в: {DateTime.Now}
└> {getMessageState()} {waitLine}

Если сообщение перестанет обновляться, возможно, произошла перезагрузка бота. В таком случае попробуйте повторно отправить файлы или сообщите об этом разработчику: @BocmenDen.",
                    Medias = [MediaMessageState]
                });
                waitLine += "🐢";
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(2)), waitTask);
            }
            await waitTask;
        }
    }
}
