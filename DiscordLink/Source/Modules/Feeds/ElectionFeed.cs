using Eco.Gameplay.Civics.Elections;
using Eco.Moose.Extensions;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionFeed : FeedModule
    {
        public override string ToString()
        {
            return "Election Feed";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.ElectionStarted | DlEventType.ElectionStopped;
        }

        protected override async Task<bool> ShouldRun()
        {
            foreach (ChannelLink link in DiscordLinkConfig.ElectionFeedChannels)
            {
                if (link.IsValid())
                    return true;
            }
            return false;
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            if (!(data[0] is Election election))
                return;

            DiscordLinkEmbed embed = new DiscordLinkEmbed();
            switch (trigger)
            {
                case DlEventType.ElectionStarted:
                    embed.WithTitle($":ballot_box:  {election.Creator.GetTagStrippedName()} Started An Election :ballot_box: ");
                    embed.AddField("Title", election.Name.StripTags(), inline: true);
                    embed.AddField("Process", election.Process.Name.StripTags(), inline: true);
                    embed.AddField("Time", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);
                    break;

                case DlEventType.ElectionStopped:
                    ElectionResult results = election.CurrentResults;
                    embed.WithTitle($":ballot_box:  Election Has Ended  :ballot_box: ");
                    embed.AddField("Title", election.Name.StripTags());
                    if (results.Vetoed)
                    {
                        embed.AddField("Result", "Vetoed", inline: true);
                        embed.AddField("Vetoer", results.Vetoer.GetTagStrippedName(), inline: true);
                        embed.AddField("Time left when vetoed", TimeFormatter.FormatSpan(election.TimeLeft), inline: true);
                    }
                    else
                    {
                        if (results.Tied)
                        {
                            embed.AddField("Result", "Tie - No action taken", inline: true);
                        }
                        else if (election.BooleanElection)
                        {
                            embed.AddField("Result", results.Passed ? "Passed" : "Failed", inline: true);
                            embed.AddField("Votes", $"For - **{results.YesVotes}**\nAgainst - **{results.NoVotes}**", inline: true);
                        }
                        else
                        {
                            bool singleWinner = results.WinningUsers.Length == 1;
                            string title;
                            string winningUsers;
                            if (singleWinner)
                            {
                                title = "Winner";
                                winningUsers = results.WinningUsers[0].GetTagStrippedName();
                            }
                            else
                            {
                                title = "Winners";
                                winningUsers = string.Join("\n", (object[])results.WinningUsers).StripTags();
                            }
                            embed.AddField(title, winningUsers, inline: true);
                        }
                    }
                    break;

                default:
                    Logger.Debug("Election Feed received unexpected trigger type");
                    return;
            }

            foreach (ChannelLink electionLink in DiscordLinkConfig.ElectionFeedChannels)
            {
                if (!electionLink.IsValid())
                    continue;

                await DiscordLink.Obj.Client.SendMessageAsync(electionLink.Channel, null, embed);
                ++_opsCount;
            }
        }
    }
}
