using OneBot.Attributes;
using OneBot.Interfaces;

namespace AIHackathon.Services
{
    [Service<ILogger>]
    public class Logger : ILogger
    {
        private readonly Dictionary<int, string> _namesServices = [];
        public void Log(string message, ILogger.LogTypes logTypes = ILogger.LogTypes.Info, int? senderId = null)
        {
            string? serviceName = senderId?.ToString();
            if (senderId != null && _namesServices.TryGetValue((int)senderId, out string? name)) serviceName = name;

            switch (logTypes)
            {
                case ILogger.LogTypes.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case ILogger.LogTypes.Error:
                    Console.ForegroundColor= ConsoleColor.DarkRed;
                    break;
            }
            Console.WriteLine($"[{logTypes}]({serviceName}) -> {message}");
            Console.ResetColor();
        }

        public void RegisterName(int sender, string name) => _namesServices[sender] = name;
    }
}
