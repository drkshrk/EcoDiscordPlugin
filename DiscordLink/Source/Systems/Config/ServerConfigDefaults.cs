using Eco.Moose.Tools.Logger;

namespace Eco.Plugins.DiscordLink
{
    public static class ServerConfigDefaultValues
    {
        public static Logger.LogLevel PluginLogLevel = Logger.LogLevel.Information;
        public static Microsoft.Extensions.Logging.LogLevel BackendLogLevel = Microsoft.Extensions.Logging.LogLevel.None;
        public static bool EnableTraceFileLogging = false;
        public static bool UseVerboseDisplay = false;
        public static bool DiscordServerOwnerIsAdmin = true;
        public static readonly string[] AdminRoles = { "Admin", "Eco Admins", "Administrator", "Moderator" };
        public const string InviteMessage = "Join us on Discord!\\n" + DLConstants.INVITE_COMMAND_TOKEN;
        public const ChatSyncMode ChatSynchronizationMode = ChatSyncMode.OptOut;
        public const int MaxMintedCurrencies = 1;
        public const int MaxPersonalCurrencies = 3;
        public const int MaxTopCurrencyHolderCount = 3;
        public const int MaxTradeWatcherDisplaysPerUser = 5;
        public const bool UseTradeWatcherFeeds = true;
        public const bool UseLinkedAccountRole = true;
        public const bool UseDemographicRoles = true;
        public const bool UseSpecialtyRoles = true;
        public const bool UseElectedTitleRoles = true;
        public static readonly string EmbedColor = "#7289da";
        public static readonly DemographicRoleSubstitution[] DemographicRoleReplacements = { new DemographicRoleSubstitution("everyone", "Eco Everyone"), new DemographicRoleSubstitution("admins", "Eco Admins") };
        public static readonly EmoteIconSubstitution[] EmoteSubstitutions = { new EmoteIconSubstitution("DiscordLink", "DiscordLinkLogo") };
    }
}
