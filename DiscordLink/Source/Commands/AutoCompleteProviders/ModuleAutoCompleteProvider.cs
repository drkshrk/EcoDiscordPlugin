using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Plugins.DiscordLink.Modules;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public class ModuleTargetAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            IEnumerable<Module> modules = DiscordLink.Obj.Modules;
            modules = modules.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = modules.Select(module => new DiscordAutoCompleteChoice(module.ToString(), module.ToString()));
            return ValueTask.FromResult(choices);
        }
    }
}
