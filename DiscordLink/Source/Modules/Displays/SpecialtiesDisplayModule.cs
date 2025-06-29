using Eco.Moose.Data;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public class SpecialtiesDisplayModule : DisplayModule
    {
        public override string ToString() => "Skill Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.GainedSpecialty
            | DlEventType.LostSpecialty | DlEventType.LeveledUpSpecialty;
        public override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DiscordLinkConfig.SkillDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();

            if (!(target is SpecialtiesChannelLink skillTarget))
                return;

            CommandData.SpecialtyAssignmentData specialtyData = Moose.Features.Skills.GetPlayerSpecialtyData(null, includeScrollNoStar: skillTarget.IncludeScrollNoStar);
            DiscordLinkEmbed report = MessageBuilder.Discord.GetSpecialtiesReport(specialtyData, skillTarget.IncludeScrollNoStar, skillTarget.IncludeInactive, null);
            displayContent.Add(new DisplayContent(embedContent: report));
        }
    }
}
