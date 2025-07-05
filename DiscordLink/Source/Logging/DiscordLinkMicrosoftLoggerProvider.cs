using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace Eco.Plugins.DiscordLink.Logging
{
    public class DiscordLinkMicrosoftLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, DSharpPlusLogWrapper> loggers = new();
        private readonly IDisposable onChangeToken;
        private DSharpPlusLogWrapperConfiguration currentConfig;

        public DiscordLinkMicrosoftLoggerProvider(IOptionsMonitor<DSharpPlusLogWrapperConfiguration> config)
        {
            this.currentConfig = config.CurrentValue;
            this.onChangeToken = config.OnChange(updatedConfig => this.currentConfig = updatedConfig);
        }

        public ILogger CreateLogger(string categoryName)
            => this.loggers.GetOrAdd(categoryName, name => new DSharpPlusLogWrapper());

        private DSharpPlusLogWrapperConfiguration GetCurrentConfig() => this.currentConfig;

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.loggers.Clear();
                this.onChangeToken?.Dispose();
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
