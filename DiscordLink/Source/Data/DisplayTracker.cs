using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    public class DisplayTracker
    {
        public DisplayTracker(ulong channelId, IEnumerable<ulong> messageIds)
        {
            ChannelId = channelId;
            MessageIds = new List<ulong>(messageIds);
        }

        public ulong ChannelId { get; private set; }
        public List<ulong> MessageIds { get; private set; }
        public int ContentData { get; set; } = -1;
    }
}
