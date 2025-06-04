using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Moose.Utils.TextUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class TradeFeedWatcherAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            LinkedUser user = UserLinkManager.LinkedUserByDiscordUser(context.Member);
            if (user == null)
                return new ValueTask<IEnumerable<DiscordAutoCompleteChoice>>();

            IEnumerable<string> feedWatchers = DLStorage.WorldData.TradeWatchers.GetValueOrDefault(context.Member.Id).
                Where(watcher => watcher.Type == Modules.ModuleArchetype.Display).Select(watcher => watcher.Key);
            if (!string.IsNullOrEmpty(context.UserInput))
                feedWatchers = feedWatchers.OrderBy(watcher => TextUtils.CalculateStringDeviationScore(context.UserInput, watcher));
            else
                feedWatchers = feedWatchers.Order();

            feedWatchers = feedWatchers.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = feedWatchers.Select(watcher => new DiscordAutoCompleteChoice(watcher, watcher));
            return ValueTask.FromResult(choices);
        }
    }
}
