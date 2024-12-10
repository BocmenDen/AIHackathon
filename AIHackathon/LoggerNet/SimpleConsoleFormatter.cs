// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace AIHackathon.LoggerNet
{
    internal sealed class SimpleConsoleFormatter : ConsoleFormatter, IDisposable
    {
        internal const string NameFormatter = "myFormatter";

        private const string LoglevelPadding = ": ";
        private static readonly string _messagePadding = new(' ', GetLogLevelString(LogLevel.Information).Length + LoglevelPadding.Length);
        private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
#if NET
        private static bool IsAndroidOrAppleMobile => OperatingSystem.IsAndroid() ||
                                                      OperatingSystem.IsTvOS() ||
                                                      OperatingSystem.IsIOS(); // returns true on MacCatalyst
#else
        private static bool IsAndroidOrAppleMobile => false;
#endif
        private readonly IDisposable? _optionsReloadToken;

        public SimpleConsoleFormatter(IOptionsMonitor<SimpleConsoleFormatterOptions> options)
            : base(NameFormatter)
        {
            ReloadLoggerOptions(options.CurrentValue);
            _optionsReloadToken = options.OnChange(ReloadLoggerOptions);
        }

        [MemberNotNull(nameof(FormatterOptions))]
        private void ReloadLoggerOptions(SimpleConsoleFormatterOptions options)
        {
            FormatterOptions = options;
        }

        public void Dispose()
        {
            _optionsReloadToken?.Dispose();
        }

        internal SimpleConsoleFormatterOptions FormatterOptions { get; set; }

        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
        {
            if (logEntry.State is BufferedLogRecord bufferedRecord)
            {
                string message = bufferedRecord.FormattedMessage ?? string.Empty;
                WriteInternal(null, textWriter, message, bufferedRecord.LogLevel, bufferedRecord.EventId.Id, bufferedRecord.EventId.Name, bufferedRecord.Exception, logEntry.Category, GetCurrentDateTime());
            }
            else
            {
                string message = logEntry.Formatter(logEntry.State, logEntry.Exception);
                if (logEntry.Exception == null && message == null)
                {
                    return;
                }

                // We extract most of the work into a non-generic method to save code size. If this was left in the generic
                // method, we'd get generic specialization for all TState parameters, but that's unnecessary.
                WriteInternal(scopeProvider, textWriter, message, logEntry.LogLevel, logEntry.EventId.Id, logEntry.EventId.Name, logEntry.Exception?.ToString(), logEntry.Category, GetCurrentDateTime());
            }
        }

        private void WriteInternal(IExternalScopeProvider? scopeProvider, TextWriter textWriter, string message, LogLevel logLevel,
            int eventId, string? eventName, string? exception, string category, DateTimeOffset stamp)
        {
            ConsoleColors logLevelColors = GetLogLevelConsoleColors(logLevel);
            string logLevelString = GetLogLevelString(logLevel);

            string? timestamp = null;
            string? timestampFormat = FormatterOptions.TimestampFormat ?? "[yyyy-MM-dd HH:mm:ss] -> ";
            if (timestampFormat != null)
            {
                timestamp = stamp.ToString(timestampFormat);
            }
            if (timestamp != null)
            {
                textWriter.Write(timestamp);
            }
            if (logLevelString != null)
            {
                textWriter.Write(logLevelString);
            }

            bool singleLine = FormatterOptions.SingleLine;

            // Example:
            // info: ConsoleApp.Program[10]
            //       Request received

            // category and event id
            textWriter.Write(LoglevelPadding);
            textWriter.Write(category);
            textWriter.Write('[');

#if NET
            Span<char> span = stackalloc char[10];
            if (eventId.TryFormat(span, out int charsWritten))
                textWriter.Write(span[..charsWritten]);
            else
#endif
                textWriter.Write(eventId.ToString());

            if (eventName != null)
            {
                textWriter.Write(" -> ");
                textWriter.Write(eventName);
            }

            textWriter.Write(']');
            if (!singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }

            // scope information
            WriteScopeInformation(textWriter, scopeProvider, singleLine);
            WriteMessage(textWriter, message, singleLine);

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                WriteMessage(textWriter, exception, singleLine);
            }
            if (singleLine)
            {
                textWriter.Write(Environment.NewLine);
            }
        }

        private static void WriteMessage(TextWriter textWriter, string message, bool singleLine)
        {
            if (!string.IsNullOrEmpty(message))
            {
                if (singleLine)
                {
                    textWriter.Write(' ');
                    WriteReplacing(textWriter, Environment.NewLine, " ", message);
                }
                else
                {
                    textWriter.Write(_messagePadding);
                    WriteReplacing(textWriter, Environment.NewLine, _newLineWithMessagePadding, message);
                    textWriter.Write(Environment.NewLine);
                }
            }

            static void WriteReplacing(TextWriter writer, string oldValue, string newValue, string message)
            {
                string newMessage = message.Replace(oldValue, newValue);
                writer.Write(newMessage);
            }
        }

        private static DateTimeOffset GetCurrentDateTime()
        {
            return DateTimeOffset.UtcNow.AddHours(3);
        }

        private static string GetLogLevelString(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => "trce",
                LogLevel.Debug => "dbug",
                LogLevel.Information => "info",
                LogLevel.Warning => "warn",
                LogLevel.Error => "fail",
                LogLevel.Critical => "crit",
                _ => throw new ArgumentOutOfRangeException(nameof(logLevel))
            };
        }

        private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
        {
            // We shouldn't be outputting color codes for Android/Apple mobile platforms,
            // they have no shell (adb shell is not meant for running apps) and all the output gets redirected to some log file.
            bool disableColors = FormatterOptions.ColorBehavior == LoggerColorBehavior.Disabled ||
                FormatterOptions.ColorBehavior == LoggerColorBehavior.Default && (!ConsoleUtils.EmitAnsiColorCodes || IsAndroidOrAppleMobile);
            if (disableColors)
            {
                return new ConsoleColors(null, null);
            }
            // We must explicitly set the background color if we are setting the foreground color,
            // since just setting one can look bad on the users console.
            return logLevel switch
            {
                LogLevel.Trace => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Debug => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
                LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
                LogLevel.Warning => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
                LogLevel.Error => new ConsoleColors(ConsoleColor.Black, ConsoleColor.DarkRed),
                LogLevel.Critical => new ConsoleColors(ConsoleColor.White, ConsoleColor.DarkRed),
                _ => new ConsoleColors(null, null)
            };
        }

        private void WriteScopeInformation(TextWriter textWriter, IExternalScopeProvider? scopeProvider, bool singleLine)
        {
            if (FormatterOptions.IncludeScopes && scopeProvider != null)
            {
                bool paddingNeeded = !singleLine;
                scopeProvider.ForEachScope((scope, state) =>
                {
                    if (paddingNeeded)
                    {
                        paddingNeeded = false;
                        state.Write(_messagePadding);
                        state.Write("=> ");
                    }
                    else
                    {
                        state.Write(" => ");
                    }
                    state.Write(scope);
                }, textWriter);

                if (!paddingNeeded && !singleLine)
                {
                    textWriter.Write(Environment.NewLine);
                }
            }
        }

        private readonly struct ConsoleColors(ConsoleColor? foreground, ConsoleColor? background)
        {
            public ConsoleColor? Foreground { get; } = foreground;

            public ConsoleColor? Background { get; } = background;
        }
    }
}
