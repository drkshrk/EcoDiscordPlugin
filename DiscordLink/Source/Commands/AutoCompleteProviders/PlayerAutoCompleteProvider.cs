using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Players;
using Eco.Moose.Extensions;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class PlayerAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<User> users = Lookups.Users;
            if (!string.IsNullOrEmpty(context.UserInput))
                users = users.OrderBy(user => TextUtils.CalculateStringDeviationScore(context.UserInput, user.Name.StripTags()));
            else
                users = users.OrderBy(user => user.IsActive).ThenBy(user => user.Name.StripTags());

            users = users.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = users.Select(user => new DiscordAutoCompleteChoice(user.GetTagStrippedName(), user.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
