using DSharpPlus.Entities;
using Eco.Moose.Tools.Logger;
using Eco.Shared.Utils;
using System.Collections.Generic;
using System.Linq;

namespace Eco.Plugins.DiscordLink
{
    public static class DiscordLinkConfig
    {
        public static void Initialize(ServerConfigData serverConfig)
        {
            _serverConfig = serverConfig;
        }

        public static void PostConnectionInit()
        {
            VerifyChannelLinks();
        }

        private static void VerifyChannelLinks()
        {
            foreach (ChannelLink link in ChannelLinks(verifiedLinksOnly: false))
            {
                if (!link.Initialize())
                    Logger.Warning($"ChannelLink failed to initialize.\nLink: {link}");
            }
        }

        private static ServerConfigData _serverConfig;

        public static string BotToken => _serverConfig.BotToken;
        public static ulong DiscordServerId => _serverConfig.DiscordServerId;
        public static DiscordColor EmbedColor => _serverConfig.EmbedColor;
        public static string ServerName => _serverConfig.ServerName;
        public static string ServerDescription => _serverConfig.ServerDescription;
        public static string ConnectionInfo => _serverConfig.ConnectionInfo;
        public static string InviteMessage => _serverConfig.InviteMessage;
        public static ChatSyncMode ChatSyncMode => _serverConfig.ChatSyncMode;
        public static Logger.LogLevel PluginLogLevel => _serverConfig.LogLevel;
        public static Microsoft.Extensions.Logging.LogLevel BackendLogLevel => _serverConfig.BackendLogLevel;
        public static int MaxTradeWatcherDisplaysPerUser => _serverConfig.MaxTradeWatcherDisplaysPerUser;
        public static bool DiscordServerOwnerIsAdmin => _serverConfig.DiscordServerOwnerIsAdmin;
        public static bool UseVerboseDisplay => _serverConfig.UseVerboseDisplay;
        public static bool UseLinkedAccountRole => _serverConfig.UseLinkedAccountRole;
        public static bool UseDemographicRoles => _serverConfig.UseDemographicRoles;
        public static bool UseSpecialtyRoles => _serverConfig.UseSpecialtyRoles;
        public static bool UseElectedTitleRoles => _serverConfig.UseElectedTitleRoles;
        public static bool UseTradeWatcherFeeds => _serverConfig.UseTradeWatcherFeeds;
        public static bool EnableTraceFileLogging => _serverConfig.EnableTraceFileLogging;
        public static IEnumerable<string> AdminRoles => _serverConfig.AdminRoles;
        public static IEnumerable<ChatChannelLink> ChatChannelLinks => _serverConfig.ChatChannelLinks;
        public static IEnumerable<ChannelLink> TradeFeedChannels => _serverConfig.TradeFeedChannels;
        public static IEnumerable<ChannelLink> CraftingFeedChannels => _serverConfig.CraftingFeedChannels;
        public static IEnumerable<ChannelLink> ServerStatusFeedChannels => _serverConfig.ServerStatusFeedChannels;
        public static IEnumerable<ChannelLink> PlayerStatusFeedChannels => _serverConfig.PlayerStatusFeedChannels;
        public static IEnumerable<ChannelLink> ElectionFeedChannels => _serverConfig.ElectionFeedChannels;
        public static IEnumerable<ServerLogFeedChannelLink> ServerLogFeedChannels => _serverConfig.ServerLogFeedChannels;
        public static IEnumerable<ServerInfoChannelLink> ServerInfoDisplayChannels => _serverConfig.ServerInfoDisplayChannels;
        public static IEnumerable<ChannelLink> WorkPartyDisplayChannels => _serverConfig.WorkPartyDisplayChannels;
        public static IEnumerable<ChannelLink> ElectionDisplayChannels => _serverConfig.ElectionDisplayChannels;
        public static IEnumerable<CurrencyChannelLink> CurrencyDisplayChannels => _serverConfig.CurrencyDisplayChannels;
        public static IEnumerable<MapChannelLink> MapDisplayChannels => _serverConfig.MapDisplayChannels;
        public static IEnumerable<LayerChannelLink> LayerDisplayChannels => _serverConfig.LayerDisplayChannels;
        public static IEnumerable<SpecialtiesChannelLink> SkillDisplayChannels => _serverConfig.SkillDisplayChannels;
        public static IEnumerable<RepairBountyChannelLink> RepairBountyDisplayChannels => _serverConfig.RepairBountyDisplayChannels;
        public static IEnumerable<ChannelLink> SnippetInputChannels => _serverConfig.SnippetInputChannels;
        public static IEnumerable<DemographicRoleSubstitution> DemographicReplacementRoles => _serverConfig.DemographicReplacementRoles;
        public static IEnumerable<EmoteIconSubstitution> EmoteIconSubstitutions => _serverConfig.EmoteIconSubstitutions;

        public static IEnumerable<ChannelLink> ChannelLinks(bool verifiedLinksOnly = true)
        {
            return ChatChannelLinks
                .Concat(TradeFeedChannels)
                .Concat(CraftingFeedChannels)
                .Concat(ServerStatusFeedChannels)
                .Concat(PlayerStatusFeedChannels)
                .Concat(ElectionFeedChannels)
                .Concat(ServerLogFeedChannels)
                .Concat(ServerInfoDisplayChannels)
                .Concat(WorkPartyDisplayChannels)
                .Concat(ElectionDisplayChannels)
                .Concat(CurrencyDisplayChannels)
                .Concat(MapDisplayChannels)
                .Concat(LayerDisplayChannels)
                .Concat(SkillDisplayChannels)
                .Concat(RepairBountyDisplayChannels)
                .Concat(SnippetInputChannels)
                .Where(x => !verifiedLinksOnly || x.Verified);
        }

        public static IEnumerable<ChatChannelLink> ChatLinksForEcoChannel(string ecoChannelName) =>
            ChatChannelLinks.Where(link => link.IsValid() && link.EcoChannel.EqualsCaseInsensitive(ecoChannelName));

        public static IEnumerable<ChatChannelLink> ChatLinksForDiscordChannel(DiscordChannel channel) =>
            ChatChannelLinks.Where(link => link.IsValid() && link.DiscordChannelId == channel.Id);
    }
}
