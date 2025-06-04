using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Gameplay.Economy;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class CurrencyAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<Currency> currencies = Lookups.Currencies;
            if (!string.IsNullOrEmpty(context.UserInput))
                currencies = currencies.OrderBy(currency => TextUtils.CalculateStringDeviationScore(context.UserInput, currency.Name.StripTags()));
            else
                currencies = currencies.OrderByDescending(currency => currency.Backed).ThenByDescending(currency => Moose.Plugin.MooseStorage.WorldData.CurrencyToTradeCountMap.GetOrDefault(currency.Id)).ThenBy(currency => currency.Name.StripTags());

            currencies = currencies.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = currencies.Select(currency => new DiscordAutoCompleteChoice(currency.Name, currency.Id.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
