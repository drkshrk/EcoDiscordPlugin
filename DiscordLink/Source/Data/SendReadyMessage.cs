using DSharpPlus.Entities;
using System.Collections.Generic;

namespace Eco.Plugins.DiscordLink
{
    public class SendReadyMessage
    {
        public SendReadyMessage(string str)
        {
            StringParts.Add(str);
        }

        public SendReadyMessage(DiscordEmbed embed)
        {
            EmbedParts.Add(embed);
        }

        public SendReadyMessage(ICollection<string> stringParts, ICollection<DiscordEmbed> embedParts)
        {
            StringParts = stringParts;
            EmbedParts = embedParts;
        }

        public ICollection<string> StringParts { get; private set; } = new List<string>();
        public ICollection<DiscordEmbed> EmbedParts { get; private set; } = new HashSet<DiscordEmbed>();
    }
}
