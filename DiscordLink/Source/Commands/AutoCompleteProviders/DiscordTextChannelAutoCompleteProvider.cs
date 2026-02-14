using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Moose.Utils.TextUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class DiscordTextChannelAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<DiscordChannel> channels = DiscordLink.Obj.Client.GetChannelsOfType(DiscordChannelType.Text);
            if (!string.IsNullOrEmpty(context.UserInput))
                channels = channels.OrderBy(channel => TextUtils.CalculateStringDeviationScore(context.UserInput, channel.Name));
            else
                channels = channels.OrderBy(channel => channel.Position);

            channels = channels.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = channels.Select(channel => new DiscordAutoCompleteChoice(channel.Name, channel.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
