using Eco.Plugins.DiscordLink.Events;
using Eco.Plugins.DiscordLink.Modules;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Eco.Shared.Utils;

namespace Eco.Plugins.DiscordLink
{
    public class DisplayCleanupModule : Module
    {
        public override string ToString()
        {
            return "Display Cleanup Module";
        }

        protected override DlEventType GetTriggers()
        {
            return DlEventType.ForceUpdate | DlEventType.ServerStarted;
        }

        protected override async Task<bool> ShouldRun() => true;

        protected override async Task HandleConfigChanged(object sender, EventArgs e)
        {
            await base.HandleConfigChanged(sender, e);

            if (await ShouldRun())
                await Update(DiscordLink.Obj, DlEventType.ForceUpdate);
        }

        protected override async Task UpdateInternal(DiscordLink plugin, DlEventType trigger, params object[] data)
        {
            // Find all display targets
            IEnumerable<DisplayModule> displays = DiscordLink.Obj.Modules.Where(module => module.GetType().DerivesFrom<DisplayModule>()).Cast<DisplayModule>();
            IEnumerable<DiscordTarget> targets = displays.Select(async display => await display.GetDiscordTargets()).SelectMany(task => task.Result);

            // Delete all tracked messages that don't correspond to a target
            List<KeyValuePair<Guid, DisplayTracker>> toDelete = new List<KeyValuePair<Guid, DisplayTracker>>();
            foreach (var entry in DLStorage.PersistentData.Displays)
            {
                if (!targets.Any(target => target.Id == entry.Key))
                    toDelete.Add(entry);
            }
            foreach (var entry in toDelete)
            {
                foreach (ulong messageId in entry.Value.MessageIds)
                {
                    // The message may have been deleted by a user, so don't throw warnings if it's missing
                    await plugin.Client.DeleteMessageAsync(entry.Value.ChannelId, messageId, "DiscordLink cleanup of obsolete display" , suppressMissingMessageWarning: true);
                    ++_opsCount;
                }
                DLStorage.PersistentData.Displays.Remove(entry.Key);
            }
        }
    }
}
