using Eco.Plugins.DiscordLink.Extensions;

namespace Eco.Plugins.DiscordLink
{
    public class DisplayContent
    {
        public DisplayContent(string textContent = "", DiscordLinkEmbed embedContent = null, int contentData = -1)
        {
            TextContent = textContent;
            EmbedContent = embedContent;
            ContentData = contentData;
        }

        public string TextContent { get; private set; }
        public DiscordLinkEmbed EmbedContent { get; private set; }
        public int ContentData { get; private set; }
    }
}
