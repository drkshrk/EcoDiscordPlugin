using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Eco.Plugins.DiscordLink
{
    internal sealed class DSharpPlusLogWrapperConfigurationOptions : ConfigureOptions<LoggerFilterOptions>
    {
        public DSharpPlusLogWrapperConfigurationOptions(LogLevel level) : base(options => options.MinLevel = level)
        {
        }
    }
}
