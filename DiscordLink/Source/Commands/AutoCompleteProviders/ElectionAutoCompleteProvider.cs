using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Civics.Elections;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class ElectionAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<Election> elections = Lookups.ActiveElections;
            if (!string.IsNullOrEmpty(context.UserInput))
                elections = elections.OrderBy(election => TextUtils.CalculateStringDeviationScore(context.UserInput, election.Name.StripTags()));
            else
                elections = elections.OrderBy(election => election.TimeLeft).ThenBy(election => election.Name.StripTags());

            elections = elections.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = elections.Select(election => new DiscordAutoCompleteChoice(election.Name.StripTags(), election.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
