using DSharpPlus.Entities;
using Eco.Core.Plugins;
using Eco.Moose.Tools.Logger;
using Eco.Plugins.DiscordLink.Extensions;
using Eco.Plugins.Networking;
using Eco.Shared.Serialization;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Eco.Plugins.DiscordLink
{
    public sealed class ServerConfig
    {
        public PluginConfig<ServerConfigData> PluginConfig { get; private set; }
        public ServerConfigData ConfigData => PluginConfig.Config;

        public delegate Task OnConfigChangedDelegate(object sender, EventArgs e);
        public event OnConfigChangedDelegate OnConfigChanged;
        public event EventHandler OnConfigSaved;

        private ServerConfigData _prevConfigData; // Used to detect differences when the config is saved

        private readonly List<ChannelLink> _allChannelLinks = new List<ChannelLink>();

        public ServerConfig()
        {
        }

        public void Initialize()
        {
            PluginConfig = new PluginConfig<ServerConfigData>("DiscordLink");
            _prevConfigData = (ServerConfigData)ConfigData.Clone();

            ConfigData.ChatChannelLinks.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.TradeFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.CraftingFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.ServerStatusFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.PlayerStatusFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.ElectionFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.ServerLogFeedChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.ServerInfoDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.WorkPartyDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.ElectionDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.CurrencyDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.MapDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.LayerDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.SkillDisplayChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
            ConfigData.SnippetInputChannels.CollectionChanged += (obj, args) => { HandleCollectionChanged(args); };
        }

        public void PostConnectionInit()
        {
            // Channel Links
            BuildChanneLinkList();
        }

        public void HandleCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            _ = HandleConfigChanged();
        }

        public async Task HandleConfigChanged()
        {
            Logger.Debug("Config Changed");

            // Do not verify if change occurred as this function is going to be called again in that case
            // Do not verify the config in case critical data has been changed, as the client will be restarted and that will trigger verification
            bool tokenChanged = ConfigData.BotToken != _prevConfigData.BotToken;
            bool guildChanged = ConfigData.DiscordServerId != _prevConfigData.DiscordServerId;
            bool logLevelChanged = ConfigData.LogLevel != _prevConfigData.LogLevel;
            bool correctionMade = !Save();

            BuildChanneLinkList();

            if (tokenChanged || guildChanged)
            {
                Logger.Info("Critical config data changed - Please restart the plugin for these changes to take effect");
            }

            if (logLevelChanged)
            {
                Logger.SetConfiguredLogLevel(ConfigData.LogLevel);
            }

            if (DiscordLink.Obj.Client.ConnectionStatus == DiscordClient.ConnectionState.Connected)
            {
                BuildChanneLinkList();
            }

            // If a correction was made, this function will be called again.
            // If the guild becomes null, this check is unstable and will be performed on plugin restart anyway.
            if (!correctionMade && !guildChanged)
            {
                if (OnConfigChanged != null)
                {
                    await OnConfigChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool Save() // Returns true if no correction was needed
        {
            bool correctionMade = false;

            // Channel Links
            foreach (ChannelLink link in _allChannelLinks)
            {
                if (link.MakeCorrections())
                {
                    correctionMade = true;
                }
            }

            // Max tracked trades per user
            if (ConfigData.MaxTradeWatcherDisplaysPerUser < 0)
            {
                ConfigData.MaxTradeWatcherDisplaysPerUser = ServerConfigDefaultValues.MaxTradeWatcherDisplaysPerUser;
            }

            // Invite Message
            if (string.IsNullOrEmpty(ConfigData.InviteMessage))
            {
                ConfigData.InviteMessage = ServerConfigDefaultValues.InviteMessage;
                correctionMade = true;
            }

            // Currency channels
            foreach (CurrencyChannelLink link in ConfigData.CurrencyDisplayChannels)
            {
                if (link.MaxMintedCount < 0)
                {
                    link.MaxMintedCount = ServerConfigDefaultValues.MaxMintedCurrencies;
                    correctionMade = true;
                }

                if (link.MaxPersonalCount < 0)
                {
                    link.MaxPersonalCount = ServerConfigDefaultValues.MaxPersonalCurrencies;
                    correctionMade = true;
                }

                if (link.MaxTopCurrencyHolderCount < 0 || link.MaxTopCurrencyHolderCount > DLConstants.MAX_TOP_CURRENCY_HOLDER_DISPLAY_LIMIT)
                {
                    link.MaxTopCurrencyHolderCount = ServerConfigDefaultValues.MaxTopCurrencyHolderCount;
                    correctionMade = true;
                }
            }

            PluginConfig.SaveAsync().Wait();
            OnConfigSaved?.Invoke(this, EventArgs.Empty);
            _prevConfigData = (ServerConfigData)ConfigData.Clone();

            return !correctionMade;
        }

        private void BuildChanneLinkList()
        {
            _allChannelLinks.Clear();
            _allChannelLinks.AddRange(PluginConfig.Config.ChatChannelLinks);
            _allChannelLinks.AddRange(PluginConfig.Config.TradeFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.CraftingFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.ServerStatusFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.PlayerStatusFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.ElectionFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.ServerLogFeedChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.ServerInfoDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.WorkPartyDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.ElectionDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.CurrencyDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.MapDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.LayerDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.SkillDisplayChannels);
            _allChannelLinks.AddRange(PluginConfig.Config.SnippetInputChannels);
        }
    }

    public class ServerConfigData : ICloneable
    {
        public object Clone() // Be careful not to change the original object here as that will trigger endless recursion.
        {
            return new ServerConfigData
            {
                BotToken = this.BotToken,
                DiscordServerId = this.DiscordServerId,
                MinEmbedSizeForFooter = this.MinEmbedSizeForFooter,
                EmbedColorHex = this.EmbedColorHex,
                ServerName = this.ServerName,
                ServerDescription = this.ServerDescription,
                ConnectionInfo = this.ConnectionInfo,
                InviteMessage = this.InviteMessage,
                ChatSyncMode = this.ChatSyncMode,
                LogLevel = this.LogLevel,
                BackendLogLevel = this.BackendLogLevel,
                MaxTradeWatcherDisplaysPerUser = this.MaxTradeWatcherDisplaysPerUser,
                DiscordServerOwnerIsAdmin = this.DiscordServerOwnerIsAdmin,
                UseVerboseDisplay = this.UseVerboseDisplay,
                UseLinkedAccountRole = this.UseLinkedAccountRole,
                UseDemographicRoles = this.UseDemographicRoles,
                UseSpecialtyRoles = this.UseSpecialtyRoles,
                UseElectedTitleRoles = this.UseElectedTitleRoles,
                UseTradeWatcherFeeds = this.UseTradeWatcherFeeds,
                EnableTraceFileLogging = this.EnableTraceFileLogging,
                AdminRoles = new ObservableCollection<string>(this.AdminRoles.Select(t => t.Clone()).Cast<string>()),
                ChatChannelLinks = new ObservableCollection<ChatChannelLink>(this.ChatChannelLinks.Select(t => t.Clone()).Cast<ChatChannelLink>()),
                TradeFeedChannels = new ObservableCollection<ChannelLink>(this.TradeFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                CraftingFeedChannels = new ObservableCollection<ChannelLink>(this.CraftingFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ServerStatusFeedChannels = new ObservableCollection<ChannelLink>(this.ServerStatusFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                PlayerStatusFeedChannels = new ObservableCollection<ChannelLink>(this.PlayerStatusFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ElectionFeedChannels = new ObservableCollection<ChannelLink>(this.ElectionFeedChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ServerLogFeedChannels = new ObservableCollection<ServerLogFeedChannelLink>(this.ServerLogFeedChannels.Select(t => t.Clone()).Cast<ServerLogFeedChannelLink>()),
                ServerInfoDisplayChannels = new ObservableCollection<ServerInfoChannelLink>(this.ServerInfoDisplayChannels.Select(t => t.Clone()).Cast<ServerInfoChannelLink>()),
                WorkPartyDisplayChannels = new ObservableCollection<ChannelLink>(this.WorkPartyDisplayChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                ElectionDisplayChannels = new ObservableCollection<ChannelLink>(this.ElectionDisplayChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                CurrencyDisplayChannels = new ObservableCollection<CurrencyChannelLink>(this.CurrencyDisplayChannels.Select(t => t.Clone()).Cast<CurrencyChannelLink>()),
                MapDisplayChannels = new ObservableCollection<MapChannelLink>(this.MapDisplayChannels.Select(t => t.Clone()).Cast<MapChannelLink>()),
                LayerDisplayChannels = new ObservableCollection<LayerChannelLink>(this.LayerDisplayChannels.Select(t => t.Clone()).Cast<LayerChannelLink>()),
                SkillDisplayChannels = new ObservableCollection<SpecialtiesChannelLink>(this.SkillDisplayChannels.Select(t => t.Clone()).Cast<SpecialtiesChannelLink>()),
                SnippetInputChannels = new ObservableCollection<ChannelLink>(this.SnippetInputChannels.Select(t => t.Clone()).Cast<ChannelLink>()),
                DemographicReplacementRoles = new ObservableCollection<DemographicRoleSubstitution>(this.DemographicReplacementRoles.Select(t => t.Clone()).Cast<DemographicRoleSubstitution>()),
                EmoteIconSubstitutions = new ObservableCollection<EmoteIconSubstitution>(this.EmoteIconSubstitutions.Select(t => t.Clone()).Cast<EmoteIconSubstitution>()),
            };
        }

        [Description("The token provided by the Discord API to allow access to the Discord bot. This setting can be changed while the server is running but will require a plugin restart to take effect."), Category("Base Configuration - Discord")]
        public string BotToken { get; set; }

        [Description("The ID if the Discord Server. This setting can be changed while the server is running but will require a plugin restart to take effect."), Category("Base Configuration - Discord")]
        public ulong DiscordServerId { get; set; }

        [Description("The roles recognized as having admin permissions in DiscordLink. This setting requires a plugin restart to take effect."), Category("Base Configuration - Discord")]
        public ObservableCollection<string> AdminRoles { get; set; } = new ObservableCollection<string>(ServerConfigDefaultValues.AdminRoles);

        [Description("Determines if the owner of the Discord server should have admin permissions in DiscordLink. This setting requires a plugin restart to take effect."), Category("Base Configuration - Discord")]
        public bool DiscordServerOwnerIsAdmin { get; set; } = ServerConfigDefaultValues.DiscordServerOwnerIsAdmin;

        [Description("The name of the Eco server, overriding the name configured within Eco. This setting can be changed while the server is running."), Category("Base Configuration - Eco")]
        public string ServerName { get; set; }

        [Description("The description of the Eco server, overriding the description configured within Eco. This setting can be changed while the server is running."), Category("Base Configuration - Eco")]
        public string ServerDescription { get; set; }

        [Description("The game server connection information to display to users. This setting can be changed while the server is running."), Category("Base Configuration - Eco")]
        public string ConnectionInfo { get; set; } = $"Server Id: {NetworkManager.Config.ID.ToString()}";

        [Description("Whether chat message should be synchroinized by default or not. This setting can be changed while the server is running."), Category("Base Configuration - Eco")]
        public ChatSyncMode ChatSyncMode { get; set; } = ServerConfigDefaultValues.ChatSynchronizationMode;

        [Description("Discord and Eco Channels to connect together for chat crossposting. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChatChannelLink> ChatChannelLinks { get; set; } = new ObservableCollection<ChatChannelLink>();

        [Description("Discord Channels in which trade events will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChannelLink> TradeFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which crafting events will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChannelLink> CraftingFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which server status events will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChannelLink> ServerStatusFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which player status events will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChannelLink> PlayerStatusFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which election events will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ChannelLink> ElectionFeedChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which server log entries will be posted. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public ObservableCollection<ServerLogFeedChannelLink> ServerLogFeedChannels { get; set; } = new ObservableCollection<ServerLogFeedChannelLink>();

        [Description("Determines if users can use trade watcher feeds. This setting can be changed while the server is running."), Category("Modules - Feeds")]
        public bool UseTradeWatcherFeeds { get; set; } = ServerConfigDefaultValues.UseTradeWatcherFeeds;

        [Description("Discord channels in which to keep a Server Info display. DiscordLink will post one server info message in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<ServerInfoChannelLink> ServerInfoDisplayChannels { get; set; } = new ObservableCollection<ServerInfoChannelLink>();

        [Description("Discord channels in which to keep ongoing work parties. DiscordLink will post messages in these channel and keep them updated through edits. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<ChannelLink> WorkPartyDisplayChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to keep a Election display. DiscordLink will post election messages in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<ChannelLink> ElectionDisplayChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Discord channels in which to keep a currency display. DiscordLink will post currency messages in these channel and keep it updated through edits. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<CurrencyChannelLink> CurrencyDisplayChannels { get; set; } = new ObservableCollection<CurrencyChannelLink>();

        [Description("Discord channels in which to keep a map display. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<MapChannelLink> MapDisplayChannels { get; set; } = new ObservableCollection<MapChannelLink>();

        [Description("Discord channels in which to keep a layer display. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<LayerChannelLink> LayerDisplayChannels { get; set; } = new ObservableCollection<LayerChannelLink>();

        [Description("Discord channels in which to keep a skill display. This setting can be changed while the server is running."), Category("Modules - Displays")]
        public ObservableCollection<SpecialtiesChannelLink> SkillDisplayChannels { get; set; } = new ObservableCollection<SpecialtiesChannelLink>();

        [Description("Discord channels in which to search for snippets for the Snippet command. This setting can be changed while the server is running."), Category("Modules - Inputs")]
        public ObservableCollection<ChannelLink> SnippetInputChannels { get; set; } = new ObservableCollection<ChannelLink>();

        [Description("Determines if a Discord role will be granted to users who link their Discord accounts. This setting can be changed while the server is running."), Category("Modules - Roles")]
        public bool UseLinkedAccountRole { get; set; } = ServerConfigDefaultValues.UseLinkedAccountRole;

        [Description("Determines if Discord roles matching ingame demographics will be granted to users who have linked their accounts. This setting can be changed while the server is running."), Category("Modules - Roles")]
        public bool UseDemographicRoles { get; set; } = ServerConfigDefaultValues.UseDemographicRoles;

        [Description("Roles that will be used (and created if needed) for the given demographics. This setting can be changed while the server is running."), Category("Modules - Roles")]
        public ObservableCollection<DemographicRoleSubstitution> DemographicReplacementRoles { get; set; } = new ObservableCollection<DemographicRoleSubstitution>(ServerConfigDefaultValues.DemographicRoleReplacements);

        [Description("Determines if Discord roles matching ingame specialties will be granted to users who have linked their accounts. This setting can be changed while the server is running."), Category("Modules - Roles")]
        public bool UseSpecialtyRoles { get; set; } = ServerConfigDefaultValues.UseSpecialtyRoles;

        [Description("Determines if Discord roles matching ingame elected titles will be granted to users who have linked their accounts. This setting can be changed while the server is running."), Category("Modules - Roles")]
        public bool UseElectedTitleRoles { get; set; } = ServerConfigDefaultValues.UseElectedTitleRoles;

        [Description("Max amount of tracked trades allowed per user. Set to 0 to disable trade watchers. This setting can be changed while the server is running, but does not apply retroactively."), Category("Commands")]
        public int MaxTradeWatcherDisplaysPerUser { get; set; } = ServerConfigDefaultValues.MaxTradeWatcherDisplaysPerUser;

        [Description("The message to use for the /DiscordInvite command. The invite link is fetched from the network config and will replace the token " + DLConstants.INVITE_COMMAND_TOKEN + ". This setting can be changed while the server is running."), Category("Commands")]
        public string InviteMessage { get; set; } = ServerConfigDefaultValues.InviteMessage;

        [Description("Determines what message types will be printed to the server log. All message types below the selected one will be printed as well. This setting can be changed while the server is running."), Category("Plugin Configuration")]
        public Logger.LogLevel LogLevel { get; set; } = ServerConfigDefaultValues.PluginLogLevel;

        [Description("Determines what backend message types will be printed to the server log. All message types below the selected one will be printed as well. This setting requires a plugin restart to take effect."), Category("Plugin Configuration")]
        public Microsoft.Extensions.Logging.LogLevel BackendLogLevel { get; set; } = ServerConfigDefaultValues.BackendLogLevel;

        [Description("Trace-Logs are not logged to file by default. Please only enable this to diagnose issues. Enabling this will save every single request made to the Discord API to the logfile. This will create huge logs over time. This setting requires a plugin restart to take effect."), Category("Plugin Configuration")]
        public bool EnableTraceFileLogging { get; set; } = ServerConfigDefaultValues.EnableTraceFileLogging;

        [Description("Determines if the output in the display tab of the server GUI should be verbose or not. This setting can be changed while the server is running."), Category("Plugin Configuration")]
        public bool UseVerboseDisplay { get; set; } = ServerConfigDefaultValues.UseVerboseDisplay;

        [Description("Emote keys to replace with eco icons using the value name. This setting can be changed while the server is running."), Category("Emotes")]
        public ObservableCollection<EmoteIconSubstitution> EmoteIconSubstitutions { get; set; } = new ObservableCollection<EmoteIconSubstitution>(ServerConfigDefaultValues.EmoteSubstitutions);

        [Description("Determines for what sizes of embeds to show the footer containing meta information about posted embeds. All embeds of sizes bigger than the selected one will have footers as well. This setting can be changed while the server is running."), Category("Style - Discord")]
        public DiscordLinkEmbed.EmbedSize MinEmbedSizeForFooter { get; set; } = ServerConfigDefaultValues.MinEmbedSizeForFooter;

        [Description("Determines the color of the left outline of embeds. Must be a valid hexadecimal color string. This setting can be changed while the server is running."), Category("Style - Discord")]
        public string EmbedColorHex { get { return _embedColorHex; } set { EmbedColor = new DiscordColor(value); _embedColorHex = value; } }
        private string _embedColorHex = ServerConfigDefaultValues.EmbedColor;

        [Browsable(false), JsonIgnore]
        public DiscordColor EmbedColor { get; set; } = new DiscordColor(ServerConfigDefaultValues.EmbedColor);
    }
}
