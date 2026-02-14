using Eco.Moose.Data;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesDisplay : DisplayModule
    {
        public override string ToString() => "Skill Display";
        // TODO: Limit demographic updates to entering or leaving the Active demographic
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.GainedSpecialty
            | DlEventType.LostSpecialty | DlEventType.LeveledUpSpecialty | DlEventType.EnteredDemographic | DlEventType.LeftDemographic;
        public override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DiscordLinkConfig.SkillDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();

            if (!(target is SpecialtiesChannelLink skillTarget))
                return;

            CommandData.SpecialtyAssignmentLookupResult specialtyData = Moose.Features.Skills.LookupSpecialtyAssignments(includeInactive: false);
            DiscordLinkEmbed report = MessageBuilder.Discord.GetSpecialtiesReport(specialtyData);
            displayContent.Add(new DisplayContent(embedContent: report));
        }
    }
}
