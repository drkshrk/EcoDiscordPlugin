using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class LayerAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<string> layerNames = Lookups.VisibleLayers.Select(layer => layer.Name);
            if (!string.IsNullOrEmpty(context.UserInput))
                layerNames = layerNames.OrderBy(name => TextUtils.CalculateStringDeviationScore(context.UserInput, name));
            else
                layerNames = layerNames.Order();

            layerNames = layerNames.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = layerNames.Select(layerName => new DiscordAutoCompleteChoice(layerName, layerName));
            return ValueTask.FromResult(choices);
        }
    }
}
