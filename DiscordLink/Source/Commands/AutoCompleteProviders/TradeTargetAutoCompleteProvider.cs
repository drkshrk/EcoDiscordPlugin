using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Commands.Processors.SlashCommands.ArgumentModifiers;
using DSharpPlus.Entities;
using Eco.Moose.Extensions;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Moose.Utils.TextUtils;
using Eco.Shared.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Eco.Moose.Data.Enums;

namespace Eco.Plugins.DiscordLink
{
    public class TradeTargetAutoCompleteProvider : IAutoCompleteProvider
    {
        public ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext context)
        {
            List<string> tradeTargetList = new List<string>();
            foreach(LookupTypes lookupType in Enum.GetValues(typeof(LookupTypes)).Cast<LookupTypes>())
            {
                if (!Moose.Data.Constants.TRADE_LOOKUP_MASK.HasFlag(lookupType))
                    continue;

                switch(lookupType)
                {
                    case LookupTypes.None:
                        break;

                    case LookupTypes.Item:
                        tradeTargetList.AddUniqueRange(Lookups.Items.Select(item => item.DisplayName.ToString()));
                        break;

                    case LookupTypes.Tag:
                        tradeTargetList.AddUniqueRange(Lookups.Tags.Select(tag => tag.DisplayName.ToString()));
                        break;

                    case LookupTypes.User:
                        tradeTargetList.AddUniqueRange(Lookups.Users.Select(user => user.GetTagStrippedName()));
                        break;

                    case LookupTypes.Store:
                        tradeTargetList.AddUniqueRange(Lookups.StoresComponents.Select(store => store.Parent.GetTagStrippedName()));
                        break;

                    default:
                        Logger.DebugError($"TradeTargetAutoCompleteProvider received unknown LookupType. LookupType = {lookupType}");
                        break;
                }
            }

            IEnumerable<string> tradeTargets = tradeTargetList;
            if (!string.IsNullOrEmpty(context.UserInput))
                tradeTargets = tradeTargets.OrderBy(target => TextUtils.CalculateStringDeviationScore(context.UserInput, target));
            else
                tradeTargets = tradeTargets.Order();

            tradeTargets = tradeTargets.Take(DLConstants.DISCORD_AUTOCORRECT_CHOICE_COUNT_LIMIT); // Avoid triggering warnings about unsupported amount of choices
            IEnumerable<DiscordAutoCompleteChoice> choices = tradeTargets.Select(target => new DiscordAutoCompleteChoice(target, target));
            return ValueTask.FromResult(choices);
        }
    }
}
