using DSharpPlus.Entities;
using Eco.Core.Utils;
using Eco.Gameplay.Civics.Elections;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.Lookups;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.DiscordLink.Utilities;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    class ElectionDisplay : DisplayModule
    {
        protected override int TimerUpdateIntervalMs { get { return 60000; } }
        protected override int TimerStartDelayMs { get { return 15000; } }

        public override string ToString() => "Election Display";
        protected override DlEventType GetTriggers() => base.GetTriggers() | DlEventType.DiscordClientConnected | DlEventType.Timer
            | DlEventType.Login | DlEventType.Vote | DlEventType.ElectionStarted | DlEventType.ElectionStopped;
        public override async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() => DiscordLinkConfig.ElectionDisplayChannels.Cast<DiscordTarget>();

        protected override void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent)
        {
            displayContent = new List<DisplayContent>();
            foreach (Election election in Lookups.ActiveElections)
            {
                DiscordLinkEmbed report = MessageBuilder.Discord.GetElectionReport(election);
                if (report.Fields.Count > 0)
                    displayContent.Add(new DisplayContent(embedContent: report, contentData: election.Id));
            }
        }

        protected async override Task PostDisplayCreated(IEnumerable<DiscordMessage> messages)
        {
            DiscordMessage message = messages.Last(); // Only put reactions on the last message
            Election election = GetElectionFromMessage(message);
            if (election != null && election.BooleanElection)
                await CreateVoteReactions(message);
        }

        protected async override Task HandleReactionChange(DiscordUser user, DiscordMessage message, DiscordEmoji emoji, DiscordReactionChange changeType)
        {
            if (emoji != DLConstants.ACCEPT_EMOJI && emoji != DLConstants.DENY_EMOJI)
                return;

            if (changeType != DiscordReactionChange.Added)
                return;

            Election election = GetElectionFromMessage(message);
            if (election == null || !election.BooleanElection)
                return;

            message.GetChannel().Guild.Members.TryGetValue(user.Id, out DiscordMember member);
            LinkedUser linkedUser = UserLinkManager.LinkedUserByDiscordUser(user, member, "Reaction Voting");
            if (linkedUser == null)
                return;

            if (!election.CanVote(linkedUser.EcoUser))
            {
                await linkedUser.DiscordMember.SendMessageAsync($"Your vote in election \"{election.Name.StripTags()}\" has not been registered as you are not an eligable voter for this election.");
                return;
            }

            string choice = emoji == DLConstants.ACCEPT_EMOJI ? "Yes" : "No";
            Result result = election.Vote(new UserRunoffVote(linkedUser.EcoUser, election.GetChoiceByName(choice).ID));
            if (result.Failed)
                Logger.Debug($"Failed to cast rection vote of type \"{choice}\" for Discord user \"{user.Username}\" in election {election.Id}. Message: {result.Message}");

            if (election.Process.AnonymousVoting)
            {
                await message.DeleteAllReactionsAsync("DiscordLink - Anonymous Election");
                await CreateVoteReactions(message);
            }
        }

        private Election GetElectionFromMessage(DiscordMessage message)
        {
            DisplayTracker tracker = DLStorage.PersistentData.Displays.Values.FirstOrDefault(tracker => tracker.MessageIds.Contains(message.Id));
            if (tracker == null)
            {
                Logger.Error($"Failed to find tracker for election display message in channel {message.Channel.GetLogName()}");
                return null;
            }

            Election election = Lookups.ActiveElections.FirstOrDefault(election => election.Id == tracker.ContentData);
            if(election == null)
            {
                Logger.Warning($"Failed to find active election matching display message tracker for message in channel {tracker}");
                return null;
            }

            return election;
        }

        private async Task CreateVoteReactions(DiscordMessage message)
        {
            if (DiscordLink.Obj.Client.ChannelHasPermission(message.GetChannel(), DiscordPermission.AddReactions))
            {
                await message.CreateReactionAsync(DLConstants.ACCEPT_EMOJI);
                await message.CreateReactionAsync(DLConstants.DENY_EMOJI);
            }
        }
    }
}
