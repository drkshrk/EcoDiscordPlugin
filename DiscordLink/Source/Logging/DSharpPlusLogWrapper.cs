using Eco.Moose.Tools.Logger;
using Microsoft.Extensions.Logging;
using System;

namespace Eco.Plugins.DiscordLink.Logging
{
    /**
     * Implements the Microsoft Logging interface to call the MightyMoose Logger.
     */
    public class DSharpPlusLogWrapper : ILogger
    {
        public DSharpPlusLogWrapper() { }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel)
        => logLevel >= DiscordLinkConfig.BackendLogLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            string message = formatter(state, exception);
            if (exception != null)
            {
                Console.WriteLine(exception);
            }

            if (message == null || !IsEnabled(logLevel))
            {
                return;
            }

            string categoryName = eventId.Name != null ? $"[{eventId.Name}]" : null;
            string outputMessage = categoryName != null ? $"[DSharpPlus] {categoryName} {message}" : $"[DSharpPlus] {message}";

            switch (logLevel)
            {
                case LogLevel.Trace:
                    Logger.Trace(outputMessage);

                    if (DiscordLinkConfig.EnableTraceFileLogging)
                        Logger.Silent(outputMessage);
                    break;
                case LogLevel.Debug:
                    Logger.Debug(outputMessage);
                    break;
                case LogLevel.Information:
                    Logger.Info(outputMessage);
                    break;
                case LogLevel.Warning:
                    Logger.Warning(outputMessage);
                    break;
                case LogLevel.Error:
                    Logger.Error(outputMessage);
                    break;
                case LogLevel.Critical:
                    Logger.Error(outputMessage);
                    break;
                case LogLevel.None:
                    break;

                default:
                    Logger.Info(outputMessage);
                    break;
            }
        }
    }
}
