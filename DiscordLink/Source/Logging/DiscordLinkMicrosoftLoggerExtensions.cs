using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using System;

namespace Eco.Plugins.DiscordLink.Logging
{
    public static class DiscordLinkMicrosoftLoggerExtensions
    {
        public static ILoggingBuilder AddDiscordLinkMicrosoftLogger(this ILoggingBuilder builder)
        {
            builder.AddConfiguration();
            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, DiscordLinkMicrosoftLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions<DSharpPlusLogWrapperConfiguration, DiscordLinkMicrosoftLoggerProvider>(builder.Services);
            return builder;
        }

        public static ILoggingBuilder AddDiscordLinkMicrosoftLogger(this ILoggingBuilder builder, Action<DSharpPlusLogWrapperConfiguration> configure)
        {
            builder.AddDiscordLinkMicrosoftLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
