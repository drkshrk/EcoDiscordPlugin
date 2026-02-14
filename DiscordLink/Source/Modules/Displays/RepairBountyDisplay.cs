using Eco.Moose.Data;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class RepairBountyDisplay : DisplayModule
    {
        protected override int TimerUpdateIntervalMs { get { return 60000; } }
        protected override int TimerStartDelayMs { get { return 20000; } }
        public override string ToString() => "Repair Bounty Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.ClaimedRepairBounty | DlEventType.Timer;
        public override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DiscordLinkConfig.RepairBountyDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();

            if (!(target is RepairBountyChannelLink repairBountyTarget))
                return;


            RepairBountyLookupResult lookupData = Moose.Features.WorldObjects.LookupRepairBounties(includeInactive: false);
            DiscordLinkEmbed report = MessageBuilder.Discord.GetRepairBountiesReport(lookupData);
            displayContent.Add(new DisplayContent(embedContent: report));
        }
    }
}
