using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Settlements;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class SettlementAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<Settlement> settlements = Lookups.ActiveSettlements;
            if (!string.IsNullOrEmpty(context.UserInput))
                settlements = settlements.OrderBy(settlement => TextUtils.CalculateStringDeviationScore(context.UserInput, settlement.Name.StripTags()));
            else
                settlements = settlements.OrderByDescending(settlement => settlement.SettlementType).ThenByDescending(settlement => settlement.Citizens.Where(citizen => citizen.IsActive).Count());

            settlements = settlements.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = settlements.Select(settlement => new DiscordAutoCompleteChoice(settlement.Name.StripTags(), settlement.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
