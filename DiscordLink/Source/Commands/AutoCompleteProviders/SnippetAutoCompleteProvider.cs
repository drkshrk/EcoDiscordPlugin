using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Moose.Utils.TextUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class SnippetAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<string> snippets = DLStorage.Instance.Snippets.Keys;
            if (!string.IsNullOrEmpty(context.UserInput))
                snippets = snippets.OrderBy(snippet => TextUtils.CalculateStringDeviationScore(context.UserInput, snippet));
            else
                snippets = snippets.Order();

            snippets = snippets.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = snippets.Select(snippet => new DiscordAutoCompleteChoice(snippet, snippet));
            return ValueTask.FromResult(choices);
        }
    }
}
