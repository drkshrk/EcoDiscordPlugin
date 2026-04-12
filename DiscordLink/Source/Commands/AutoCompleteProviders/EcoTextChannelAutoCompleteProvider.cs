using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Systems.Messaging.Chat.Channels;
using Eco.Moose.Extensions;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class EcoTextChannelAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<Channel> channels = Lookups.ChatChannels;
            if (!string.IsNullOrEmpty(context.UserInput))
                channels = channels.OrderBy(channel => TextUtils.CalculateStringDeviationScore(context.UserInput, channel.Name));
            else
                channels = channels.OrderByDescending(channel => channel.Users.Select(chatUser => chatUser.UserSet).Select(userSet => userSet.Where(user => user.IsActive).Count()))
                    .ThenBy(channel => channel.Users.Select(chatUser => chatUser.UserSet).Select(userSet => userSet.Select(user => user.GetTagStrippedName())));

            channels = channels.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = channels.Select(channel => new DiscordAutoCompleteChoice(channel.Name, channel.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}