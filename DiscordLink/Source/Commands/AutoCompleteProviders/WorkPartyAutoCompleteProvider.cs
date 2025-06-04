using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Economy.WorkParties;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class WorkPartyAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<WorkParty> workParties = Lookups.ActiveWorkParties;
            if (!string.IsNullOrEmpty(context.UserInput))
                workParties = workParties.OrderBy(workParty => TextUtils.CalculateStringDeviationScore(context.UserInput, workParty.Name.StripTags()));
            else
                workParties = workParties.OrderByDescending(workParty => workParty.PercentDone).ThenBy(workParty => workParty.Name.StripTags());

            workParties = workParties.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = workParties.Select(workParty => new DiscordAutoCompleteChoice(workParty.Name, workParty.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
