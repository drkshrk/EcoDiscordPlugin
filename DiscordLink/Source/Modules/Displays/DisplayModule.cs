using DSharpPlus.Entities;
using Eco.Moose.Tools.Logger;
using Eco.Moose.Utils.SystemUtils;
using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink.Modules
{
    public abstract class DisplayModule : Module
    {
        public DateTime LastUpdateTime { get; protected set; } = DateTime.MinValue;

        protected virtual int TimerUpdateIntervalMs { get; } = -1;
        protected virtual int TimerStartDelayMs { get; } = 0;
        protected virtual int HighFrequencyEventDelayMs { get; } = 2000;

        private Timer _updateTimer = null;
        private Timer _highFrequencyEventTimer = null;

        protected override DlEventType GetTriggers() => DlEventType.ManualStart | DlEventType.ForceUpdate | DlEventType.DiscordMessageDeleted | DlEventType.DiscordReactionAdded | DlEventType.DiscordReactionRemoved;
        public virtual async Task<IEnumerable<DiscordTarget>> GetDiscordTargets() { throw new NotImplementedException(); }

        protected override async Task<bool> ShouldRun()
        {
            foreach (DiscordTarget target in await GetDiscordTargets())
            {
                // If there is at least one valid target, we should run the display
                if (target.IsValid())
                    return true;
            }
            return false;
        }

        public override async Task<string> GetDisplayText(string childInfo, bool verbose)
        {
            string lastUpdateTime = (LastUpdateTime == DateTime.MinValue) ? "Never" : LastUpdateTime.ToString("yyyy-MM-dd HH:mm");
            IEnumerable<DiscordTarget> targets = await GetDiscordTargets();
            int trackedMessageCount = 0;
            foreach (DiscordTarget target in targets)
            {
                if (DLStorage.PersistentData.Displays.TryGetValue(target.Id, out DisplayTracker tracker))
                    trackedMessageCount += tracker.MessageIds.Count;
            }
            string info = $"Last update time: {lastUpdateTime}";
            info += $"\r\nTracked Display Messages: {trackedMessageCount}";
            info += $"\r\n{childInfo}";

            return await base.GetDisplayText(info, verbose);
        }

        protected override async Task Initialize()
        {
            bool timerStarted = StartTimer();
            await base.Initialize();

            if (!timerStarted) // If the module doesn't rely on timed updates, we should trigger a manual update
                await Update(DiscordLink.Obj, DlEventType.ManualStart);
        }

        protected override async Task Shutdown()
        {
            StopTimer();
            await base.Shutdown();
        }

        public bool StartTimer()
        {
            if ((GetTriggers() & DlEventType.Timer) == 0)
                return false;

            if (_updateTimer != null)
                StopTimer();

            _updateTimer = new Timer(this.TriggerTimedUpdate, null, TimerStartDelayMs, TimerUpdateIntervalMs == -1 ? Timeout.Infinite : TimerUpdateIntervalMs);
            return true;
        }

        public void StopTimer()
        {
            if ((GetTriggers() & DlEventType.Timer) == 0)
                return;

            SystemUtils.StopAndDestroyTimer(ref _updateTimer);
        }

        private void TriggerTimedUpdate(object stateInfo)
        {
            _ = base.Update(DiscordLink.Obj, DlEventType.Timer, null);
            SystemUtils.StopAndDestroyTimer(ref _highFrequencyEventTimer);
        }

        protected sealed override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            bool modifiedDisplayTrackingData = false;

            // Handle deleted messages first to avoid exceptions
            if (trigger == DlEventType.DiscordMessageDeleted)
            {
                if (!(data[0] is DiscordMessage message))
                    return;

                KeyValuePair<Guid, DisplayTracker> IdAndTracker = DLStorage.PersistentData.Displays.FirstOrDefault(entry => entry.Value.MessageIds.Contains(message.Id));
                if (IdAndTracker.Key == Guid.Empty)
                    return;

                // Clean up any remaining parts of the display
                foreach (ulong messageId in IdAndTracker.Value.MessageIds)
                {
                    if (message.Id == messageId)
                        continue; // The message from the event has already been deleted by the user

                    // The message may have been deleted by a user, so don't throw warnings if it's missing
                    await plugin.Client.DeleteMessageAsync(IdAndTracker.Value.ChannelId, messageId, "DiscordLink cleanup of partially deleted display", suppressMissingMessageWarning: true);
                }

                DLStorage.PersistentData.Displays.Remove(IdAndTracker.Key);
                modifiedDisplayTrackingData = true;
            }
            else if (trigger == DlEventType.DiscordReactionAdded || trigger == DlEventType.DiscordReactionRemoved)
            {
                DiscordReactionChange changeType = (trigger == DlEventType.DiscordReactionAdded ? DiscordReactionChange.Added : DiscordReactionChange.Removed);
                await HandleReactionChange(data[0] as DiscordUser, data[1] as DiscordMessage, data[2] as DiscordEmoji, changeType);
                return;
            }

            // Block Display implementations from using edit and delete events
            if (trigger == DlEventType.DiscordMessageEdited || trigger == DlEventType.DiscordMessageDeleted)
                return;

            // Avoid hitting the rate limitation by not allowig events that can be fired often to pass straight through.
            if ((trigger & HighFrequencyTriggerFlags) == trigger)
            {
                if (_highFrequencyEventTimer == null)
                    _highFrequencyEventTimer = new Timer(this.TriggerTimedUpdate, null, HighFrequencyEventDelayMs, Timeout.Infinite);
                return;
            }

            // Get a copy of the target list - The config can manipulate the original enumerable while we are iterating over it
            IEnumerable<DiscordTarget> targets = (await GetDiscordTargets()).ToList();

            foreach(DiscordTarget target in targets)
            {
                GetDisplayContent(target, out List<DisplayContent> displayContent);

                ChannelLink channelLink = target as ChannelLink;
                UserLink userLink = target as UserLink;
                if (channelLink == null && userLink == null)
                {
                    Logger.Warning($"Failed to update display module \"{this}\". Could not resolve type of Discord target with ID: {target.Id}");
                    continue;
                }

                DiscordChannel targetChannel;
                if (channelLink != null && channelLink.IsValid())
                {
                    targetChannel = channelLink.Channel;
                }    
                else if (userLink != null)
                {
                    targetChannel = await plugin.Client.GetOrCreateDmChannelAsync(userLink.Member);
                }
                else
                {
                    Logger.Warning($"Failed to update display module \"{this}\". Could not resolve discord channel type of Discord target with ID: {target.Id}");
                    continue;
                }
                
                // Update the display if it already exists
                if (DLStorage.PersistentData.Displays.TryGetValue(target.Id, out DisplayTracker tracker))
                {
                    foreach (DisplayContent content in displayContent)
                    {
                        SendReadyMessage messageData = plugin.Client.FormatMessageForSending(targetChannel, content.TextContent, content.EmbedContent);

                        tracker.ContentData = content.ContentData;
                        int existingMessageCount = tracker.MessageIds.Count;
                        int targetMessageCount = messageData.StringParts.Count + messageData.EmbedParts.Count;
                        for(int i = 0; i < targetMessageCount; ++i)
                        {
                            IEnumerable<DiscordMessage> createdMessages;
                            if (i < existingMessageCount)
                            {
                                DiscordMessage message = await plugin.Client.FetchMessageAsync(targetChannel, tracker.MessageIds[i], expectNotFound: true);
                                ++_opsCount;
                                if(message == null)
                                {
                                    Logger.Warning($"Failed to find message with ID {tracker.MessageIds[i]} in channel \"{targetChannel}\" when updating {this}");
                                    tracker.MessageIds.RemoveAt(i);
                                    --existingMessageCount;
                                    --i;
                                    continue;
                                }

                                if (i < messageData.StringParts.Count)
                                    createdMessages = await plugin.Client.ModifyMessageAsync(message, new SendReadyMessage(messageData.StringParts.ElementAt(i)));
                                else
                                    createdMessages = await plugin.Client.ModifyMessageAsync(message, new SendReadyMessage(messageData.EmbedParts.ElementAt(i - messageData.StringParts.Count)));
                                ++_opsCount;
                                tracker.MessageIds.AddRange(createdMessages.Select(message => message.Id));
                            }
                            else
                            {
                                if (i < messageData.StringParts.Count)
                                    createdMessages = await plugin.Client.SendMessageAsync(targetChannel, new SendReadyMessage(messageData.StringParts.ElementAt(i)));
                                else
                                    createdMessages = await plugin.Client.SendMessageAsync(targetChannel, new SendReadyMessage(messageData.EmbedParts.ElementAt(i - messageData.StringParts.Count)));
                                ++_opsCount;
                                tracker.MessageIds.AddRange(createdMessages.Select(message => message.Id));
                            }
                        }

                        // Delete any leftover messages
                        int messagesLeft = existingMessageCount - targetMessageCount;
                        if(messagesLeft > 0)
                        {
                            for (int i = targetMessageCount; i < existingMessageCount; ++i)
                            {
                                DiscordMessage message = await plugin.Client.FetchMessageAsync(targetChannel, tracker.MessageIds[i], expectNotFound: true);
                                if (message != null)
                                {
                                    // Message may have been removed manually by user
                                    await plugin.Client.DeleteMessageAsync(message);
                                    ++_opsCount;
                                }
                                tracker.MessageIds.Remove(tracker.MessageIds[i]);
                            }
                        }
                    }
                }
                else // Create the display if it does not already exist
                {
                    foreach (DisplayContent content in displayContent)
                    {
                        SendReadyMessage messageData = plugin.Client.FormatMessageForSending(targetChannel, content.TextContent, content.EmbedContent);
                        IEnumerable<DiscordMessage> createdMessages = await plugin.Client.SendMessageAsync(targetChannel, content.TextContent, content.EmbedContent);
                        ++_opsCount;

                        if (!createdMessages.Any())
                        {
                            Logger.Error($"Failed to create Display message for \"{this}\" in {targetChannel.GetLogName()}");
                            continue;
                        }

                        DLStorage.PersistentData.Displays.Add(target.Id, new DisplayTracker(targetChannel.Id, createdMessages.Select(message => message.Id)));
                        modifiedDisplayTrackingData = true;
                        await PostDisplayCreated(createdMessages);
                    }
                }
            }

            if (modifiedDisplayTrackingData)
                DLStorage.Instance.Write();

            LastUpdateTime = DateTime.Now;
        }

        protected abstract void GetDisplayContent(DiscordTarget target, out List<DisplayContent> displayContent);

        protected async virtual Task PostDisplayCreated(IEnumerable<DiscordMessage> messages) { }

        protected async virtual Task PostDisplayEdited(IEnumerable<DiscordMessage> messages) { }

        protected async virtual Task HandleReactionChange(DiscordUser user, DiscordMessage message, DiscordEmoji reaction, DiscordReactionChange changeType) { }
    }
}
